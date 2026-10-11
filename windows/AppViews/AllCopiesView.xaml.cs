// The code beside AllCopiesView.xaml, which says what the view is. Here: what the model
// has of the copies is put into the view whenever the model says it has changed, what
// is picked is passed on, and the two buttons at the foot do what only a view can: show
// a folder in File Explorer, and ask the person where to save a file.
//
// Nothing here is a rule, and nothing here changes a song.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>Every copy the app keeps, to look at or to save as a file.</summary>
public partial class AllCopiesView : UserControl
{
    private readonly AllCopiesModel model;
    private bool showing;
    private IReadOnlyList<AllCopiesModel.Row>? shownRows;
    private IReadOnlyList<KeptCopy>? shownCopies;

    public AllCopiesView(AllCopiesModel model)
    {
        this.model = model;
        InitializeComponent();
        Look.Follow(this);
        WhatTheseAre.Text = AllCopiesModel.WhatTheseAre;
        None.Text = AllCopiesModel.NoneYet;
        model.PropertyChanged += TheModelChanged;
        // What is kept is read when the settings are opened.
        model.Read();
        Show();
    }

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    private static Visibility Seen(bool seen) => seen ? Visibility.Visible : Visibility.Collapsed;

    // Puts what the model has into the view.
    private void Show()
    {
        showing = true;
        try
        {
            var any = model.Rows.Count > 0;
            None.Visibility = Seen(!any);
            Kept.Visibility = Seen(any);
            if (!ReferenceEquals(shownRows, model.Rows))
            {
                shownRows = model.Rows;
                Songs.ItemsSource = model.Rows;
            }
            Songs.SelectedValue = model.PickedSong;

            var copies = model.PickedRow?.Copies ?? [];
            if (!ReferenceEquals(shownCopies, copies))
            {
                shownCopies = copies;
                Copies.ItemsSource = copies.Select(copy => new EarlierVersionsPanel.CopyRow(copy.Id, model.When(copy), LibraryModel.Why(copy))).ToList();
            }
            Copies.SelectedValue = model.PickedCopy;

            var looked = model.Looked;
            Problem.Text = looked?.Problem ?? "";
            Problem.Visibility = Seen(looked?.Problem is not null);
            Scroll.Visibility = Seen(looked is not null && looked.Problem is null);
            Lines.Show(looked?.Lines ?? []);

            Foot.Text = model.Notice;
            Foot.SetResourceReference(TextBlock.ForegroundProperty, model.Problem is null ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush");
            ShowFolder.IsEnabled = any;
            SaveAs.IsEnabled = model.Copy is not null;
        }
        finally
        {
            showing = false;
        }
    }

    private void ASongWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Songs.SelectedValue is string id)
        {
            model.PickSong(id);
        }
    }

    private void ACopyWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Copies.SelectedValue is string id)
        {
            model.PickCopy(id);
        }
    }

    // The folder the copies are in, opened in File Explorer. (It is there: the button
    // is greyed while nothing is kept.)
    private void ShowTheirFolder(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(model.Folder))
        {
            return;
        }
        try
        {
            using var shown = Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { model.Folder } });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            // File Explorer would not start. There is nothing to be done about that
            // here, and nothing was lost.
        }
    }

    private void SaveThisCopyAs(object sender, RoutedEventArgs e)
    {
        if (model.Copy is not { } copy)
        {
            return;
        }
        var asking = new SaveFileDialog
        {
            Title = "Save this copy as a file of its own. The song in the library is not changed.",
            FileName = model.NameToSaveAs(copy),
            DefaultExt = ".pro",
            Filter = "ProPresenter presentation (*.pro)|*.pro",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (asking.ShowDialog(Window.GetWindow(this)) == true)
        {
            model.SavePicked(asking.FileName);
        }
    }
}
