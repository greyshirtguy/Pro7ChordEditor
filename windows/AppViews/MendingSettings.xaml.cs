// The code beside MendingSettings.xaml, which says what the view is. Here: its one
// switch shows what the model has whenever the model says it has changed, and a click on
// it is passed on. The model remembers it, and the list's marks and the song's offers go
// by it at once, in the window behind this one (LibraryModel.Mending.cs).

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Pro7ChordEditor.AppViews;

/// <summary>Mending's one setting: whether chords that look as if they were put on in the old editor are offered to be moved.</summary>
public partial class MendingSettings : UserControl
{
    private readonly LibraryModel model;

    public MendingSettings(LibraryModel model)
    {
        this.model = model;
        InitializeComponent();
        Look.Follow(this);
        model.PropertyChanged += TheModelChanged;
        Unloaded += (_, _) => model.PropertyChanged -= TheModelChanged;
        Loaded += (_, _) =>
        {
            // (Shown again after being put away: it follows the model again.)
            model.PropertyChanged -= TheModelChanged;
            model.PropertyChanged += TheModelChanged;
            Show();
        };
        Show();
    }

    /// <summary>The switch: for a test to click.</summary>
    internal CheckBox Switch => MoveBack;

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    private void Show() => MoveBack.IsChecked = model.OffersToMoveBack;

    private void MoveBackWasClicked(object sender, RoutedEventArgs e) => model.SetOffersToMoveBack(MoveBack.IsChecked == true);
}
