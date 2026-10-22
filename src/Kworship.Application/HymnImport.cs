using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Kworship.Application;

public sealed record HymnImportRow(string Title, string Lyrics, string Author = "", string Copyright = "",
    string Arrangement = "", string Hymnal = "", string Number = "");
public sealed record HymnImportResult(int Added, int Skipped);

public static class HymnImport
{
    public const int MaximumBytes = 20_000_000;
    public const int MaximumSongs = 2000;
    private static readonly string[] Columns = ["title","lyrics","author","copyright","arrangement","hymnal","number"];
    public static IReadOnlyList<Song> Read(Stream source, string extension)
    {
        using var buffer = new MemoryStream();
        var block = new byte[8192]; int count;
        while ((count = source.Read(block,0,block.Length)) > 0)
        {
            if (buffer.Length + count > MaximumBytes) throw new ArgumentException("Hymn files must be at most 20 MB.");
            buffer.Write(block,0,count);
        }
        var bytes = buffer.ToArray();
        var text = new UTF8Encoding(false,true).GetString(bytes).TrimStart('\uFEFF');
        IReadOnlyList<HymnImportRow> rows;
        switch(extension.ToLowerInvariant())
        {
            case ".json":
                rows = JsonSerializer.Deserialize<List<HymnImportRow>>(text,new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, MaxDepth=8
                }) ?? throw new ArgumentException("Expected a JSON array of hymns.");
                break;
            case ".csv": rows=ReadCsv(text); break;
            default: throw new ArgumentException("Choose a UTF-8 CSV or JSON hymn file.");
        }
        if(rows.Count is 0 or > MaximumSongs)throw new ArgumentException("Import between 1 and 2,000 hymns at a time.");
        return rows.Select((row,index)=>Convert(row,index+1)).ToArray();
    }
    private static IReadOnlyList<HymnImportRow> ReadCsv(string text)
    {
        var records=CsvRecords(text);
        var header = records.FirstOrDefault()?.Select(s=>s.Trim().ToLowerInvariant()).ToArray() ?? [];
        if(!header.Contains("title")||!header.Contains("lyrics")||header.Any(h=>!Columns.Contains(h))||header.Distinct().Count()!=header.Length)
            throw new ArgumentException("CSV needs unique title and lyrics columns; optional columns: author, copyright, arrangement, hymnal, number.");
        var result = new List<HymnImportRow>();
        foreach(var values in records.Skip(1))
        {
            if(values.Length!=header.Length)throw new ArgumentException($"CSV hymn {result.Count+1} has a different field count from the header.");
            string Get(string key){var i=Array.IndexOf(header,key);return i<0?"":values[i];}
            result.Add(new(Get("title"),Get("lyrics"),Get("author"),Get("copyright"),Get("arrangement"),Get("hymnal"),Get("number")));
        }
        return result;
    }
    private static IReadOnlyList<string[]> CsvRecords(string text)
    {
        var records=new List<string[]>();var fields=new List<string>();var field=new StringBuilder();
        bool quoted=false,closed=false;
        void EndField(){fields.Add(field.ToString());field.Clear();closed=false;}
        void EndRecord()
        {
            EndField();
            if(fields.Count>1||fields[0].Length>0)records.Add(fields.ToArray());
            fields.Clear();
            if(records.Count>MaximumSongs+1)throw new ArgumentException("Import at most 2,000 hymns at a time.");
        }
        for(int i=0;i<text.Length;i++)
        {
            var ch=text[i];
            if(quoted)
            {
                if(ch=='"')
                {
                    if(i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}
                    else{quoted=false;closed=true;}
                }
                else field.Append(ch);
            }
            else if(ch==',')EndField();
            else if(ch is '\r' or '\n'){EndRecord();if(ch=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;}
            else if(ch=='"'&&field.Length==0&&!closed)quoted=true;
            else if(closed||ch=='"')throw new ArgumentException("Malformed CSV quoting. Quote entire fields and double embedded quotes.");
            else field.Append(ch);
        }
        if(quoted)throw new ArgumentException("Unclosed quoted field in CSV.");
        if(field.Length>0||fields.Count>0||closed)EndRecord();
        return records;
    }
    private static Song Convert(HymnImportRow? row,int index)
    {
        try
        {
            if(row is null)throw new ArgumentException("Hymn is null.");
            string Single(string? value,int max,string field,bool required=false)
            {
                if(value is null||value.Length>max||value.Any(char.IsControl)|| (required&&string.IsNullOrWhiteSpace(value)))
                    throw new ArgumentException($"Invalid {field} (maximum {max} characters, single line).");
                return value.Trim();
            }
            var title=Single(row.Title,300,"title",true);var author=Single(row.Author,1000,"author");var credit=Single(row.Copyright,4000,"copyright");
            var hymnal=Single(row.Hymnal,150,"hymnal");var number=Single(row.Number,30,"number");
            if((hymnal.Length==0)!=(number.Length==0))throw new ArgumentException("Supply both hymnal and number, or leave both empty.");
            if(row.Lyrics is null||row.Lyrics.Length is 0 or > 100000||string.IsNullOrWhiteSpace(row.Lyrics))throw new ArgumentException("Lyrics must contain 1–100,000 characters.");
            if(row.Arrangement is null||row.Arrangement.Length>10000)throw new ArgumentException("Invalid arrangement.");
            var song=new Song(Guid.NewGuid(),0,(hymnal.Length==0?"":$"{hymnal} #{number} — ")+title,
                row.Lyrics.Replace("\r\n","\n").Replace('\r','\n').Trim(),author,credit,row.Arrangement.Trim());
            _=SongPresentation.Create(song with{Revision=1});return song;
        }
        catch(ArgumentException error){throw new ArgumentException($"Hymn {index}: {error.Message}",error);}
    }
    // Case-insensitive metadata and equivalent newline/outer whitespace variants count as exact duplicates.
    public static string Fingerprint(Song song)
    {
        string Normalize(string value)=>value.Replace("\r\n","\n").Replace('\r','\n').Trim().ToUpperInvariant();
        var fields=new[]{song.Title,song.Lyrics,song.Author,song.Copyright,song.Arrangement}.Select(Normalize).ToArray();
        return System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fields))));
    }
}
