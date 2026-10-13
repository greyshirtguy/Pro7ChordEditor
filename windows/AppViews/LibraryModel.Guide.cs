// The window's model, the part that says when the guide is to be shown
// (spec/13-guide.md, rules 2 to 4; Guide.cs has its words, GuideView.cs shows them).
//
// The guide opens by itself once: at the first start of the app on a PC, which is the
// first start at which the app's own settings do not say that it has been shown. That
// it has, is written down when it opens, and not when it is closed; and writing it
// changes nothing else that is kept. After that it opens only when it is asked for: by
// Help in the menu, by F1, and by the ? at the right of the sheet's strip.
//
// The model only says that the guide is wanted (`GuideWanted`). Opening a window is the
// window's to do.

namespace Pro7ChordEditor.AppViews;

public sealed partial class LibraryModel
{
    // Whether the guide has opened by itself already. It is remembered.
    private bool guideShown;

    /// <summary>The guide is to be shown: because it was asked for, or because this is the app's first start on this PC.</summary>
    public event Action? GuideWanted;

    /// <summary>Goes up each time the guide is to be shown: for a test to count.</summary>
    public int GuideAsked { get; private set; }

    /// <summary>The guide is asked for: by the menu, by its key, or by the ? at the right of the strip.</summary>
    public void ShowGuide()
    {
        GuideAsked++;
        GuideWanted?.Invoke();
    }

    // At the app's start, once the libraries have been found: if nothing says the guide
    // has been shown, it is shown, and that is written down now. What is kept is read
    // again and written back with that one thing more in it, so that nothing else of
    // it is other than it was.
    private void ShowTheGuideIfItNeverWas(bool shown)
    {
        guideShown = shown;
        if (guideShown)
        {
            return;
        }
        guideShown = true;
        appData.Keep(appData.ReadSettings() with { GuideShown = true });
        appData.Log("The guide was shown, as it is once.");
        ShowGuide();
    }
}
