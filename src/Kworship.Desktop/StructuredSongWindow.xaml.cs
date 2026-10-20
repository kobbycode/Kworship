using System.Windows;
using Kworship.Application;
namespace Kworship.Desktop;
public partial class StructuredSongWindow:Window
{
    private readonly StructuredSongEditor editor;
    public Song? Result{get;private set;}
    public StructuredSongWindow(Song song)
    {
        InitializeComponent();editor=new(song);DataContext=editor;
        if(editor.Sections.Count==0)editor.AddSection("Verse");
        SectionsList.SelectedIndex=0;
    }
    private void Add(string kind){SectionsList.SelectedItem=editor.AddSection(kind);}
    private void AddVerse(object sender,RoutedEventArgs e)=>Add("Verse");
    private void AddChorus(object sender,RoutedEventArgs e)=>Add("Chorus");
    private void AddBridge(object sender,RoutedEventArgs e)=>Add("Bridge");
    private void DeleteSection(object sender,RoutedEventArgs e)
    {try{if(SectionsList.SelectedItem is EditableSection section)editor.RemoveSection(section);}catch(InvalidOperationException error){EditorStatus.Text=error.Message;}}
    private void AddToOrder(object sender,RoutedEventArgs e)
    {if(SectionsList.SelectedItem is EditableSection section){editor.Order.Add(section);OrderList.SelectedIndex=editor.Order.Count-1;}}
    private void OrderUp(object sender,RoutedEventArgs e)=>MoveOrder(-1);
    private void OrderDown(object sender,RoutedEventArgs e)=>MoveOrder(1);
    private void MoveOrder(int delta){var index=OrderList.SelectedIndex;if(index<0)return;var target=Math.Clamp(index+delta,0,editor.Order.Count-1);editor.Order.Move(index,target);OrderList.SelectedIndex=target;}
    private void RemoveOrder(object sender,RoutedEventArgs e){if(OrderList.SelectedIndex>=0)editor.Order.RemoveAt(OrderList.SelectedIndex);}
    private void ClearOrder(object sender,RoutedEventArgs e)=>editor.Order.Clear();
    private void ApplyChanges(object sender,RoutedEventArgs e)
    {try{Result=editor.Build();DialogResult=true;}catch(ArgumentException error){EditorStatus.Text=error.Message;}}
}
