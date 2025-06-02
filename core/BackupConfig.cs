using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BackupConfig
{
    public DirectoryInfo OriginDirectory { get; }
    public List<DirectoryInfo> BackupDirectories { get; } = [];
    [JsonIgnore] public FileInfo ConfigFileInfo { get; }

    public List<string> IgnoredPaths { get; } = [];
    public List<string> IgnoredFileExtensions { get; } = [];
    public List<string> IgnoredPrefixes { get; } = [];
    public List<string> IgnoredFolderNames { get; } = [];
    public List<string> IgnoredFileNames { get; } = [];

    // backwards compat (when removing, also remove constructor arguments)
    public DirectoryInfo OriginInfo { set { } }
    public List<DirectoryInfo> BackupInfos { set { } }

    private BackupConfig(DirectoryInfo originDirectory)
    {
        OriginDirectory = originDirectory;
        ConfigFileInfo = OriginDirectory.File("backup_config.json");
    }

    [JsonConstructor]
    public BackupConfig(DirectoryInfo originDirectory, List<DirectoryInfo> backupDirectories, List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredPrefixes, List<string> ignoredFolderNames, List<string> ignoredFileNames, List<DirectoryInfo> backupInfos, DirectoryInfo originInfo = null) :
        this(originDirectory ?? originInfo ?? throw new ArgumentNullException(nameof(originDirectory)))
    {
        BackupDirectories = Guard.ThrowIfNullOrEmpty(backupDirectories ?? backupInfos);
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