using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var ignores = configdto.IgnoresFileInfo is null || !configdto.IgnoresFileInfo.Exists ? new([], [], [], [], []) : JsonSerializer.Deserialize(configdto.IgnoresFileInfo, BackupJsonContext.Default.BackupIgnores);
        var config = MakeBackupConfig(configdto, ignores, fileInfo);

        var wasUpgrade = configdto.IgnoresFileInfo is null;

        if (!config.IgnoredFileNames.Contains("desktop.ini"))
        {
            config.IgnoredFileNames.Add("desktop.ini");
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

    private static BackupConfig MakeBackupConfig(BackupConfigDto config, BackupIgnores ignores, FileInfo source)
    {
        if (config.IgnoredPaths is not null)
        {
            ignores.IgnoredPaths.AddRange(config.IgnoredPaths);
        }
        if (config.IgnoredFileExtensions is not null)
        {
            ignores.IgnoredFileExtensions.AddRange(config.IgnoredFileExtensions);
        }
        if (config.IgnoredPrefixes is not null)
        {
            ignores.IgnoredPrefixes.AddRange(config.IgnoredPrefixes);
        }
        if (config.IgnoredFolderNames is not null)
        {
            ignores.IgnoredFolderNames.AddRange(config.IgnoredFolderNames);
        }
        if (config.IgnoredFolderNames is not null)
        {
            ignores.IgnoredFolderNames.AddRange(config.IgnoredFolderNames);
        }

        return new(config.OriginDirectory, config.BackupDirectories,
            ignoredPaths: [.. ignores.IgnoredPaths.Select(p => Path.Combine(config.OriginDirectory.FullName, p))],
            ignoredFileExtensions: ignores.IgnoredFileExtensions,
            ignoredPrefixes: ignores.IgnoredPrefixes,
            ignoredFolderNames: ignores.IgnoredFolderNames,
            ignoredFileNames: ignores.IgnoredFileNames
        )
        {
            ConfigFileInfo = source,
            IgnoresFileInfo = config.IgnoresFileInfo ?? config.OriginDirectory.File("backup_ignores.json"),
        };
    }

    internal sealed class BackupConfigDto
    {
        public DirectoryInfo OriginDirectory { get; }
        public List<DirectoryInfo> BackupDirectories { get; }
        public FileInfo? IgnoresFileInfo { get; }
        public List<string>? IgnoredPaths { get; }
        public List<string>? IgnoredFileExtensions { get; }
        public List<string>? IgnoredPrefixes { get; }
        public List<string>? IgnoredFolderNames { get; }
        public List<string>? IgnoredFileNames { get; }

        public BackupConfigDto(BackupConfig config)
        {
            OriginDirectory = config.OriginDirectory;
            BackupDirectories = config.BackupDirectories;
            IgnoresFileInfo = config.IgnoresFileInfo;
        }

        [JsonConstructor, EditorBrowsable(EditorBrowsableState.Never)]
        internal BackupConfigDto(DirectoryInfo originDirectory, List<DirectoryInfo> backupDirectories, FileInfo ignoresFileInfo, List<string>? ignoredPaths = null, List<string>? ignoredFileExtensions = null, List<string>? ignoredPrefixes = null, List<string>? ignoredFolderNames = null, List<string>? ignoredFileNames = null)
        {
            OriginDirectory = originDirectory;
            BackupDirectories = backupDirectories;
            IgnoresFileInfo = ignoresFileInfo;
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
        public List<string> IgnoredFileExtensions { get; }
        public List<string> IgnoredPrefixes { get; }
        public List<string> IgnoredFolderNames { get; }
        public List<string> IgnoredFileNames { get; }

        public BackupIgnores(BackupConfig config)
        {
            IgnoredPaths = [.. config.IgnoredPaths.Select(d => Path.GetRelativePath(config.OriginDirectory.FullName, d))];
            IgnoredFileExtensions = config.IgnoredFileExtensions;
            IgnoredPrefixes = config.IgnoredPrefixes;
            IgnoredFolderNames = config.IgnoredFolderNames;
            IgnoredFileNames = config.IgnoredFileNames;
        }

        [JsonConstructor, EditorBrowsable(EditorBrowsableState.Never)]
        internal BackupIgnores(List<string> ignoredPaths, List<string> ignoredFileExtensions, List<string> ignoredPrefixes, List<string> ignoredFolderNames, List<string> ignoredFileNames)
        {
            IgnoredPaths = ignoredPaths;
            IgnoredFileExtensions = ignoredFileExtensions;
            IgnoredPrefixes = ignoredPrefixes;
            IgnoredFolderNames = ignoredFolderNames;
            IgnoredFileNames = ignoredFileNames;
        }
    }
}