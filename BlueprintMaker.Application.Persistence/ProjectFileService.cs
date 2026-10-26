using System.Text;

namespace BlueprintMaker.Application.Persistence;

public static class ProjectFileService
{
    public const string Extension = ".bpmaker";

    public static string AutosavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BlueprintMaker", "autosave" + Extension);

    /// <summary>Запись через временный файл: при сбое старый файл остаётся целым.</summary>
    public static void Save(string path, ProjectFileDto dto)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        var tmp = full + ".tmp";
        File.WriteAllText(tmp, ProjectSerializer.ToJson(dto), Encoding.UTF8);

        if (File.Exists(full)) File.Replace(tmp, full, null);
        else File.Move(tmp, full);
    }

    public static ProjectFileDto Load(string path) =>
        ProjectSerializer.FromJson(File.ReadAllText(path, Encoding.UTF8));

    public static void DeleteAutosave()
    {
        try { if (File.Exists(AutosavePath)) File.Delete(AutosavePath); } catch { }
    }
}