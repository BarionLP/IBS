using Ametrin.Serialization;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BlacklistBackupConfig : IBackupConfig {
    [JsonConverter(typeof(DirectoryInfoJsonConverter))] public DirectoryInfo OriginInfo { get; }
    [JsonConverter(typeof(DirectoryInfoJsonConverter))] public DirectoryInfo BackupInfo { get; }
    [JsonConverter(typeof(FileInfoJsonConverter))] public FileInfo ConfigFileInfo { get; }

    public List<string> IgnoredPaths { get; } = new();
    public List<string> IgnoredFileExtensions { get; } = new();
    public List<string> IgnoredKeywords { get; } = new();
    public List<string> IgnoredFolderNames { get; } = new();
    public List<string> IgnoredFileNames { get; } = new();

    public BlacklistBackupConfig(string originPath, string backupPath) {
        OriginInfo = new(originPath);
        BackupInfo = new(Path.Combine(backupPath, "_Storage"));
        ConfigFileInfo = new(Path.Combine(backupPath, "config.json"));

        if(!BackupInfo.Exists) BackupInfo.Create();
        if(!ConfigFileInfo.Exists) ConfigFileInfo.Create();
    }

    [JsonConstructor]
    public BlacklistBackupConfig(DirectoryInfo originInfo, DirectoryInfo backupInfo, FileInfo configFileInfo, List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredKeywords, List<string> ignoredFolderNames, List<string> ignoredFileNames) {
        OriginInfo = originInfo;
        BackupInfo = backupInfo;
        ConfigFileInfo = configFileInfo;
        IgnoredPaths = ignoredPaths;
        IgnoredFileExtensions = ignoredFileExtensions;
        IgnoredKeywords = ignoredKeywords;
        IgnoredFolderNames = ignoredFolderNames;
        IgnoredFileNames = ignoredFileNames;
    }

    public IEnumerable<DirectoryInfo> GetDirectories() => GetValidDirectories(OriginInfo);
    public IEnumerable<FileInfo> GetFiles() => GetDirectories().SelectMany(GetValidFiles);

    public IEnumerable<DirectoryInfo> GetDirectoriesFromBackup() => BackupInfo.EnumerateDirectories("*", SearchOption.AllDirectories);
    public IEnumerable<FileInfo> GetFilesFromBackup() => BackupInfo.EnumerateFiles("*", SearchOption.AllDirectories);

    public IEnumerable<DirectoryInfo> GetValidDirectories(DirectoryInfo directoryInfo) {
        yield return directoryInfo;
        foreach(var directory in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)) {
            if(IsIgnored(directory)) {
                Trace.TraceInformation("Ignored {0}", directory.FullName);
                continue;
            }

            foreach(var subDirectory in GetValidDirectories(directory)) {
                yield return subDirectory;
            }
        }
    }

    public IEnumerable<FileInfo> GetValidFiles(DirectoryInfo directoryInfo) {
        foreach(var file in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly)) {
            if(IsIgnored(file)) continue;
            yield return file;
        }
    }

    public bool IsIgnored(FileSystemInfo info) {
        if(info.Name.StartsWith('$')) return true;
        if(info.Name == "System Volume Information") return true;
        if(IgnoredPaths.Contains(info.FullName)) return true;
        if(info is FileInfo fileInfo) {
            if(IgnoredFileExtensions.Contains(fileInfo.Extension)) return true;
            if(IgnoredFileNames.Contains(fileInfo.Name)) return true;
        }
        if(info is DirectoryInfo directoryInfo) {
            if(IgnoredFolderNames.Contains(directoryInfo.Name)) return true;
        }
        foreach(var keyword in IgnoredKeywords) {
            if(info.FullName.Contains(keyword)) return true;
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
    public BlacklistBackupConfig IgnoreKeywords(params string[] keywords) {
        IgnoredKeywords.AddRange(keywords);
        return this;
    }
}

public interface IBackupConfig {
    [JsonConverter(typeof(DirectoryInfoJsonConverter))] public DirectoryInfo OriginInfo { get; }
    [JsonConverter(typeof(DirectoryInfoJsonConverter))] public DirectoryInfo BackupInfo { get; }
    [JsonConverter(typeof(FileInfoJsonConverter))] public FileInfo ConfigFileInfo { get; }

    public IEnumerable<DirectoryInfo> GetDirectories();
    public IEnumerable<FileInfo> GetFiles();
    public IEnumerable<DirectoryInfo> GetDirectoriesFromBackup();
    public IEnumerable<FileInfo> GetFilesFromBackup();
}