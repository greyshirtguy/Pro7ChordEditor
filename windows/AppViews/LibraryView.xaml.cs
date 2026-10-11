// The code beside LibraryView.xaml, which says what the view is. Here it is handed its
// model, told to follow the look it is under (Look.cs), and keeps the lists as wide as
// the edge was last dragged to, as far as the window's width lets it (ListsLook.Width).

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Pro7ChordEditor.AppViews;

/// <summary>What the window holds: the lists at the left, the song at the right.</summary>
public partial class LibraryView : UserControl
{
    // How wide the lists were when the edge was last let go.
    private double leftAt = ListsLook.UsualWidth;

    public LibraryView(LibraryModel model)
    {
        InitializeComponent();
        Look.Follow(this);
        // (The two panes have it from here: what a view is handed, those in it are
        // handed too.)
        DataContext = model;
        SizeChanged += (_, _) => LayOut();
    }

    public LibraryModel Model => (LibraryModel)DataContext;

    // Gives the lists their width for the width the view now has.
    private void LayOut()
    {
        if (ActualWidth <= 0)
        {
            return;
        }
        var width = ListsLook.Width(leftAt, ActualWidth);
        if (Lists.Width.Value != width)
        {
            Lists.Width = new GridLength(width);
        }
    }

    private void TheEdgeWasDragged(object sender, DragCompletedEventArgs e)
    {
        leftAt = Lists.ActualWidth;
        LayOut();
    }
}
