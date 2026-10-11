// The code beside SettingsView.xaml, which says what the view is. Here its two sections
// are given what they hold: the chords' colours, of the window's model, and All Copies,
// of the copies this PC's app keeps.

using System.Windows.Controls;

namespace Pro7ChordEditor.AppViews;

/// <summary>The settings' sections: Chords, and Backups.</summary>
public partial class SettingsView : UserControl
{
    public SettingsView(LibraryModel model, AllCopiesModel copies)
    {
        InitializeComponent();
        Look.Follow(this);
        ChordColours = new ChordColourSettings(model);
        Chords.Content = ChordColours;
        Backups.Content = new AllCopiesView(copies);
    }

    /// <summary>The first section: for a test to choose a colour in.</summary>
    internal ChordColourSettings ChordColours { get; }

    /// <summary>Shows a section by its name, as a click on its tab does.</summary>
    internal void Show(string section) =>
        Sections.SelectedItem = Sections.Items.OfType<TabItem>().First(tab => (string)tab.Header == section);
}
