using System.Buffers.Text;
using System.Diagnostics;
using Ametrin.Optional;
using Ametrin.Utils;
using IBS.Core;
using IBS.Core.Serialization;

var origin = new DirectoryInfo(@"I:\Coding\TestChamber\IBS\Origin");
if (!origin.Exists)
{
    Console.WriteLine("❌: test directory not found!");
    return;
}

AskToResetGit(origin.Parent!);

var config = BackupConfigSerializer.Load(origin.File("backup_config.json")).OrThrow();

foreach (var backupDir in config.BackupDirectories)
{
    if (backupDir.Exists) continue;
    Console.WriteLine($"❌: Backup {backupDir} not found");
    return;
}


var backups = await Task.WhenAll(config.BackupDirectories.Select(BackupV2.CreateAsync));

AssertExistsWithContent(origin.File("tosync.txt"), "this file has been changed");
AssertExistsWithContent(origin.File("synced.txt"), "this file is in sync");
AssertExistsWithContent(origin.File("sub_folder/sub_file.md"), "yayay");
AssertExistsInBackup(backups[0], "deleted.txt");
AssertExistsInBackup(backups[0], "synced.txt");
AssertExistsInBackup(backups[1], "deleted_dir/file_in_deleted_dir.md");

await FileSyncer.SyncV2(config);

// refresh the backup states
backups = await Task.WhenAll(config.BackupDirectories.Select(BackupV2.CreateAsync));

AssertFileBackedUp(origin.File("tosync.txt"));
AssertFileBackedUp(origin.File("synced.txt"));
AssertFileBackedUp(origin.File("sub_folder/sub_file.md"));
AssertSoftDeleted(backups[0], "deleted.txt");
AssertSoftDeleted(backups[1], "deleted_dir/file_in_deleted_dir.md");

AssertAboutNow(backups[0].MetaData.LastWriteTime, $"{backups[0].Root} says it was not synced");
AssertAboutNow(backups[1].MetaData.LastWriteTime, $"{backups[1].Root} says it was not synced");

Console.WriteLine("✅: no further errors found");

ResetGit(origin.Parent!);

static bool AssertExists(FileSystemInfo fileInfo)
{
    if (fileInfo.Exists) return true;
    Console.WriteLine($"❌: {fileInfo.FullName} does not exists");
    return false;
}

static bool AssertExistsInBackup(BackupV2 backup, string path, bool deleted = false)
{
    if (backup.GetFile(path).Branch(out var node))
    {
        if (!deleted && node.Info.DeletedAt is not null)
        {
            Console.WriteLine($"❌: {path} is marked as deleted");
            return false;
        }
        
        if (node.GetFileInfo().Exists)
        {
            return true;
        }

        Console.WriteLine($"❌: {path} has not been copied");
    }
    else
    {
        Console.WriteLine($"❌: {path} does not exists");
    }
    return false;
}

static bool AssertNotExists(FileSystemInfo fileInfo)
{
    if (!fileInfo.Exists) return true;
    Console.WriteLine($"❌: {fileInfo.FullName} does exists");
    return false;
}

void AssertFileBackedUp(FileInfo fileInfo)
{
    if (!AssertExists(fileInfo)) return;

    var hash = fileInfo.ComputeSHA256Hash();
    var relativePath = fileInfo.GetRelativePath(origin);
    foreach (var backup in backups)
    {
        if (!AssertExistsInBackup(backup, relativePath)) continue;
        var node = backup.GetFile(relativePath).OrThrow();
        if (hash.SequenceEqual(Base64Url.DecodeFromChars(node.Info.GetLatest()!.Hash))) continue;
        Console.WriteLine($"❌: {relativePath} is not correctly backed up to {backup.Root}");
    }
}

static void AssertSoftDeleted(BackupV2 backup, string relativePath)
{
    if (backup.GetFile(relativePath).Branch(out var node))
    {
        if (node.Info.DeletedAt is DateTime deletedAt)
        {
            AssertAboutNow(deletedAt, $"{relativePath} was marked as deleted to long ago");
        }
        else
        {
            Console.WriteLine($"❌: {relativePath} was not marked as deleted");
        }
        AssertExistsInBackup(backup, relativePath, deleted: true);
    }
}

static void AssertExistsWithContent(FileInfo fileInfo, string expectedContent)
{
    if (!AssertExists(fileInfo)) return;
    var actualContent = File.ReadAllText(fileInfo.FullName);
    if (expectedContent == actualContent) return;
    Console.WriteLine($"❌: {fileInfo.FullName} content do not match!");
    Console.WriteLine($"\texpected: {expectedContent}");
    Console.WriteLine($"\t  actual: {actualContent}");
}

static void AssertAboutNow(DateTime dateTime, string message, TimeSpan? tolerance = null)
{
    tolerance ??= TimeSpan.FromMilliseconds(50);
    var difference = DateTime.Now - dateTime;
    if (difference > tolerance)
    {
        Console.WriteLine($"❌: {message} ({difference})");
    }
}

static void AskToResetGit(DirectoryInfo root)
{
    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = "status --porcelain",
        WorkingDirectory = root.FullName,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var process = Process.Start(psi)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (!string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("There are uncommitted changes in the git repository.");
        Console.Write("Do you want to discard them? (y/n): ");
        var response = Console.ReadKey();
        Console.WriteLine();
        if (response.Key is ConsoleKey.Y)
        {
            ResetGit(root);
            Console.WriteLine("All changes have been discarded.");
        }
    }
}

static void ResetGit(DirectoryInfo root)
{
    // Discard all changes including untracked files
    var resetPsi = new ProcessStartInfo
    {
        FileName = "cmd",
        Arguments = "/C git reset --hard && git clean -fd",
        WorkingDirectory = root.FullName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var resetProcess = Process.Start(resetPsi)!;
    resetProcess.WaitForExit();
}