using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BlacklistBackupConfig : IBackupConfig {
    public DirectoryInfo OriginInfo { get; }
    public List<DirectoryInfo> BackupInfos { get; } = [];
    [JsonIgnore] public FileInfo ConfigFileInfo { get; }
    //public FileInfo MetaDataFileInfo { get; }

    public List<string> IgnoredPaths { get; } = [];
    public List<string> IgnoredFileExtensions { get; } = [];
    public List<string> IgnoredPrefixes { get; } = [];
    public List<string> IgnoredFolderNames { get; } = [];
    public List<string> IgnoredFileNames { get; } = [];

    private BlacklistBackupConfig(DirectoryInfo originInfo) {
        OriginInfo = originInfo;
        //MetaDataFileInfo = metaDataFileInfo;
        ConfigFileInfo = new(Path.Combine(OriginInfo.FullName, "backup_config.json"));
    }

    [JsonConstructor]
    public BlacklistBackupConfig(DirectoryInfo originInfo, List<DirectoryInfo> backupInfos, List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredPrefixes, List<string> ignoredFolderNames, List<string> ignoredFileNames) : 
        this(originInfo){

        BackupInfos = backupInfos;
        IgnoredPaths = ignoredPaths;
        IgnoredFileExtensions = ignoredFileExtensions;
        IgnoredPrefixes = ignoredPrefixes;
        IgnoredFolderNames = ignoredFolderNames;
        IgnoredFileNames = ignoredFileNames;
    }

    public static BlacklistBackupConfig Create(string originPath, string backupPath) {
        var config = new BlacklistBackupConfig(new(originPath));

        (config as IBackupConfig).AddBackupLocation(backupPath);

        if(!config.ConfigFileInfo.Exists)
            config.ConfigFileInfo.Create().Dispose();
        //if(!config.MetaDataFileInfo.Exists) MetaDataFileInfo.Create().Dispose();
        config.IgnoreFolders("System Volume Information", ".git");
        config.IgnoreExtensions(".blend1");
        config.IgnorePrefix("$");
        //config.IgnorePaths(config.ConfigFileInfo.FullName);

        return config;
    }

    public bool ShouldInclude(FileSystemInfo info) => !ShouldExclude(info);

    public bool ShouldExclude(FileSystemInfo info) {
        if(info.Name.StartsWith('$')) return true;

        if(IgnoredPaths.Contains(info.FullName)) return true;
        if(info is FileInfo fileInfo) {
            if(IgnoredFileExtensions.Contains(fileInfo.Extension)) return true;
            if(IgnoredFileNames.Contains(fileInfo.Name)) return true;
        }
        if(info is DirectoryInfo directoryInfo) {
            if(IgnoredFolderNames.Contains(directoryInfo.Name)) return true;
        }
        foreach(var prefix in IgnoredPrefixes) {
            if(info.Name.StartsWith(prefix)) return true;
        }

        return false;
    }

    public BlacklistBackupConfig IgnoreFolders(params string[] folderName) {
        IgnoredFolderNames.AddRange(folderName);
        return this;
    }
    public BlacklistBackupConfig IgnorePaths(params string[] path) {
        IgnoredPaths.AddRange(path);
        return this;
    }
    public BlacklistBackupConfig IgnoreExtensions(params string[] extensions) {
        IgnoredFileExtensions.AddRange(extensions);
        return this;
    }
    public BlacklistBackupConfig IgnoreFiles(params string[] fileNames) {
        IgnoredFileNames.AddRange(fileNames);
        return this;
    }
    public BlacklistBackupConfig IgnorePrefix(params string[] keywords) {
        IgnoredPrefixes.AddRange(keywords);
        return this;
    }
}

public interface IBackupConfig {
    public DirectoryInfo OriginInfo { get; }
    public List<DirectoryInfo> BackupInfos { get; }
    public FileInfo ConfigFileInfo { get; }
    //public FileInfo MetaDataFileInfo { get; }

    public bool ShouldInclude(FileSystemInfo info);
    public bool ShouldExclude(FileSystemInfo info);

    public virtual FileInfo GetFileInfo(string relativePath) => new(Path.Combine(OriginInfo.FullName, relativePath));
    public virtual DirectoryInfo GetFolderInfo(string relativePath) => new(Path.Combine(OriginInfo.FullName, relativePath));
    public virtual IEnumerable<FileInfo> GetBackupFiles(string relativePath) {
        foreach(var backupInfo in BackupInfos) {
            if(!backupInfo.Exists) continue;

            yield return new FileInfo(Path.Combine(backupInfo.FullName, relativePath));
        }
    }

    public virtual IEnumerable<DirectoryInfo> GetDirectories() => GetValidDirectories(OriginInfo);
    public virtual IEnumerable<FileInfo> GetFiles() => GetDirectories().SelectMany(GetValidFiles);
    public virtual IEnumerable<DirectoryInfo> GetBackupDirectories() {
        foreach(var backupInfo in BackupInfos) {
            backupInfo.Refresh();
            if(!backupInfo.Exists) continue;
            foreach(var folder in backupInfo.EnumerateDirectories("*", SearchOption.AllDirectories)) {
                yield return folder;
            }
        }
    }
    public virtual IEnumerable<FileInfo> GetBackupFiles() {
        foreach(var backupInfo in BackupInfos) {
            if(!backupInfo.Exists) continue;
            foreach(var file in backupInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {
                yield return file;
            }
        }
    }
    protected virtual IEnumerable<DirectoryInfo> GetValidDirectories(DirectoryInfo directoryInfo) {
        yield return directoryInfo;
        foreach(var directory in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)) {
            if(ShouldExclude(directory)) continue;

            foreach(var subDirectory in GetValidDirectories(directory)) {
                yield return subDirectory;
            }
        }
    }
    protected virtual IEnumerable<FileInfo> GetValidFiles(DirectoryInfo directoryInfo) {
        foreach(var file in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly)) {
            if(ShouldExclude(file)) continue;
            yield return file;
        }
    }

    public virtual void ForeachBackup(Action<DirectoryInfo> action) {
        foreach(var backupInfo in BackupInfos) {
            if(!backupInfo.Exists) continue;
            action(backupInfo);
        }
    }

    public virtual void AddBackupLocation(string path) {
        var info = new DirectoryInfo(Path.Combine(path, "_Storage"));
        if(!info.Exists) info.Create();
        BackupInfos.Add(info);
    }
}