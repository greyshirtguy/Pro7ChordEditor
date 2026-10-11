// The code beside RevertQuestionPanel.xaml, which says what the panel is. Here: it is
// there while the model is asking, with the model's words in it, and the answer is
// passed on.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pro7ChordEditor.AppViews;

/// <summary>The question Revert asks first, over the window.</summary>
public partial class RevertQuestionPanel : UserControl
{
    private LibraryModel? model;

    public RevertQuestionPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
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

    private void Show()
    {
        var question = model?.RevertQuestion;
        var wasOpen = Visibility == Visibility.Visible;
        Visibility = question is null ? Visibility.Collapsed : Visibility.Visible;
        if (question is null)
        {
            return;
        }
        Heading.Text = question.Title;
        Message.Text = question.Message;
        Yes.Content = question.Yes;
        if (!wasOpen)
        {
            // Cancel has the keyboard: it is what Enter and the space bar give.
            Cancel.Focus();
        }
    }

    private void OutsideWasClicked(object sender, MouseButtonEventArgs e) => model?.DontRevert();

    private void CancelWasClicked(object sender, RoutedEventArgs e) => model?.DontRevert();

    private void YesWasClicked(object sender, RoutedEventArgs e) => model?.Revert();
}
