using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BackupConfig
{
    public DirectoryInfo OriginInfo { get; }
    public List<DirectoryInfo> BackupInfos { get; } = [];
    [JsonIgnore] public FileInfo ConfigFileInfo { get; }

    public List<string> IgnoredPaths { get; } = [];
    public List<string> IgnoredFileExtensions { get; } = [];
    public List<string> IgnoredPrefixes { get; } = [];
    public List<string> IgnoredFolderNames { get; } = [];
    public List<string> IgnoredFileNames { get; } = [];

    private BackupConfig(DirectoryInfo originInfo)
    {
        OriginInfo = originInfo;
        ConfigFileInfo = new(Path.Combine(OriginInfo.FullName, "backup_config.json"));
    }

    [JsonConstructor]
    public BackupConfig(DirectoryInfo originInfo, List<DirectoryInfo> backupInfos, List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredPrefixes, List<string> ignoredFolderNames, List<string> ignoredFileNames) :
        this(originInfo)
    {
        BackupInfos = backupInfos;
        IgnoredPaths = ignoredPaths;
        IgnoredFileExtensions = ignoredFileExtensions;
        IgnoredPrefixes = ignoredPrefixes;
        IgnoredFolderNames = ignoredFolderNames;
        IgnoredFileNames = ignoredFileNames;
    }

    public static BackupConfig Create(string originPath, string backupPath)
    {
        var config = new BackupConfig(new(originPath));

        config.AddBackupLocation(backupPath);

        if (!config.ConfigFileInfo.Exists)
        {
            config.ConfigFileInfo.Create().Dispose();
        }

        config.IgnoreFolders("System Volume Information", ".git");
        config.IgnoreExtensions(".blend1");
        config.IgnoreExtensions(".deleted");
        config.IgnoreExtensions(".old");
        config.IgnoreExtensions(".tmp");
        config.IgnorePrefix("$");

        return config;
    }

    public bool ShouldInclude(FileSystemInfo info) => !ShouldExclude(info);

    public bool ShouldExclude(FileSystemInfo info)
    {
        if (IgnoredPaths.Contains(info.FullName))
        {
            return true;
        }

        if (info is FileInfo fileInfo && (IgnoredFileExtensions.Contains(fileInfo.Extension) || IgnoredFileNames.Contains(fileInfo.Name)))
        {
            return true;
        }

        if (info is DirectoryInfo directoryInfo && IgnoredFolderNames.Contains(directoryInfo.Name))
        {
            return true;
        }

        foreach (var prefix in IgnoredPrefixes)
        {
            if (info.Name.StartsWith(prefix))
            {
                return true;
            }
        }

        return false;
    }

    public BackupConfig IgnoreFolders(params ReadOnlySpan<string> folderName)
    {
        IgnoredFolderNames.AddRange(folderName);
        return this;
    }
    public BackupConfig IgnorePaths(params ReadOnlySpan<string> path)
    {
        IgnoredPaths.AddRange(path);
        return this;
    }
    public BackupConfig IgnoreExtensions(params ReadOnlySpan<string> extensions)
    {
        IgnoredFileExtensions.AddRange(extensions);
        return this;
    }
    public BackupConfig IgnoreFiles(params ReadOnlySpan<string> fileNames)
    {
        IgnoredFileNames.AddRange(fileNames);
        return this;
    }
    public BackupConfig IgnorePrefix(params ReadOnlySpan<string> keywords)
    {
        IgnoredPrefixes.AddRange(keywords);
        return this;
    }

    public void AddBackupLocation(string path)
    {
        var info = new DirectoryInfo(path);
        info.CreateIfNotExists();
        BackupInfos.Add(info);
    }
}