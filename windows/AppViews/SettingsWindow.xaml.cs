// The code beside SettingsWindow.xaml, which says what the window is. Here it is given
// what it holds: the settings' view, of the window's own model and of the copies this
// PC's app keeps.

using System.Windows;

namespace Pro7ChordEditor.AppViews;

/// <summary>The settings: the chords' colours, and the copies that are kept.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(LibraryModel model)
        : this(model, AllCopiesModel.OnThisPC())
    {
    }

    public SettingsWindow(LibraryModel model, AllCopiesModel copies)
    {
        InitializeComponent();
        Content = new SettingsView(model, copies);
    }

    /// <summary>Shows one of the settings' sections, by its name: "About".</summary>
    public void Show(string section) => ((SettingsView)Content).Show(section);
}
