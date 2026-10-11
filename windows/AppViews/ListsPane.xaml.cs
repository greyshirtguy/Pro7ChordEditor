// The code beside ListsPane.xaml, which says what the pane is. Here: what is in the
// model is put into the lists whenever the model says it has changed, what the person
// does in them is passed on to the model, and the bar between them is dragged.
//
// Nothing here is a rule. How much height the bar leaves each list is ListsLook's
// arithmetic; which songs are listed, what a row says of a song, and what a click on a
// song does are LibraryModel's.
//
// SHOWING AND BEING TOLD. Putting the model's choice into a list makes the list say
// that its choice changed, exactly as a click does. So while this is showing what the
// model has (`showing`), what the lists say is not passed on.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>The left of the window: the place and its libraries, over the songs of the library chosen.</summary>
public partial class ListsPane : UserControl
{
    private LibraryModel? model;
    private bool showing;
    private IReadOnlyList<Place>? shownPlaces;
    private IReadOnlyList<Library>? shownLibraries;
    private List<Row> shownRows = [];

    // While the bar is being dragged: how tall the libraries' part was when the drag
    // began, where the pointer was then, and how tall the drag has made it.
    private double? began;
    private double pressedAt;
    private double? dragged;

    public ListsPane()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
        SizeChanged += (_, _) => LayOut();
    }

    /// <summary>A place where one is picked: its id, and what it is called there.</summary>
    public sealed record PlaceChoice(string Id, string Label);

    /// <summary>A song's row in the list. It is told when its marks have been worked out, and the row is drawn again by itself.</summary>
    public sealed class Row(LibraryModel.SongRow song) : INotifyPropertyChanged
    {
        private LibraryModel.SongRow song = song;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id => song.Id;

        public string Name => song.Name;

        public bool CannotBeRead => song.CannotBeRead;

        public bool IsOldEditorsCopy => song.IsOldEditorsCopy;

        public bool HasChords => song.HasChords;

        public string CannotBeReadTip => LibraryModel.SongRow.CannotBeReadTip;

        public string OldEditorsCopyTip => LibraryModel.SongRow.OldEditorsCopyTip;

        public string ChordsTip => song.ChordsTip;

        /// <summary>How strongly the row is drawn: a file that cannot be read is fainter.</summary>
        public double Strength => song.CannotBeRead ? 0.55 : 1;

        public void Is(LibraryModel.SongRow now)
        {
            if (song != now)
            {
                song = now;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }

    private void Follow(LibraryModel? now)
    {
        if (model is not null)
        {
            model.PropertyChanged -= TheModelChanged;
        }
        model = now;
        if (model is not null)
        {
            model.PropertyChanged += TheModelChanged;
        }
        Show();
    }

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    // Puts what the model has into the pane.
    private void Show()
    {
        if (model is null)
        {
            return;
        }
        showing = true;
        try
        {
            NoPlace.Text = model.NoPlaceSentence;
            NoPlace.Visibility = model.NoPlaceSentence.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            Places.Visibility = model.Places.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (!ReferenceEquals(shownPlaces, model.Places))
            {
                shownPlaces = model.Places;
                Places.ItemsSource = model.Places.Select(place => new PlaceChoice(place.Id, LibraryModel.Describe(place))).ToList();
            }
            Places.SelectedValue = model.Place?.Id;
            Places.ToolTip = model.Place?.Folder;

            NoLibraries.Text = model.NoLibrariesSentence;
            NoLibraries.Visibility = model.NoLibrariesSentence.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (!ReferenceEquals(shownLibraries, model.Libraries))
            {
                shownLibraries = model.Libraries;
                Libraries.ItemsSource = model.Libraries;
            }
            Libraries.SelectedValue = model.Library?.Id;

            Count.Text = model.SongsCount;
            if (!string.Equals(Search.Text, model.Typed, StringComparison.Ordinal))
            {
                Search.Text = model.Typed;
            }
            Hint.Visibility = model.Typed.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Clear.Visibility = model.Typed.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            All.IsChecked = model.Filter == MarkFilter.All;
            WithChords.IsChecked = model.Filter == MarkFilter.WithChords;
            Without.IsChecked = model.Filter == MarkFilter.WithoutChords;

            // The songs. While they are the same songs in the same order (marks coming
            // in, a song opened), each row is only told what it now is, so that the
            // list stays where it was scrolled to; otherwise the list is made again.
            var rows = model.Rows;
            if (rows.Count == shownRows.Count && rows.Zip(shownRows).All(pair => Paths.Same(pair.First.Id, pair.Second.Id)))
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    shownRows[i].Is(rows[i]);
                }
            }
            else
            {
                shownRows = [.. rows.Select(row => new Row(row))];
                Songs.ItemsSource = shownRows;
            }
            Songs.SelectedItem = shownRows.FirstOrDefault(row => Paths.Same(row.Id, model.SongId));
            LayOut();
        }
        finally
        {
            showing = false;
        }
    }

    // ---- What the person does in the lists

    private void APlaceWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Places.SelectedValue is string id)
        {
            model?.ChoosePlace(id);
        }
    }

    private void ALibraryWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Libraries.SelectedValue is string id)
        {
            model?.ChooseLibrary(id);
        }
    }

    private void ASongWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Songs.SelectedItem is Row row)
        {
            model?.Open(row.Id);
        }
    }

    private void TheSearchWasTyped(object sender, TextChangedEventArgs e)
    {
        if (!showing && model is not null)
        {
            model.Typed = Search.Text;
        }
    }

    private void TheSearchIsCleared(object sender, RoutedEventArgs e)
    {
        if (model is not null)
        {
            model.Typed = "";
        }
        Search.Focus();
    }

    // (A click on one of the three already chosen would switch it off, as such a button
    // does by itself; showing the model again puts it back on.)
    private void AllIsChosen(object sender, RoutedEventArgs e) => Narrow(MarkFilter.All);

    private void WithChordsIsChosen(object sender, RoutedEventArgs e) => Narrow(MarkFilter.WithChords);

    private void WithoutIsChosen(object sender, RoutedEventArgs e) => Narrow(MarkFilter.WithoutChords);

    private void Narrow(MarkFilter to)
    {
        if (model is not null)
        {
            model.Filter = to;
        }
        Show();
    }

    private void ChooseAFolder(object sender, RoutedEventArgs e)
    {
        var asking = new OpenFolderDialog
        {
            Title = "Choose a ProPresenter folder, its Libraries folder, or any folder of .pro files",
            Multiselect = false,
        };
        if (asking.ShowDialog(Window.GetWindow(this)) == true && asking.FolderName.Length > 0)
        {
            model?.Add(asking.FolderName);
        }
    }

    // ---- The bar between the two lists

    // Gives the libraries' part its height: what the drag has made it, or its share.
    private void LayOut()
    {
        if (model is null || ActualHeight <= 0)
        {
            return;
        }
        var top = dragged ?? ListsLook.Top(model.ListsSplit, ActualHeight);
        if (LibrariesRow.Height.Value != top)
        {
            LibrariesRow.Height = new GridLength(top);
        }
    }

    private void TheBarIsPressed(object sender, MouseButtonEventArgs e)
    {
        // Clicked twice, it goes back to where it usually is.
        if (e.ClickCount == 2)
        {
            EndTheDrag();
            model?.SetListsSplit(null);
            return;
        }
        began = LibrariesRow.ActualHeight;
        pressedAt = e.GetPosition(this).Y;
        Bar.CaptureMouse();
    }

    private void TheBarIsMoved(object sender, MouseEventArgs e)
    {
        if (began is not { } start || ActualHeight <= 0)
        {
            return;
        }
        dragged = ListsLook.Top((start + e.GetPosition(this).Y - pressedAt) / ActualHeight, ActualHeight);
        LayOut();
    }

    private void TheBarIsLetGo(object sender, MouseButtonEventArgs e)
    {
        var left = dragged;
        EndTheDrag();
        if (left is { } height && ActualHeight > 0)
        {
            // Where it was left is remembered, as its share of the height.
            model?.SetListsSplit(height / ActualHeight);
        }
    }

    private void TheBarIsLost(object sender, MouseEventArgs e)
    {
        // The drag was taken away (another window came to the front): as if never begun.
        if (began is not null)
        {
            began = null;
            dragged = null;
            LayOut();
        }
    }

    private void EndTheDrag()
    {
        began = null;
        dragged = null;
        if (Bar.IsMouseCaptured)
        {
            Bar.ReleaseMouseCapture();
        }
        Grip.Opacity = Bar.IsMouseOver ? 0.55 : 0.28;
    }

    private void ThePointerIsOverTheBar(object sender, MouseEventArgs e) => Grip.Opacity = 0.55;

    private void ThePointerHasLeftTheBar(object sender, MouseEventArgs e)
    {
        if (began is null)
        {
            Grip.Opacity = 0.28;
        }
    }
}
