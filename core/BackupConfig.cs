using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BackupConfig
{
    public DirectoryInfo OriginDirectory { get; }
    public List<DirectoryInfo> BackupDirectories { get; } = [];
    public required FileInfo ConfigFileInfo { get; init; }
    public required FileInfo IgnoresFileInfo { get; init; }

    public List<string> IgnoredPaths { get; } = [];
    public List<string> IgnoredFileExtensions { get; } = [];
    public List<string> IgnoredPrefixes { get; } = [];
    public List<string> IgnoredFolderNames { get; } = [];
    public List<string> IgnoredFileNames { get; } = [];

    private BackupConfig(DirectoryInfo originDirectory)
    {
        OriginDirectory = originDirectory;
    }

    [JsonConstructor]
    public BackupConfig(DirectoryInfo originDirectory, List<DirectoryInfo> backupDirectories, List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredPrefixes, List<string> ignoredFolderNames, List<string> ignoredFileNames) :
        this(Guard.ThrowIfNull(originDirectory))
    {
        BackupDirectories = Guard.ThrowIfNullOrEmpty(backupDirectories);
        IgnoredPaths = ignoredPaths;
        IgnoredFileExtensions = ignoredFileExtensions;
        IgnoredPrefixes = ignoredPrefixes;
        IgnoredFolderNames = ignoredFolderNames;
        IgnoredFileNames = ignoredFileNames;
    }

    public static BackupConfig Create(string originPath, string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

        var origin = new DirectoryInfo(originPath);
        var config = new BackupConfig(origin)
        {
            ConfigFileInfo = origin.File("backup_config.json"),
            IgnoresFileInfo = origin.File("backup_ignores.json"),
        };

        config.AddBackupLocation(backupPath);

        if (!config.ConfigFileInfo.Exists)
        {
            config.ConfigFileInfo.Create().Dispose();
        }

        config.IgnoreFolders("System Volume Information", ".Trash-1000", ".git");
        config.IgnoreExtensions(".blend1", ".deleted", ".old", ".tmp");
        config.IgnoreFiles("desktop.ini");
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
        BackupDirectories.Add(info);
    }
}