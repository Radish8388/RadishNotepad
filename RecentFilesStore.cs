using System.IO;
using System.Text.Json;

namespace RadishNotepad
{
    public class RecentFilesStore
    {
        private readonly string _path;
        public List<string> Files { get; private set; } = new();

        public RecentFilesStore()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Radish", "Radish Notepad");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "recent-files.json");
            Load();
        }

        public void Load()
        {
            if (!File.Exists(_path)) return;
            try
            {
                Files = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_path)) ?? new();
            }
            catch (JsonException)
            {
                Files = new();   // corrupt file: start fresh rather than crash
            }
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Files));
        }

        public void Add(string filePath, int maxCount = 10)
        {
            Files.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
            Files.Insert(0, filePath);
            if (Files.Count > maxCount) Files.RemoveRange(maxCount, Files.Count - maxCount);
            Save();
        }
    }
}
