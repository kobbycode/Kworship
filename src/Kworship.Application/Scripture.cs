using System.Xml;
using System.Xml.Linq;

namespace Kworship.Application;

public sealed record BibleVerse(string Book, int Chapter, int Number, string Text);
public sealed record BibleTranslation(string Title, string Attribution, IReadOnlyList<BibleVerse> Verses)
{
    public Song Passage(string book, int chapter, int first, int last)
    {
        if (first < 1 || last < first || last - (long)first > 99)
            throw new ArgumentException("Choose between 1 and 100 consecutive verses in one chapter.");
        var verses = Verses.Where(v => v.Book == book && v.Chapter == chapter && v.Number >= first && v.Number <= last).OrderBy(v => v.Number).ToArray();
        if (verses.Length != last - first + 1) throw new ArgumentException("The selected range contains verses missing from this translation.");
        var reference = $"{book} {chapter}:{first}" + (last == first ? "" : $"–{last}");
        // One verse per slide; the reference is part of the projected body.
        var lyrics = string.Join("\n\n", verses.Select(v => $"[{book} {chapter}:{v.Number}]\n{book} {chapter}:{v.Number}\n{v.Text}"));
        return new(Guid.NewGuid(), 0, $"{reference} — {Title}", lyrics, "", $"{Title} · {Attribution}");
    }
}

public static class ZefaniaBible
{
    public static BibleTranslation Read(Stream stream, string attribution)
    {
        if (string.IsNullOrWhiteSpace(attribution) || attribution.Length > 4000)
            throw new ArgumentException("Enter the translation's required attribution and licence/source information (up to 4,000 characters).");
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 32_000_000,
            IgnoreComments = true
        });
        var root = XDocument.Load(reader).Root;
        if (root?.Name != "XMLBIBLE") throw new ArgumentException("Expected a Zefania XML Bible with an XMLBIBLE root.");
        var title = ((string?)root.Attribute("biblename") ?? (string?)root.Element("INFORMATION")?.Element("title") ?? "").Trim();
        if (title.Length is 0 or > 200) throw new ArgumentException("The Bible file needs a title of 1–200 characters.");
        var verses = new List<BibleVerse>();
        var addresses = new HashSet<(string, int, int)>();
        var books = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var book in root.Elements("BIBLEBOOK"))
        {
            var name = ((string?)book.Attribute("bname") ?? "").Trim();
            if (name.Length is 0 or > 100 || name.IndexOfAny(['[', ']', '\r', '\n']) >= 0 || !books.Add(name))
                throw new ArgumentException("Each Bible book must have a unique, valid bname.");
            foreach (var chapter in book.Elements("CHAPTER"))
            {
                var c = Positive(chapter, "cnumber");
                foreach (var verse in chapter.Elements("VERS"))
                {
                    var n = Positive(verse, "vnumber");
                    // Preserve inline STYLE words but omit study notes and cross references.
                    var body = string.Join(" ", PlainText(verse).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                    if (body.Length is 0 or > 12000 || !addresses.Add((name,c,n)))
                        throw new ArgumentException("Bible verses must have unique addresses and 1–12,000 characters of text.");
                    verses.Add(new(name,c,n,body));
                }
            }
        }
        if (verses.Count is 0 or > 100000) throw new ArgumentException("The Bible file must contain 1–100,000 verses.");
        return new(title, attribution.Trim(), verses.AsReadOnly());
    }
    private static int Positive(XElement element, string name) => int.TryParse((string?)element.Attribute(name), out var value) && value is > 0 and <= 1000
        ? value : throw new ArgumentException($"Invalid {name} in Bible file.");
    private static string PlainText(XElement element) => string.Concat(element.Nodes().Select(node => node switch
    {
        XText text => text.Value,
        XElement child when child.Name.LocalName is "NOTE" or "XREF" => "",
        XElement child when child.Name.LocalName == "BR" => " ",
        XElement child => PlainText(child),
        _ => ""
    }));
}
