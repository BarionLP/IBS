using System.Diagnostics;
using System.Text.Json.Serialization;

namespace IBS.Core;

public sealed class BackupConfig
{
    public DirectoryInfo OriginDirectory { get; }
    public List<DirectoryInfo> BackupDirectories { get; } = [];
    public required FileInfo ConfigFileInfo { get; init; }
    public required FileInfo IgnoresFileInfo { get; init; }

    public HashSet<string> IgnoredPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> IgnoredFileExtensions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> IgnoredPrefixes { get; } = [];
    public HashSet<string> IgnoredFolderNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> IgnoredFileNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    private BackupConfig(DirectoryInfo originDirectory)
    {
        OriginDirectory = originDirectory;
    }

    [JsonConstructor]
    public BackupConfig(DirectoryInfo originDirectory, List<DirectoryInfo> backupDirectories, HashSet<string> ignoredPaths, HashSet<string> ignoredFileExtensions, List<string> ignoredPrefixes, HashSet<string> ignoredFolderNames, HashSet<string> ignoredFileNames) :
        this(ThrowIf.Null(originDirectory))
    {
        BackupDirectories = ThrowIf.NullOrEmpty(backupDirectories);
        Debug.Assert(ignoredPaths.Comparer == StringComparer.OrdinalIgnoreCase);
        Debug.Assert(ignoredFileExtensions.Comparer == StringComparer.OrdinalIgnoreCase);
        Debug.Assert(ignoredFolderNames.Comparer == StringComparer.OrdinalIgnoreCase);
        Debug.Assert(ignoredFileNames.Comparer == StringComparer.OrdinalIgnoreCase);
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
        config.IgnoreFiles("desktop.ini", "bootTel.dat");
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

    public BackupConfig IgnorePaths(params ReadOnlySpan<string> path)
    {
        IgnoredPaths.UnionWith(path);
        return this;
    }
    public BackupConfig IgnoreFolders(params ReadOnlySpan<string> folderNames)
    {
        IgnoredFolderNames.UnionWith(folderNames);
        return this;
    }
    public BackupConfig IgnoreExtensions(params ReadOnlySpan<string> extensions)
    {
        IgnoredFileExtensions.UnionWith(extensions);
        return this;
    }
    public BackupConfig IgnoreFiles(params ReadOnlySpan<string> fileNames)
    {
        IgnoredFileNames.UnionWith(fileNames);
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