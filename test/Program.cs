foreach(var file in Directory.EnumerateFiles("B:\\Backup\\Data\\_Storage", "*.deleted", SearchOption.AllDirectories))
{
    var f = file;
    while (f.EndsWith(".deleted"))
    {
        f = f[..(^".deleted".Length)];
    }

    Console.WriteLine(f);

    if (File.Exists(f))
    {
        File.Delete(file);
    }
    else
    {
        File.Move(file, f);
        Console.WriteLine(f);
    }
}