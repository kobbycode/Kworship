using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
namespace Kworship.Application;

public sealed record LyricSection(string Label,string Text);
public static class SongStructure
{
    public static IReadOnlyList<LyricSection> Parse(string lyrics) => Regex.Split(lyrics.Replace("\r\n","\n"),@"\n[ \t]*\n+")
        .Select(block=>block.Trim()).Where(block=>block.Length>0).Select((block,index)=>
        {
            var lines=block.Split('\n');
            var labeled=lines[0].StartsWith('[')&&lines[0].EndsWith(']')&&lines.Length>1;
            return new LyricSection(labeled?lines[0][1..^1]:$"Slide {index+1}",labeled?string.Join('\n',lines.Skip(1)).Trim():block);
        }).ToArray();
    public static IReadOnlyList<LyricSection> Arrange(IReadOnlyList<LyricSection> sections,string arrangement)
    {
        if(string.IsNullOrWhiteSpace(arrangement))return sections;
        var tokens=arrangement.Split([',','\n','\r'],StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(tokens.Length==0)throw new ArgumentException("The play order must contain section labels, or be left empty.");
        return tokens.Select(label=>
        {
            var found=sections.Where(section=>section.Label.Equals(label,StringComparison.OrdinalIgnoreCase)).ToArray();
            if(found.Length!=1)throw new ArgumentException($"Play order section '{label}' must match exactly one section label.");
            return found[0];
        }).ToArray();
    }
}
public sealed class EditableSection(string label,string text):INotifyPropertyChanged
{
    private string label=label,text=text;
    public string Label{get=>label;set{label=value;PropertyChanged?.Invoke(this,new(nameof(Label)));}}
    public string Text{get=>text;set{text=value;PropertyChanged?.Invoke(this,new(nameof(Text)));}}
    public event PropertyChangedEventHandler? PropertyChanged;
}
public sealed class StructuredSongEditor
{
    private readonly Song source;
    public string Title{get;set;}
    public string Author{get;set;}
    public string Copyright{get;set;}
    public ObservableCollection<EditableSection> Sections{get;}=[];
    public ObservableCollection<EditableSection> Order{get;}=[];
    public StructuredSongEditor(Song source)
    {
        this.source=source;Title=source.Title;Author=source.Author;Copyright=source.Copyright;
        foreach(var section in SongStructure.Parse(source.Lyrics))Sections.Add(new(section.Label,section.Text));
        if(!string.IsNullOrWhiteSpace(source.Arrangement))
            foreach(var section in SongStructure.Arrange(SongStructure.Parse(source.Lyrics),source.Arrangement))
                Order.Add(Sections.Single(item=>item.Label.Equals(section.Label,StringComparison.OrdinalIgnoreCase)));
    }
    public EditableSection AddSection(string kind)
    {
        var number=1;var label=kind=="Verse"?"Verse 1":kind;
        while(Sections.Any(section=>section.Label.Equals(label,StringComparison.OrdinalIgnoreCase)))label=kind+" "+(++number);
        var section=new EditableSection(label,"");Sections.Add(section);return section;
    }
    public void RemoveSection(EditableSection section)
    {
        if(Order.Contains(section))throw new InvalidOperationException("Remove this section from the play order before deleting it.");
        Sections.Remove(section);
    }
    public Song Build()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        if(Sections.Count==0)throw new ArgumentException("Add at least one section.");
        foreach(var section in Sections)
        {
            if(string.IsNullOrWhiteSpace(section.Label)||section.Label.IndexOfAny([',','\n','\r','[',']'])>=0)
                throw new ArgumentException("Section labels cannot be empty or contain commas, brackets or newlines.");
            ArgumentException.ThrowIfNullOrWhiteSpace(section.Text);
            if(Regex.IsMatch(section.Text,@"\r?\n[ \t]*\r?\n"))throw new ArgumentException("Use a separate section/slide instead of blank lines inside a section.");
        }
        if(Sections.Select(section=>section.Label.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Sections.Count)
            throw new ArgumentException("Give every section a unique label before saving a structured song.");
        if(Order.Any(section=>!Sections.Contains(section)))throw new ArgumentException("Play order references a removed section.");
        return source with {Title=Title.Trim(),Author=Author.Trim(),Copyright=Copyright.Trim(),
            Lyrics=string.Join("\n\n",Sections.Select(section=>$"[{section.Label.Trim()}]\n{section.Text.Trim()}")),
            Arrangement=string.Join(", ",Order.Select(section=>section.Label.Trim()))};
    }
}
