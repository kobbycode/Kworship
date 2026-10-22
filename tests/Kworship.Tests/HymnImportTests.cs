using System.Text;
using System.Text.Json;
using Kworship.Application;
using Kworship.Domain;
using Kworship.Infrastructure.Sqlite;
using Xunit;
namespace Kworship.Tests;

public sealed class HymnImportTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"Kworship-hymns-"+Guid.NewGuid());
    private LibraryStore Store=>new(Path.Combine(directory,"library.db"));
    private static HymnImportRow Row(string title="Original hymn")=>new(title,"[Verse 1]\nOriginal verse words\n\n[Chorus]\nOriginal chorus words","Test author","Test credit","Verse 1, Chorus, Verse 1","Example hymnal","12");
    private static IReadOnlyList<Song> Read(string text,string extension=".json")
    {using var stream=new MemoryStream(Encoding.UTF8.GetBytes(text));return HymnImport.Read(stream,extension);}
    private static IReadOnlyList<Song> Songs(params HymnImportRow[] rows)=>Read(JsonSerializer.Serialize(rows));
    [Fact] public void JsonPreservesNumberedTitleCreditsAndRepeatedArrangement()
    {
        var song=Assert.Single(Songs(Row()));Assert.Equal("Example hymnal #12 — Original hymn",song.Title);
        Assert.Equal("Test author",song.Author);Assert.Equal("Test credit",song.Copyright);
        Assert.Equal(3,SongPresentation.Create(song with{Revision=1}).Slides.Length);
    }
    [Fact] public void CsvSupportsBomQuotedCommasEscapedQuotesAndMultilineLyrics()
    {
        var songs=Read("\uFEFFtitle,lyrics,author\r\n\"A, hymn\",\"[Verse 1]\r\nSay \"\"hello\"\"\r\n\r\n[Chorus]\r\nSing again\",Writer\r\n", ".csv");
        var song=Assert.Single(songs);Assert.Equal("A, hymn",song.Title);Assert.Contains("Say \"hello\"",song.Lyrics);
        Assert.Equal(2,SongPresentation.Create(song with{Revision=1}).Slides.Length);
    }
    [Theory]
    [InlineData("title,lyrics,unknown\nA,B,C")]
    [InlineData("title,title,lyrics\nA,B,C")]
    [InlineData("title\nA")]
    [InlineData("title,lyrics\nA,B,C")]
    [InlineData("title,lyrics")]
    public void InvalidCsvRejected(string text)=>Assert.Throws<ArgumentException>(()=>Read(text,".csv"));
    [Fact] public void MalformedCsvRejected()=>Assert.Throws<ArgumentException>(()=>Read("title,lyrics\nA,\"unclosed",".csv"));
    [Theory] [InlineData("[]")] [InlineData("[null]")] [InlineData("[{\"title\":\"A\"}]")] [InlineData("null")]
    public void MissingHymnsOrRequiredFieldsRejected(string text)=>Assert.Throws<ArgumentException>(()=>Read(text));
    [Fact] public void UnknownJsonFieldsRejected()=>Assert.Throws<JsonException>(()=>Read("[{\"title\":\"A\",\"lyrics\":\"B\",\"lyrcs\":\"typo\"}]"));
    [Theory] [InlineData("", "1")] [InlineData("Example hymnal", "")]
    public void NumberAndHymnalMustBePaired(string hymnal,string number)=>Assert.Throws<ArgumentException>(()=>Songs(Row() with{Hymnal=hymnal,Number=number}));
    [Fact] public void BrokenArrangementRejectsWholeFile()=>Assert.Throws<ArgumentException>(()=>Songs(Row(),Row("Bad") with{Arrangement="Missing verse"}));
    [Fact] public void TooManyHymnsRejected()=>Assert.Throws<ArgumentException>(()=>Songs(Enumerable.Repeat(Row(),HymnImport.MaximumSongs+1).ToArray()));
    [Fact] public void OversizedFileRejected(){using var stream=new MemoryStream(new byte[HymnImport.MaximumBytes+1]);Assert.Throws<ArgumentException>(()=>HymnImport.Read(stream,".json"));}
    [Fact] public void InvalidUtf8Rejected(){using var stream=new MemoryStream([0xff,0xff]);Assert.Throws<DecoderFallbackException>(()=>HymnImport.Read(stream,".json"));}
    [Fact] public void ImportSkipsExistingAndWithinBatchDuplicatesWithoutOverwriting()
    {
        var store=Store;var first=Songs(Row())[0];var saved=store.SaveSong(first,0);
        var rows=Songs(Row(),Row("Other"),Row("Other"));var result=store.ImportSongs(rows);
        Assert.Equal(new HymnImportResult(1,2),result);Assert.Equal(2,store.ListSongs().Count);Assert.Equal(saved,store.GetSong(saved.Id));
        Assert.Equal(new HymnImportResult(0,3),store.ImportSongs(rows));
    }
    [Fact] public void DifferentLyricsOrCreditsWithSameTitleAreKeptSeparately()
    {
        var store=Store;var songs=Songs(Row(),Row() with{Copyright="Different permission"},Row() with{Lyrics="Different original words",Arrangement=""});
        Assert.Equal(3,store.ImportSongs(songs).Added);Assert.Equal(3,store.ListSongs().Count);
    }
    [Fact] public void FailedInsertRollsBackEarlierInserts()
    {
        var store=Store;var existing=store.SaveSong(Songs(Row())[0],0);
        var candidates=Songs(Row("New"),Row("Conflicting ID")).ToArray();candidates[1]=candidates[1] with{Id=existing.Id};
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>store.ImportSongs(candidates));
        Assert.Equal(existing,Assert.Single(store.ListSongs()));
    }
    [Fact] public async Task ImportPreservesDraftLiveAndServiceAndImportedHymnCanBeScheduled()
    {
        var store=Store;var saved=store.SaveSong(Songs(Row("Live"))[0],0);var vm=new WorkspaceViewModel(store,new Output());await vm.InitializeAsync();
        vm.PreviewSong(saved);vm.Dispatch(new TakePreview());vm.Dispatch(new SetBlack(false));var live=vm.State.Live;
        vm.NewSong();vm.SongTitle="Unsaved draft";vm.Lyrics="Original draft";vm.AddSong(saved);
        var imports=Songs(Row("Imported"));await vm.ImportHymnsAsync(imports);
        Assert.Same(live,vm.State.Live);Assert.False(vm.State.IsBlack);Assert.True(vm.IsSongDirty);Assert.Equal("Unsaved draft",vm.SongTitle);Assert.Single(vm.Entries);
        vm.AddSong(store.GetSong(imports[0].Id));await vm.SaveScheduleAsync();
        var reloaded=new WorkspaceViewModel(store,new Output());await reloaded.InitializeAsync();await reloaded.OpenScheduleAsync(store.GetSchedule(vm.CurrentScheduleId));
        reloaded.SelectedScheduleIndex=1;reloaded.PreviewEntry();Assert.Contains("Original verse words",reloaded.PreviewText);
        Assert.Contains("Test credit",reloaded.State.Preview!.Item.Credit);
    }
    [Fact] public void EquivalentCaseAndNewlinesHaveSameFingerprint()
    {
        var song=Songs(Row())[0];Assert.Equal(HymnImport.Fingerprint(song),HymnImport.Fingerprint(song with{Lyrics="  "+song.Lyrics.ToUpperInvariant().Replace("\n","\r\n")+"  "}));
    }
    private sealed class Output:IPresentationOutput{public void Present(PresentationState state){}}
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
