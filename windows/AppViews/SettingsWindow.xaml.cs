// The code beside SettingsWindow.xaml, which says what the window is. Here it is given
// what it holds: All Copies, of the copies this PC's app keeps.

using System.Windows;

namespace Pro7ChordEditor.AppViews;

/// <summary>The settings: so far, the copies that are kept.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(AllCopiesModel.OnThisPC())
    {
    }

    public SettingsWindow(AllCopiesModel copies)
    {
        InitializeComponent();
        Content = new AllCopiesView(copies);
    }
}
