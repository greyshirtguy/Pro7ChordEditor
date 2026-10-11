// The code beside QuestionPanel.xaml, which says what the panel is. Here: it is there
// while the model is asking, with the model's words in it, and the answer is passed on.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pro7ChordEditor.AppViews;

/// <summary>A question the model asks before something is done, over the window.</summary>
public partial class QuestionPanel : UserControl
{
    private LibraryModel? model;

    public QuestionPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>What the panel says: its heading, the words under it, and what its two buttons say. For a test.</summary>
    internal (string Heading, string Message, string Yes, string Other) Says => (Heading.Text, Message.Text, (string)Yes.Content, (string)Cancel.Content);

    /// <summary>The button that says yes, and the other, which is what Enter and Esc give. For a test to click.</summary>
    internal (Button Yes, Button Other) Buttons => (Yes, Cancel);

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

    private void Show()
    {
        var question = model?.Asked;
        var wasOpen = Visibility == Visibility.Visible;
        Visibility = question is null ? Visibility.Collapsed : Visibility.Visible;
        if (question is null)
        {
            return;
        }
        Heading.Text = question.Title;
        Message.Text = question.Message;
        Yes.Content = question.Yes;
        Cancel.Content = question.No;
        if (!wasOpen || !ReferenceEquals(question, asked))
        {
            // The second button has the keyboard: it is what Enter and the space bar
            // give.
            Cancel.Focus();
        }
        asked = question;
    }

    // The question last shown: one that follows another at once (what is to become of
    // the copy, after its chords were put in) gives the keyboard to its second button
    // afresh.
    private LibraryModel.Question? asked;

    private void OutsideWasClicked(object sender, MouseButtonEventArgs e) => model?.Answer(false);

    private void CancelWasClicked(object sender, RoutedEventArgs e) => model?.Answer(false);

    private void YesWasClicked(object sender, RoutedEventArgs e) => model?.Answer(true);
}
