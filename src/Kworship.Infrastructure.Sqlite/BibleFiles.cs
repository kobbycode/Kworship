using System.Text.Json;
using Kworship.Application;
namespace Kworship.Infrastructure.Sqlite;

// Separate source library; selected passages are saved in the canonical SQLite library.
public sealed class BibleFiles(string directory)
{
    public IReadOnlyList<string> List() => Directory.Exists(directory) ? Directory.GetFiles(directory, "*.bible.json").Order().ToArray() : [];
    public BibleTranslation Load(string path)
    {
        if (new FileInfo(path).Length > 64_000_000) throw new InvalidDataException("Bible library file is too large.");
        var value = JsonSerializer.Deserialize<BibleTranslation>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid Bible library file.");
        if (string.IsNullOrWhiteSpace(value.Title) || string.IsNullOrWhiteSpace(value.Attribution) || value.Verses is null || value.Verses.Count is 0 or > 100000
            || value.Verses.Any(v => v is null || string.IsNullOrWhiteSpace(v.Book) || string.IsNullOrWhiteSpace(v.Text) || v.Chapter <= 0 || v.Number <= 0)
            || value.Verses.Select(v => (v.Book,v.Chapter,v.Number)).Distinct().Count() != value.Verses.Count)
            throw new InvalidDataException("Invalid Bible library data. Reimport the source XML.");
        return value;
    }
    public string Import(Stream xml, string attribution)
    {
        var bible = ZefaniaBible.Read(xml, attribution);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".bible.json");
        var temp = path + ".tmp";
        try
        {
            using (var stream = new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            { JsonSerializer.Serialize(stream,bible); stream.Flush(true); }
            File.Move(temp,path);
            return path;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
