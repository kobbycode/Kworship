using System.Text;
using System.Xml;
using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Xunit;
namespace Kworship.Tests;

public sealed class ScriptureTests
{
    private const string Xml = """
    <XMLBIBLE biblename="Test translation"><BIBLEBOOK bname="Test book" bnumber="1"><CHAPTER cnumber="1">
    <VERS vnumber="1">Original <STYLE>test words</STYLE>.<NOTE>Do not project notes</NOTE><BR/>Next line.</VERS>
    <VERS vnumber="2">Second original test verse.</VERS></CHAPTER></BIBLEBOOK></XMLBIBLE>
    """;
    private static MemoryStream Stream(string xml = Xml) => new(Encoding.UTF8.GetBytes(xml));
    [Fact] public void PassagePreservesReferenceAndCreditsWithoutStudyNotes()
    {
        using var stream = Stream(); var bible = ZefaniaBible.Read(stream,"Test content, authored for tests");
        var song = bible.Passage("Test book",1,1,2); var presentation = SongPresentation.Create(song with { Revision = 1 });
        Assert.Equal(2,presentation.Slides.Length);
        Assert.Contains("Test book 1:1",presentation.Slides[0].Text);
        Assert.Contains("Original test words. Next line.",presentation.Slides[0].Text);
        Assert.DoesNotContain("Do not project",presentation.Slides[0].Text);
        Assert.Contains("Test content",presentation.Credit);
    }
    [Theory] [InlineData(0,1)] [InlineData(2,1)] [InlineData(1,3)] [InlineData(1,101)]
    public void MissingAndInvalidRangesAreRejected(int first,int last)
    { using var stream = Stream(); Assert.Throws<ArgumentException>(() => ZefaniaBible.Read(stream,"Test").Passage("Test book",1,first,last)); }
    [Fact] public void DuplicateAddressesRejectedBeforePersistence()
    { using var stream = Stream(Xml.Replace("vnumber=\"2\"","vnumber=\"1\"")); Assert.Throws<ArgumentException>(() => ZefaniaBible.Read(stream,"Test")); }
    [Fact] public void ExternalEntitiesAndDoctypesAreRejected()
    { using var stream = Stream("<!DOCTYPE XMLBIBLE [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><XMLBIBLE biblename='Test'>&x;</XMLBIBLE>"); Assert.Throws<XmlException>(() => ZefaniaBible.Read(stream,"Test")); }
    [Fact] public void ImportPersistsOfflineAndFailedImportLeavesExistingFilesAlone()
    {
        var directory = Path.Combine(Path.GetTempPath(),"Kworship-bible-test-"+Guid.NewGuid());
        try
        {
            var files = new BibleFiles(directory); using var stream = Stream();
            var path = files.Import(stream,"Test attribution");
            Assert.Equal("Test translation",files.Load(path).Title);
            using var invalid = Stream("<invalid/>"); Assert.Throws<ArgumentException>(() => files.Import(invalid,"Test"));
            Assert.Single(files.List()); Assert.Equal(2,files.Load(path).Verses.Count);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
    [Fact] public void BundledAsvHasExpectedStructureReferenceAndOmissionBehavior()
    {
        var bible = new BibleFiles("").Load(Path.Combine(AppContext.BaseDirectory,"asv.bible.json"));
        Assert.Equal(66,bible.Verses.Select(v=>v.Book).Distinct().Count());
        Assert.Equal(1189,bible.Verses.Select(v=>(v.Book,v.Chapter)).Distinct().Count());
        Assert.Equal(31086,bible.Verses.Count);
        var passage=bible.Passage("John",3,16,16);
        Assert.Contains("For God so loved the world",passage.Lyrics);
        Assert.Contains("Public domain",passage.Copyright);
        Assert.Throws<ArgumentException>(()=>bible.Passage("John",5,3,5));
        Assert.Contains("The grace of the Lord Jesus",bible.Passage("Revelation of John",22,21,21).Lyrics);
    }
    [Fact] public void AttributionRequired()
    { using var stream = Stream(); Assert.Throws<ArgumentException>(() => ZefaniaBible.Read(stream," ")); }
}
