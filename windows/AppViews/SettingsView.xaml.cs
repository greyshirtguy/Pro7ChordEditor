// The code beside SettingsView.xaml, which says what the view is. Here its four
// sections are given what they hold: the chords' colours and mending's one switch, both
// of the window's model; All Copies, of the copies this PC's app keeps; and what the
// app says of itself.

using System.Windows.Controls;

namespace Pro7ChordEditor.AppViews;

/// <summary>The settings' sections: Chords, Mending, Backups, and About.</summary>
public partial class SettingsView : UserControl
{
    public SettingsView(LibraryModel model, AllCopiesModel copies)
    {
        InitializeComponent();
        Look.Follow(this);
        ChordColours = new ChordColourSettings(model);
        Chords.Content = ChordColours;
        MendingSwitch = new MendingSettings(model);
        Mending.Content = MendingSwitch;
        Backups.Content = new AllCopiesView(copies);
        AboutTheApp = new AboutSettings();
        About.Content = AboutTheApp;
    }

    /// <summary>The fourth section: for a test to read.</summary>
    internal AboutSettings AboutTheApp { get; }

    /// <summary>The second section: for a test to click its switch.</summary>
    internal MendingSettings MendingSwitch { get; }

    /// <summary>The first section: for a test to choose a colour in.</summary>
    internal ChordColourSettings ChordColours { get; }

    /// <summary>Shows a section by its name, as a click on its tab does.</summary>
    internal void Show(string section) =>
        Sections.SelectedItem = Sections.Items.OfType<TabItem>().First(tab => (string)tab.Header == section);
}
