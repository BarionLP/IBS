using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ametrin.Optional.Nullable;
using Ametrin.Serialization;

namespace IBS.Core.Serialization;

public static class BackupConfigSerializer
{
    public static void Save(BackupConfig config)
    {
        JsonSerializer.SerializeToFile(config.ConfigFileInfo, new(config), BackupJsonContext.Default.BackupConfigDto, overwrite: true);
        JsonSerializer.SerializeToFile(config.IgnoresFileInfo, new(config), BackupJsonContext.Default.BackupIgnores, overwrite: true);
    }

    public static Result<BackupConfig> Load(FileInfo fileInfo)
    {
        var configdto = JsonSerializer.Deserialize(fileInfo, BackupJsonContext.Default.BackupConfigDto);
        var ignoreFileInfo = configdto.IgnoreInfoPath.Map(configdto.OriginDirectory.File);
        var ignores = ignoreFileInfo is null || !ignoreFileInfo.Exists ? new([], [], [], [], []) : JsonSerializer.Deserialize(ignoreFileInfo, BackupJsonContext.Default.BackupIgnores);
        var config = MakeBackupConfig(configdto, ignores, fileInfo);

        var wasUpgrade = configdto.IgnoreInfoPath is null;

        if (config.IgnoredFileNames.Add("desktop.ini"))
        {
            wasUpgrade = true;
        }
        if (config.IgnoredFileNames.Add("bootTel.dat"))
        {
            wasUpgrade = true;
        }

        foreach (var i in ..config.BackupDirectories.Count)
        {
            if (config.BackupDirectories[i].Name is "_Storage")
            {
                config.BackupDirectories[i] = config.BackupDirectories[i].Parent!;
                wasUpgrade = true;
            }
        }

        if (wasUpgrade)
        {
            Save(config);
        }

        return config;
    }

    private static string NormalizePath(string root, string path)
        => new FileInfo(Path.Combine(root, path)).FullName;

    private static BackupConfig MakeBackupConfig(BackupConfigDto config, BackupIgnores ignores, FileInfo source)
    {
        var created = new BackupConfig(config.OriginDirectory, config.BackupDirectories,
            ignoredPaths: ignores.IgnoredPaths.Select(p => NormalizePath(config.OriginDirectory.FullName, p)).ToHashSet(StringComparer.OrdinalIgnoreCase),
            ignoredFileExtensions: ignores.IgnoredFileExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase),
            ignoredPrefixes: ignores.IgnoredPrefixes,
            ignoredFolderNames: ignores.IgnoredFolderNames.ToHashSet(StringComparer.OrdinalIgnoreCase),
            ignoredFileNames: ignores.IgnoredFileNames.ToHashSet(StringComparer.OrdinalIgnoreCase)
        )
        {
            ConfigFileInfo = source,
            IgnoresFileInfo = config.IgnoreInfoPath is not null ? config.OriginDirectory.File(config.IgnoreInfoPath) : config.OriginDirectory.File("backup_ignore.json"),
        };

        if (config.IgnoredPaths is not null)
        {
            created.IgnoredPaths.UnionWith(config.IgnoredPaths.Select(p => NormalizePath(config.OriginDirectory.FullName, p)));
        }
        if (config.IgnoredFileExtensions is not null)
        {
            created.IgnoredFileExtensions.UnionWith(config.IgnoredFileExtensions);
        }
        if (config.IgnoredPrefixes is not null)
        {
            created.IgnoredPrefixes.AddRange(config.IgnoredPrefixes);
        }
        if (config.IgnoredFolderNames is not null)
        {
            created.IgnoredFolderNames.UnionWith(config.IgnoredFolderNames);
        }
        if (config.IgnoredFileNames is not null)
        {
            created.IgnoredFileNames.UnionWith(config.IgnoredFileNames);
        }

        return created;
    }

    internal sealed class BackupConfigDto
    {
        public DirectoryInfo OriginDirectory { get; }
        public List<DirectoryInfo> BackupDirectories { get; }
        public string? IgnoreInfoPath { get; }
        public string? IgnoresFileInfo { get; }
        public HashSet<string>? IgnoredPaths { get; }
        public HashSet<string>? IgnoredFileExtensions { get; }
        public List<string>? IgnoredPrefixes { get; }
        public HashSet<string>? IgnoredFolderNames { get; }
        public HashSet<string>? IgnoredFileNames { get; }

        public BackupConfigDto(BackupConfig config)
        {
            OriginDirectory = config.OriginDirectory;
            BackupDirectories = config.BackupDirectories;
            IgnoresFileInfo = config.IgnoresFileInfo.GetRelativePath(config.OriginDirectory);
        }

        [JsonConstructor, EditorBrowsable(EditorBrowsableState.Never)]
        internal BackupConfigDto(DirectoryInfo originDirectory, List<DirectoryInfo> backupDirectories, string? ignoreInfoPath, string? ignoresFileInfo, HashSet<string>? ignoredPaths = null, HashSet<string>? ignoredFileExtensions = null, List<string>? ignoredPrefixes = null, HashSet<string>? ignoredFolderNames = null, HashSet<string>? ignoredFileNames = null)
        {
            OriginDirectory = originDirectory;
            BackupDirectories = backupDirectories;
            IgnoreInfoPath = ignoreInfoPath ?? ignoresFileInfo;
            IgnoresFileInfo = null;
            IgnoredPaths = ignoredPaths;
            IgnoredFileExtensions = ignoredFileExtensions;
            IgnoredPrefixes = ignoredPrefixes;
            IgnoredFolderNames = ignoredFolderNames;
            IgnoredFileNames = ignoredFileNames;
        }
    }

    internal sealed class BackupIgnores
    {
        public List<string> IgnoredPaths { get; }
        public IEnumerable<string> IgnoredFileExtensions { get; }
        public List<string> IgnoredPrefixes { get; }
        public IEnumerable<string> IgnoredFolderNames { get; }
        public IEnumerable<string> IgnoredFileNames { get; }

        public BackupIgnores(BackupConfig config)
        {
            IgnoredPaths = [.. config.IgnoredPaths.Select(d => Path.GetRelativePath(config.OriginDirectory.FullName, d))];
            IgnoredFileExtensions = config.IgnoredFileExtensions;
            IgnoredPrefixes = config.IgnoredPrefixes;
            IgnoredFolderNames = config.IgnoredFolderNames;
            IgnoredFileNames = config.IgnoredFileNames;
        }

        [JsonConstructor, EditorBrowsable(EditorBrowsableState.Never)]
        internal BackupIgnores(List<string> ignoredPaths, IEnumerable<string> ignoredFileExtensions, List<string> ignoredPrefixes, IEnumerable<string> ignoredFolderNames, IEnumerable<string> ignoredFileNames)
        {
            IgnoredPaths = ignoredPaths;
            IgnoredFileExtensions = ignoredFileExtensions;
            IgnoredPrefixes = ignoredPrefixes;
            IgnoredFolderNames = ignoredFolderNames;
            IgnoredFileNames = ignoredFileNames;
        }
    }
}