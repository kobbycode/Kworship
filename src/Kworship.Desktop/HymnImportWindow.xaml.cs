using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Kworship.Application;
using Microsoft.Win32;
namespace Kworship.Desktop;

public partial class HymnImportWindow : Window
{
    private readonly IReadOnlyList<Song> existing;
    private IReadOnlyList<Song> candidates=[];
    public IReadOnlyList<Song>? Result { get;private set; }
    public HymnImportWindow(IReadOnlyList<Song> existing)
    { InitializeComponent();this.existing=existing; }
    private async void ChooseFile(object sender,RoutedEventArgs e)
    {
        var picker=new OpenFileDialog{Filter="Hymn collections (*.csv;*.json)|*.csv;*.json",CheckFileExists=true};
        if(picker.ShowDialog(this)==true)await LoadSource(picker.FileName);
    }
    internal async Task LoadSource(string path)
    {
        ChooseHymnsButton.IsEnabled=ConfirmHymnImportButton.IsEnabled=false;
        candidates=[];HymnCandidates.ItemsSource=null;HymnPreview.Text="Reading collection…";HymnCredit.Text="";
        try
        {
            candidates=await Task.Run(()=>{using var stream=File.OpenRead(path);return HymnImport.Read(stream,Path.GetExtension(path));});
            var fingerprints=existing.Select(HymnImport.Fingerprint).ToHashSet();
            var additions=candidates.Count(song=>fingerprints.Add(HymnImport.Fingerprint(song)));
            ImportSummary.Text=$"{Path.GetFileName(path)} · {candidates.Count} hymns · {additions} new · {candidates.Count-additions} exact duplicates to skip";
            HymnCandidates.ItemsSource=candidates;HymnCandidates.SelectedIndex=0;
            ConfirmHymnImportButton.IsEnabled=additions>0;
            ImportMessage.Text=additions==0?"All hymns are already present. No import is needed.":"Review the lyrics and credits, then Import collection. All new hymns are saved together; live output is unchanged.";
        }
        catch(Exception error)
        {
            App.Log("hymn_file_read_failed",error);ImportSummary.Text="File rejected. Nothing was imported.";HymnPreview.Text="Choose another file or correct the source.";
            ImportMessage.Text=error is ArgumentException or JsonException or System.Text.DecoderFallbackException
                ?error.Message:"Could not read that file. Check its location and permissions.";
        }
        finally{ChooseHymnsButton.IsEnabled=true;}
    }
    private void SelectionChanged(object sender,SelectionChangedEventArgs e)
    {
        if(HymnCandidates.SelectedItem is not Song song)return;
        HymnPreview.Text=song.Title+"\n\n"+song.Lyrics;
        HymnCredit.Text=string.Join(" · ",new[]{song.Author,song.Copyright}.Where(s=>s.Length>0));
    }
    private void ConfirmImport(object sender,RoutedEventArgs e)
    { if(candidates.Count==0||!ConfirmHymnImportButton.IsEnabled)return;Result=candidates;DialogResult=true; }
    private void SaveExample(object sender,RoutedEventArgs e)
    {
        var picker=new SaveFileDialog{FileName="Kworship-hymn-example.json",Filter="JSON hymn collection (*.json)|*.json",OverwritePrompt=true};
        if(picker.ShowDialog(this)!=true)return;
        try
        {
            var example=new[]{new HymnImportRow("Original example hymn","[Verse 1]\nWe gather here in hope today\n\n[Chorus]\nTogether we will sing", "Kworship example", "Original example text; replace with authorised lyrics", "Verse 1, Chorus, Verse 1", "Example hymnal", "1")};
            File.WriteAllText(picker.FileName,JsonSerializer.Serialize(example,new JsonSerializerOptions{WriteIndented=true}));
            ImportMessage.Text="Example saved. Replace the example words and metadata with your authorised collection, then choose the file.";
        }
        catch(Exception error){App.Log("hymn_example_write_failed",error);ImportMessage.Text="Could not save the example file.";}
    }
}
