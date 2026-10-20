using System.IO;
using System.Windows;
using System.Windows.Controls;
using Kworship.Application;
using Kworship.Infrastructure.Sqlite;
using Microsoft.Win32;
namespace Kworship.Desktop;

public partial class ScriptureWindow : Window
{
    private sealed record TranslationFile(string Title, string Path);
    private readonly BibleFiles files;
    private BibleTranslation? bible;
    private Song? passage;
    public Song? Result { get; private set; }
    public ScriptureWindow(string directory)
    {
        InitializeComponent(); files = new(directory); Reload();
    }
    private void Reload(string? selected = null)
    {
        var entries = new List<TranslationFile>(); var failed = 0;
        foreach (var path in new BibleFiles(Path.Combine(AppContext.BaseDirectory,"Content","Bibles")).List().Concat(files.List()))
        {
            try { entries.Add(new(files.Load(path).Title,path)); }
            catch (Exception error) { App.Log("bible_load_failed",error); failed++; }
        }
        Translation.ItemsSource = entries;
        Translation.SelectedItem = entries.FirstOrDefault(e => e.Path == selected) ?? entries.FirstOrDefault();
        if (failed > 0) Message.Text = $"{failed} unreadable Bible file(s) were preserved. Reimport their XML sources.";
    }
    private async void ImportBible(object sender, RoutedEventArgs e)
    {
        var attribution = Attribution.Text;
        if (string.IsNullOrWhiteSpace(attribution)) { Message.Text = "Enter the translation's attribution, licence and source before importing."; return; }
        var picker = new OpenFileDialog { Filter = "Zefania Bible XML (*.xml)|*.xml", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        await ImportSource(picker.FileName, attribution);
    }
    internal async Task ImportSource(string source, string attribution)
    {
        ImportBibleButton.IsEnabled = false;
        try
        {
            var path = await Task.Run(() => { using var stream = File.OpenRead(source); return files.Import(stream,attribution); });
            Reload(path); Message.Text = "Translation imported. Choose book, chapter and verses; saving a passage does not change live output.";
        }
        catch (Exception error) { App.Log("bible_import_failed",error); Message.Text = "Import failed: " + error.Message; }
        finally { ImportBibleButton.IsEnabled = true; }
    }
    private void TranslationChanged(object sender, SelectionChangedEventArgs e)
    {
        bible = null; Book.ItemsSource = null; passage = null; UsePassageButton.IsEnabled = false;
        if (Translation.SelectedItem is not TranslationFile file) return;
        try { bible = files.Load(file.Path); Book.ItemsSource = bible.Verses.Select(v => v.Book).Distinct().ToArray(); Book.SelectedIndex = 0; Message.Text = bible.Attribution; }
        catch (Exception error) { Message.Text = "Could not open translation: " + error.Message; }
    }
    private void BookChanged(object sender, SelectionChangedEventArgs e)
    {
        Chapter.ItemsSource = bible?.Verses.Where(v => v.Book == Book.SelectedItem as string).Select(v => v.Chapter).Distinct().Order().ToArray(); Chapter.SelectedIndex = 0;
    }
    private void ChapterChanged(object sender, SelectionChangedEventArgs e)
    {
        FirstVerse.ItemsSource = bible?.Verses.Where(v => v.Book == Book.SelectedItem as string && v.Chapter == (Chapter.SelectedItem as int?)).Select(v => v.Number).Order().ToArray(); FirstVerse.SelectedIndex = 0;
    }
    private void FirstChanged(object sender, SelectionChangedEventArgs e)
    {
        LastVerse.ItemsSource = (FirstVerse.ItemsSource as int[])?.Where(n => FirstVerse.SelectedItem is int first && n >= first && n - first < 100).ToArray(); LastVerse.SelectedIndex = 0;
    }
    private void RangeChanged(object sender, SelectionChangedEventArgs e)
    {
        passage = null; UsePassageButton.IsEnabled = false; PassageText.Text = "Choose a valid passage.";
        if (bible is null || Book.SelectedItem is not string book || Chapter.SelectedItem is not int chapter || FirstVerse.SelectedItem is not int first || LastVerse.SelectedItem is not int last) return;
        try
        {
            passage = bible.Passage(book,chapter,first,last);
            PassageText.Text = passage.Title + "\n\n" + string.Join("\n\n", SongStructure.Parse(passage.Lyrics).Select(s => s.Text));
            UsePassageButton.IsEnabled = true;
        }
        catch (ArgumentException error) { Message.Text = error.Message; }
    }
    private void UsePassage(object sender, RoutedEventArgs e)
    { if (passage is null) return; Result = passage; DialogResult = true; }
}
