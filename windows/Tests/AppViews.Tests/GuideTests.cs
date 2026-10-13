// The guide: one page on how chords are put on (spec/13-guide.md; "What to hold your
// build to", at its foot, is this file's list).
//
//   - the words the app carries are spec/guide.json to the byte, and what the app
//     shows is those, with Windows' names for the keys and no curly bracket left;
//   - the page of the spec says the same words;
//   - it is shown once by itself, at the app's first start on a PC, and that is written
//     down when it opens, changing nothing else that is kept;
//   - the page as it is laid out: every sentence on it, its pictures where the words
//     have them, never wider than they were drawn; and what it says where the guide
//     cannot be found;
//   - the two places it is asked for from the window: the ? at the right of the
//     strip, and the view's own key and menu (the menu is held in ChordSheetViewTests).
//
// NEW, and none of it is in Simple Presenter: there is no vector. The cases are the
// Mac's (GuideTests.swift), and what is of this app's own way of laying a page out is
// MINE. The pictures themselves are GuidePictureTests'.
//
// What only a hand can tell is not here, since no test has an app to open a window in:
// that it opens at the first start, over the main window, and not at the second; that
// Help, F1 and the question mark each open it, and bring it to the front when it is
// open already; and that a chord can be put on in the main window while it is open.

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class GuideTests
{
    private static readonly string TheSpecs = Path.Combine(TestData.Repository, "spec", "guide.json");

    private static Guide AsTheSpecHasIt() => Guide.Read(File.ReadAllBytes(TheSpecs)) ?? throw new InvalidDataException("spec/guide.json is not the guide");

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    // ---- The words

    [Fact]
    public void TheWordsTheAppCarriesAreTheSpecsToTheByteAndAreWhatTheAppFindsAndShowsInWindowsWords()
    {
        var specs = File.ReadAllBytes(TheSpecs);
        Assert.Equal(specs, File.ReadAllBytes(Path.Combine(GuidePictureTests.Kept, GuideFiles.WordsName)));
        // What is inside the app as it is built is that file.
        Assert.Equal(specs, GuideFiles.Bytes(GuideFiles.WordsName));
        Assert.NotNull(GuideFiles.Guide);
        Assert.Equal(AsTheSpecHasIt().SaidOn(Guide.Windows), GuideFiles.Guide, GuidesAreTheSame.Instance);
        Assert.Equal("windows", Guide.Windows);
        Assert.Null(GuideFiles.Bytes("no-such-file.json"));
    }

    // Two guides say the same when every sentence of them does, and their pictures.
    private sealed class GuidesAreTheSame : IEqualityComparer<Guide?>
    {
        internal static readonly GuidesAreTheSame Instance = new();

        public bool Equals(Guide? one, Guide? other) =>
            one is not null && other is not null
            && one.EverySentence.SequenceEqual(other.EverySentence, StringComparer.Ordinal)
            && one.PicturesNamed.SequenceEqual(other.PicturesNamed, StringComparer.Ordinal)
            && one.Pictures.OrderBy(pair => pair.Key, StringComparer.Ordinal).SequenceEqual(other.Pictures.OrderBy(pair => pair.Key, StringComparer.Ordinal));

        public int GetHashCode(Guide? guide) => guide?.Title.GetHashCode(StringComparison.Ordinal) ?? 0;
    }

    [Fact]
    public void ItHasWhatItIsLaidOutByATitleWhatToKnowFirstTheQuickWayWithItsOneLargeSentenceAndAPictureAndTheWaysAfterIt()
    {
        var guide = AsTheSpecHasIt();
        Assert.Equal("Putting chords on a song", guide.Title);
        Assert.Equal("Before you start", guide.First.Heading);
        Assert.Equal(4, guide.First.Points.Count);
        Assert.Equal(6, guide.Sections.Count);
        var quick = guide.Sections[0];
        Assert.Equal(("The quick way", "Point at a word and press 1 to 7.", "point-and-press"), (quick.Heading, quick.Lead, quick.Picture));
        Assert.Equal(2, quick.Paragraphs!.Count);
        // Only the first section has a sentence written large.
        Assert.All(guide.Sections.Skip(1), section => Assert.Null(section.Lead));
        Assert.Equal(["point-and-press", "typing", "dragging"], guide.PicturesNamed);
        Assert.Equal(guide.PicturesNamed.Order(StringComparer.Ordinal), guide.Pictures.Keys.Order(StringComparer.Ordinal));
        Assert.Contains(guide.Sections, section => section.Sample == "[G]Amazing [G7]grace how [C]sweet the [G]sound");
        Assert.StartsWith("This page is in the Help menu", guide.Foot, StringComparison.Ordinal);
        // Every part is among what there is to read, in the order it is read.
        var all = guide.EverySentence;
        Assert.Equal(guide.Title, all[0]);
        Assert.Equal(guide.Foot, all[^1]);
        Assert.Equal(
            2 + 4 + guide.Sections.Sum(section => 1 + (section.Lead is null ? 0 : 1) + (section.Paragraphs?.Count ?? 0) + (2 * (section.Items?.Count ?? 0)) + (section.Sample is null ? 0 : 1)) + 1,
            all.Count);
        Assert.True(all.ToList().IndexOf(quick.Lead!) < all.ToList().IndexOf(quick.Paragraphs[0]));
    }

    [Fact]
    public void EveryShortNameInCurlyBracketsHasAWordOnEachPlatformAndNoneIsLeftInWhatIsSaidAndWindowsSaysItsOwnKeys()
    {
        var guide = AsTheSpecHasIt();
        Assert.Equal(["mac", "windows"], guide.Names.Keys.Order(StringComparer.Ordinal));
        // The short names the words use, each in curly brackets.
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sentence in guide.EverySentence.Concat(guide.Pictures.Values))
        {
            for (var open = sentence.IndexOf('{', StringComparison.Ordinal); open >= 0; open = sentence.IndexOf('{', open + 1))
            {
                used.Add(sentence[(open + 1)..sentence.IndexOf('}', open)]);
            }
        }
        Assert.NotEmpty(used);
        foreach (var (platform, names) in guide.Names)
        {
            Assert.All(used, name => Assert.True(names.ContainsKey(name), platform + " has no word for {" + name + "}"));
            var said = guide.SaidOn(platform);
            Assert.All(said.EverySentence.Concat(said.Pictures.Values), sentence => Assert.DoesNotContain("{", sentence, StringComparison.Ordinal));
            Assert.All(said.EverySentence.Concat(said.Pictures.Values), sentence => Assert.DoesNotContain("}", sentence, StringComparison.Ordinal));
        }
        // Windows says its own keys, and none of the Mac's.
        var windows = string.Join("\n", guide.SaidOn(Guide.Windows).EverySentence);
        Assert.Contains("With Ctrl held as it is let go, it is copied", windows, StringComparison.Ordinal);
        Assert.Contains("Ctrl+C copies", windows, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Shift+C those of its whole group", windows, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Z undoes the last change, and Ctrl+Y puts it back", windows, StringComparison.Ordinal);
        Assert.Contains("and Enter keeps it", windows, StringComparison.Ordinal);
        Assert.Contains("Ctrl+1 shows the sheet and Ctrl+2 the text", windows, StringComparison.Ordinal);
        Assert.DoesNotContain("Command", windows, StringComparison.Ordinal);
        Assert.DoesNotContain("Option", windows, StringComparison.Ordinal);
        Assert.DoesNotContain("Return", windows, StringComparison.Ordinal);
        // And the Mac its own, from the same file.
        var mac = string.Join("\n", guide.SaidOn("mac").EverySentence);
        Assert.Contains("With Option held as it is let go", mac, StringComparison.Ordinal);
        Assert.Contains("Shift+Command+Z puts it back", mac, StringComparison.Ordinal);
        // A platform the file has not got leaves the short names standing, to be seen.
        Assert.Contains("{ctrl}", string.Join("\n", guide.SaidOn("linux").EverySentence), StringComparison.Ordinal);
    }

    [Fact]
    public void ANameIsPutInWhereverItStandsInCurlyBracketsAndOneThatIsNotKnownIsLeftStandingAndSoIsABracketWithNoOther()
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal) { ["ctrl"] = "Ctrl", ["enter"] = "Enter" };
        Assert.Equal("Ctrl+C and Ctrl+V", Guide.PutInto(names, "{ctrl}+C and {ctrl}+V"));
        Assert.Equal("Press Enter.", Guide.PutInto(names, "Press {enter}."));
        Assert.Equal("Ctrl", Guide.PutInto(names, "{ctrl}"));
        Assert.Equal("Nothing to put in.", Guide.PutInto(names, "Nothing to put in."));
        Assert.Equal("", Guide.PutInto(names, ""));
        // One that is not known is left standing, brackets and all.
        Assert.Equal("Hold {hyper} and Ctrl", Guide.PutInto(names, "Hold {hyper} and {ctrl}"));
        // A bracket with no other is left as it is, and what is before it is kept.
        Assert.Equal("Ctrl and {enter", Guide.PutInto(names, "{ctrl} and {enter"));
        Assert.Equal("a } b Ctrl", Guide.PutInto(names, "a } b {ctrl}"));
    }

    [Fact]
    public void WhatIsNotTheGuideIsReadAsNothingAndAGuideWithAPartLeftOutLikewise()
    {
        Assert.Null(Guide.Read(Encoding.UTF8.GetBytes("this is not the guide")));
        Assert.Null(Guide.Read([]));
        Assert.Null(Guide.Read(Encoding.UTF8.GetBytes("[1, 2, 3]")));
        Assert.Null(Guide.Read(Encoding.UTF8.GetBytes("{}")));
        var whole = Encoding.UTF8.GetString(File.ReadAllBytes(TheSpecs));
        Assert.NotNull(Guide.Read(Encoding.UTF8.GetBytes(whole)));
        foreach (var part in new[] { "\"title\"", "\"first\"", "\"sections\"", "\"foot\"", "\"pictures\"", "\"names\"" })
        {
            Assert.Equal(1, whole.Split(part).Length - 1);
            Assert.Null(Guide.Read(Encoding.UTF8.GetBytes(whole.Replace(part, "\"left out\"", StringComparison.Ordinal))));
        }
    }

    // ---- The page of the spec says the same words

    // Words with what is not said taken out of them: the stars a page of the spec has
    // round what is bold, which way a quotation mark curls, how the lines are broken,
    // and the characters with no width that keep a key's name whole on the guide's page.
    private static string Plain(string text)
    {
        var plain = text.Replace("**", "", StringComparison.Ordinal).Replace(((char)0x2060).ToString(), "", StringComparison.Ordinal);
        foreach (var (curly, straight) in new[] { ((char)0x2019, '\''), ((char)0x2018, '\''), ((char)0x201C, '"'), ((char)0x201D, '"') })
        {
            plain = plain.Replace(curly, straight);
        }
        return string.Join(" ", plain.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void ThePageOfTheSpecSaysTheGuidesWordsEverySentenceOfThemAsWindowsSaysThem()
    {
        var page = Plain(File.ReadAllText(Path.Combine(TestData.Repository, "spec", "13-guide.md")));
        var windows = AsTheSpecHasIt().SaidOn(Guide.Windows);
        Assert.All(windows.EverySentence, sentence => Assert.True(page.Contains(Plain(sentence), StringComparison.Ordinal), "spec/13-guide.md does not say: " + sentence));
        // And what each picture shows, which the page has under the picture.
        Assert.All(windows.Pictures.Values, says => Assert.True(page.Contains(Plain(says), StringComparison.Ordinal), "spec/13-guide.md does not say of a picture: " + says));
        // What the window is called, and what the ? says of itself, are the page's too.
        Assert.Contains(Plain(GuideView.Title), page, StringComparison.Ordinal);
        Assert.Contains(Plain(GuideView.Tip), page, StringComparison.Ordinal);
        Assert.Contains(Plain(GuideView.NotFound), page, StringComparison.Ordinal);
    }

    // ---- Shown once (rule 2)

    [Fact]
    public void TheAppShowsTheGuideOnceByItselfAtItsFirstStartOnAPCAndNotAtTheNextAndAskedForItIsShownWhenever() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        var wanted = 0;
        model.GuideWanted += () => wanted++;
        Assert.Equal(0, model.GuideAsked);
        Assert.Null(pc.AppData.ReadSettings().GuideShown);
        await model.StartAsync();
        Assert.Equal((1, 1), (model.GuideAsked, wanted));
        // That it has, is written down as it opens: not when it is closed.
        Assert.True(pc.AppData.ReadSettings().GuideShown);
        Assert.Contains("The guide was shown, as it is once.", pc.Log, StringComparison.Ordinal);
        // The libraries were found by then.
        Assert.NotEmpty(model.Libraries);
        // Asked to start again, it does nothing, and the guide is not asked for again.
        await model.StartAsync();
        Assert.Equal(1, model.GuideAsked);

        // The next start of the app: not asked for.
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(0, again.GuideAsked);
        Assert.True(pc.AppData.ReadSettings().GuideShown);
        // Asked for, it is shown whenever, as often as it is asked.
        again.ShowGuide();
        again.ShowGuide();
        Assert.Equal(2, again.GuideAsked);
        // And whatever the app then remembers, it remembers that still.
        again.SetListsSplit(0.5);
        again.ChooseLibrary(pc.Library("Kids"));
        Assert.True(pc.AppData.ReadSettings().GuideShown);
        var third = pc.Model();
        await third.StartAsync();
        Assert.Equal(0, third.GuideAsked);
    });

    [Fact]
    public void NothingButItsBeingShownSaysThatTheGuideWasShownWhatTheAppRemembersBeforeItsFirstStartDoesNot() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        // A folder is chosen, and so remembered, before the window has started.
        model.Add(Path.Combine(pc.Place, "Libraries", "Songs"));
        Assert.Single(pc.AppData.ReadSettings().ChosenFolders);
        Assert.Null(pc.AppData.ReadSettings().GuideShown);
        await model.StartAsync();
        Assert.Equal(1, model.GuideAsked);
        Assert.True(pc.AppData.ReadSettings().GuideShown);
        Assert.Single(pc.AppData.ReadSettings().ChosenFolders);
    });

    [Fact]
    public void WhoeverHadTheAppBeforeItHadAGuideIsShownItOnceTooAndWhatTheAppKeptForThemIsAsItWas() => OnAMadeUpPC(async pc =>
    {
        // What an earlier version left: a folder chosen that is not there today, the
        // place and library last open, which are not to be found either, and how the
        // window was left.
        var kept = new AppData.Settings
        {
            ChosenFolders = [@"Z:\Not Plugged In\Songs"],
            LastPlace = "a place that is not found today",
            LastLibrary = "a library of it",
            SheetZoom = 1.25,
            TextZoom = 0.9,
            ListsSplit = 0.5,
            ListsWidth = 400,
            SheetChordColour = "#7fe3ff",
            TextChordColour = "#c95fff",
            SheetLineColour = "#33cc66",
            OffersToMoveBack = true,
        };
        pc.AppData.Keep(kept);
        Assert.Equal(kept, pc.AppData.ReadSettings());

        var model = pc.Model();
        await model.StartAsync();
        Assert.Equal(1, model.GuideAsked);
        var now = kept with { GuideShown = true };
        Assert.Equal(now, pc.AppData.ReadSettings());
        // And it was read as it was: nothing of it was lost on the way.
        Assert.Equal((1.25, "#7fe3ff", 400.0), (model.SheetZoom, model.SheetChordColour, model.ListsWidth));

        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(0, again.GuideAsked);
        Assert.Equal(now, pc.AppData.ReadSettings());
    });

    [Fact]
    public void SettingsThatCannotBeReadAreTakenAsNotThereTheGuideIsShownAndTheAppGoesOnAndSettingsOfOtherVersionsAreReadAllTheSame() => OnAMadeUpPC(async pc =>
    {
        Directory.CreateDirectory(pc.AppData.Folder);
        var file = Path.Combine(pc.AppData.Folder, "settings.json");
        // Written before there was a guide; and with things in them this version does
        // not know.
        File.WriteAllText(file, "{\"chosenFolders\": [\"a\"], \"sheetZoom\": 1.1}");
        Assert.Null(pc.AppData.ReadSettings().GuideShown);
        Assert.Equal(["a"], pc.AppData.ReadSettings().ChosenFolders);
        Assert.Equal(1.1, pc.AppData.ReadSettings().SheetZoom);
        File.WriteAllText(file, "{\"chosenFolders\": [], \"guideShown\": true, \"somethingLater\": 3}");
        Assert.True(pc.AppData.ReadSettings().GuideShown);

        // That cannot be read at all.
        File.WriteAllText(file, "{ this is not settings");
        var model = pc.Model();
        await model.StartAsync();
        Assert.Equal(1, model.GuideAsked);
        Assert.True(pc.AppData.ReadSettings().GuideShown);
        Assert.NotEmpty(model.Libraries);
    });

    // ---- Where it is asked for from the window (rule 3)

    [Fact]
    public void TheQuestionMarkAtTheRightOfTheStripAsksForTheGuideAndSaysWhatItIs() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Amazing Grace"));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var strip = view.SongPane.ChordSheet;
        var guide = (Button)strip.FindName("Guide");
        var smaller = (Button)strip.FindName("Smaller");
        var larger = (Button)strip.FindName("Larger");
        Assert.Equal("The guide: the ways to put a chord on, on one page. It is in the Help menu too.", guide.ToolTip);
        Assert.Equal(GuideView.Tip, guide.ToolTip);
        Assert.Equal("?", ((TextBlock)guide.Content).Text);
        // A third round button after the two that zoom, a little apart from those two.
        double Left(FrameworkElement part) => part.TranslatePoint(new Point(0, 0), view).X;
        Assert.True(Left(smaller) < Left(larger) && Left(larger) < Left(guide));
        Assert.Equal((smaller.ActualWidth, smaller.ActualHeight), (guide.ActualWidth, guide.ActualHeight));
        var between = Left(larger) - (Left(smaller) + smaller.ActualWidth);
        Assert.True(Left(guide) - (Left(larger) + larger.ActualWidth) >= between + 8, "the ? is no further from the + than the + is from the minus");
        // It does not take the keyboard from the sheet, or from a chord being typed.
        Assert.False(guide.Focusable);

        var asked = model.GuideAsked;
        var wanted = 0;
        model.GuideWanted += () => wanted++;
        guide.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal((asked + 1, 1), (model.GuideAsked, wanted));
        // The view's own key for it, and its menu, ask the model too.
        Assert.True(view.KeyPressed(System.Windows.Input.Key.F1, System.Windows.Input.ModifierKeys.None));
        Assert.Equal((asked + 2, 2), (model.GuideAsked, wanted));
        // Nothing of the song was touched by any of it.
        Assert.False(model.ChangedSinceOpened);
        Assert.Equal("Pro7 Chord Editor Guide", GuideView.Title);
    });

    // ---- The page (rules 8 to 11)

    private static GuideView Page(Guide? guide, double width = 780, double height = 820, bool dark = false)
    {
        var view = new GuideView(guide);
        Look.Wear(view, dark);
        Pictures.LayOut(view, width, height);
        return view;
    }

    // Where a block of the page is, down the page.
    private static double TopOf(Block block) => block.ContentStart.GetCharacterRect(LogicalDirection.Forward).Top;

    [Fact]
    public void ThePageSaysEverySentenceOfTheGuideInItsOrderWithItsPicturesWhereTheWordsHaveThem() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var guide = GuideFiles.Guide!;
        var view = Page(guide);
        var says = Plain(view.Says);
        // Every sentence, in the order it is read; and no curly bracket anywhere.
        var from = 0;
        foreach (var sentence in guide.EverySentence)
        {
            var at = says.IndexOf(Plain(sentence), from, StringComparison.Ordinal);
            Assert.True(at >= 0, "the page does not say, after what goes before it: " + sentence);
            from = at;
        }
        Assert.DoesNotContain("{", view.Says, StringComparison.Ordinal);
        Assert.DoesNotContain("}", view.Says, StringComparison.Ordinal);
        // The name of a key is kept whole where a line is broken: each plus has a
        // character with no width either side of it that joins what is beside it.
        var joiner = (char)0x2060;
        Assert.Contains("Ctrl" + joiner + "+" + joiner + "1 shows the sheet", view.Says, StringComparison.Ordinal);
        Assert.Equal(view.Says.Count(unit => unit == '+') * 2, view.Says.Count(unit => unit == joiner));

        // The three pictures, each what the app carries under its name, and each said
        // in words for whoever cannot see it.
        Assert.Equal(3, view.PicturesShown.Count);
        for (var n = 0; n < 3; n++)
        {
            var name = guide.PicturesNamed[n];
            Assert.Same(GuideView.PictureNamed(name), view.PicturesShown[n].Source);
            Assert.Equal(guide.Pictures[name], System.Windows.Automation.AutomationProperties.GetName(view.PicturesShown[n]));
        }

        // The title is the largest thing on the page, and the one large sentence of the
        // quick way the next, in the sheet's orange; the box at the top is tinted with
        // that orange.
        var blocks = view.Document.Blocks.ToList();
        var title = (Paragraph)blocks[0];
        Assert.Equal(guide.Title, new TextRange(title.ContentStart, title.ContentEnd).Text);
        var first = Assert.IsType<Section>(blocks[1]);
        var tint = ((SolidColorBrush)first.Background).Color;
        Assert.Equal((SheetLook.Accent.R, SheetLook.Accent.G, SheetLook.Accent.B), (tint.R, tint.G, tint.B));
        Assert.True(tint.A is > 10 and < 80);
        var lead = blocks.OfType<Paragraph>().Single(paragraph => new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text == guide.Sections[0].Lead);
        Assert.Equal(SheetLook.Accent, ((SolidColorBrush)lead.Foreground).Color);
        Assert.True(title.FontSize > lead.FontSize);
        Assert.All(blocks.OfType<Paragraph>().Where(paragraph => paragraph != title && paragraph != lead), paragraph => Assert.True(paragraph.FontSize < lead.FontSize));
        // The line of ChordPro text: in letters of one width, its chords bold.
        var sample = blocks.OfType<Paragraph>().Single(paragraph => new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.StartsWith("[G]Amazing", StringComparison.Ordinal));
        Assert.Contains("Mono", sample.FontFamily.Source, StringComparison.Ordinal);
        var chords = sample.Inlines.OfType<Run>().Where(run => run.Text.StartsWith('[')).ToList();
        Assert.Equal(["[G]", "[G7]", "[C]", "[G]"], chords.Select(run => run.Text));
        Assert.All(chords, run => Assert.Equal(FontWeights.Bold, run.FontWeight));
        Assert.All(chords, run => Assert.Equal(Look.Colours(dark: false)[Look.Chord], ((SolidColorBrush)run.Foreground).Color));
        // In the colour chords have in the text, where another is chosen there.
        view.ChordColour = Color.FromRgb(0x44, 0x88, 0xFF);
        var again = view.Document.Blocks.OfType<Paragraph>().Single(paragraph => new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.StartsWith("[G]Amazing", StringComparison.Ordinal));
        Assert.All(again.Inlines.OfType<Run>().Where(run => run.Text.StartsWith('[')), run => Assert.Equal(Color.FromRgb(0x44, 0x88, 0xFF), ((SolidColorBrush)run.Foreground).Color));
        // The foot is small, and last.
        var foot = (Paragraph)view.Document.Blocks.LastBlock;
        Assert.Equal(guide.Foot, new TextRange(foot.ContentStart, foot.ContentEnd).Text);
        Assert.True(foot.FontSize < 14);
    });

    [Fact]
    public void ALineOfTheWordsIsAtMostSevenHundredAndTwentyWideAndAPictureIsNeverShownLargerThanItWasDrawn() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var guide = GuideFiles.Guide!;
        // As the window opens: 780 wide, a margin of 30 round the page.
        var view = Page(guide);
        Assert.Equal((720.0, 30.0), (GuideView.PageWidth, GuideView.PageMargin));
        Assert.Equal(30, view.Document.PagePadding.Left);
        Assert.Equal(30, view.Document.PagePadding.Top);
        var first = view.PicturesShown[0];
        // (The page has a scroll bar down its side, which takes a little of the width.)
        Assert.InRange(first.ActualWidth, 680, 720);
        Assert.Equal(first.ActualWidth / first.Source.Width, first.ActualHeight / first.Source.Height, 0.01);
        // The picture of the quick way is in view as the window opens.
        Assert.True(first.TranslatePoint(new Point(0, first.ActualHeight), view).Y < 820);

        // Wider: the words stay 720 wide, in the middle; a picture is 720 and no more.
        Pictures.LayOut(view, 1400, 900);
        var side = view.Document.PagePadding.Left;
        Assert.True(side > 300, "the margin at the side of a wide page is " + side);
        Assert.Equal(side, view.Document.PagePadding.Right);
        Assert.InRange(1400 - SystemParameters.VerticalScrollBarWidth - (2 * side), 719, 721);
        Assert.Equal(720, view.PicturesShown[0].ActualWidth, 0.6);
        Assert.Equal(view.PicturesShown[0].Source.Height, view.PicturesShown[0].ActualHeight, 0.6);
        // (One that is under a way of doing a thing stands in from the page's edge with
        // it, and so is a little narrower.)
        Assert.All(view.PicturesShown.Skip(1), picture => Assert.InRange(picture.ActualWidth, 690, 720));

        // Narrower: the pictures are shown smaller, in proportion, and nothing is cut.
        Pictures.LayOut(view, 460, 600);
        Assert.Equal(30, view.Document.PagePadding.Left);
        Assert.All(view.PicturesShown, picture => Assert.InRange(picture.ActualWidth, 300, 400));
        Assert.All(view.PicturesShown, picture => Assert.Equal(picture.ActualWidth / picture.Source.Width, picture.ActualHeight / picture.Source.Height, 0.01));
    });

    // "Each picture is on the page where the words have it", told by how much of the
    // sheet's own dark is on a light page: three pictures' worth on the whole page, and
    // none where the guide has no pictures. (A page with a picture left out is not to
    // pass for one that has them all, as the Mac's first way of telling let it.)
    [Fact]
    public void EachPictureIsOnThePageAsMuchOfTheSheetsDarkAsThreePicturesHaveAndNoneWithoutThem() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var guide = GuideFiles.Guide!;
        // (The sheet's dark greys are a little blue, each of them: #1b1c1f, #222428,
        // #202226. The page's own words are dark too, on a light page, but are grey
        // with no blue in it, and so are not counted.)
        static int Dark((int Width, int Height, int[] Dots) picture) =>
            picture.Dots.Count(dot => ((dot >> 16) & 0xFF) < 0x40 && (dot & 0xFF) - ((dot >> 16) & 0xFF) is >= 3 and <= 8);
        // The whole page, drawn tall enough to have all of it.
        var whole = Pictures.Drawn(Page(guide, 780, 3200), 780, 3200);
        var each = guide.PicturesNamed.Select(name => GuideView.PictureNamed(name)!).ToList();
        var ofThree = each.Sum(picture => picture.PixelWidth * picture.PixelHeight);
        // (The pictures are a little narrower than they were drawn, beside the page's
        // scroll bar; most of each is the sheet's dark; and the words are dark too.)
        var dark = Dark(whole);
        Assert.InRange(dark, ofThree * 0.55, ofThree * 1.0);
        // With the pictures' names taken out of the words: none of that dark.
        var none = guide with
        {
            Sections = [.. guide.Sections.Select(section => section with { Picture = null, Items = section.Items?.Select(item => item with { Picture = null }).ToList() })],
        };
        var without = Page(none, 780, 3200);
        Assert.Empty(without.PicturesShown);
        Assert.True(Dark(Pictures.Drawn(without, 780, 3200)) < ofThree * 0.06);
        // With one left out: two pictures' worth, and not three.
        var two = guide with { Sections = [.. guide.Sections.Select((section, n) => n == 0 ? section with { Picture = null } : section)] };
        var twoDark = Dark(Pictures.Drawn(Page(two, 780, 3200), 780, 3200));
        Assert.InRange(twoDark, dark * 0.5, dark * 0.8);
    });

    [Fact]
    public void WhereTheGuideCannotBeFoundThePageSaysSoAndIsStillAPageAndAPictureThatIsNotThereHasItsWordsInItsPlace() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var view = Page(null);
        Assert.Equal("The guide could not be found in this copy of the app.", Plain(view.Says));
        Assert.Empty(view.PicturesShown);
        var drawn = Pictures.Drawn(view, 780, 820);
        Assert.True(drawn.Dots.Distinct().Count() > 4);

        // A picture the app has not got: what it shows is said in words where it would be.
        var guide = GuideFiles.Guide!;
        var missing = guide with
        {
            Sections = [.. guide.Sections.Select((section, n) => n == 0 ? section with { Picture = "no-such-picture" } : section)],
            Pictures = new Dictionary<string, string>(guide.Pictures, StringComparer.Ordinal) { ["no-such-picture"] = "What the picture would have shown." },
        };
        var page = Page(missing);
        Assert.Equal(2, page.PicturesShown.Count);
        Assert.Contains("What the picture would have shown.", page.Says, StringComparison.Ordinal);
    });

    [Fact]
    public void ThePageIsLightOrDarkAsTheSystemIsAndItsPicturesAreOfTheSheetWhichIsDarkInBoth() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var guide = GuideFiles.Guide!;
        static Color At((int Width, int Height, int[] Dots) picture, double x, double y)
        {
            var dot = picture.Dots[((int)(y * Pictures.Scale) * picture.Width) + (int)(x * Pictures.Scale)];
            return Color.FromRgb((byte)(dot >> 16), (byte)(dot >> 8), (byte)dot);
        }
        foreach (var dark in new[] { false, true })
        {
            var view = Page(guide, dark: dark);
            var drawn = Pictures.Drawn(view, 780, 820);
            // The page's ground, at its margin: light, or dark.
            Assert.Equal(dark, Look.IsDark(At(drawn, 8, 400)));
            // In the middle of the first picture's strip: the sheet's own dark, in both.
            var picture = view.PicturesShown[0];
            var corner = picture.TranslatePoint(new Point(picture.ActualWidth / 2, 6), view);
            Assert.True(Look.IsDark(At(drawn, corner.X, corner.Y)));
        }
    });

    // ---- Where the window opens (rule 2)

    [Fact]
    public void ItOpensToOneSideOfTheMainWindowWhereTheScreenHasRoomAndOverItsRightHandPartWhereItHasNone()
    {
        var screen = new Rect(0, 0, 2560, 1400);
        var size = new Size(780, 820);
        // Room at the right of the main window: beside it there.
        Assert.Equal(new Point(1412, 130), GuideWindow.PlaceBeside(new Rect(100, 100, 1300, 800), screen, size));
        // None at the right, and room at the left.
        Assert.Equal(new Point(1500 - 12 - 780, 130), GuideWindow.PlaceBeside(new Rect(1500, 100, 1000, 800), screen, size));
        // Room at neither side: at the right of the screen, over the main window.
        Assert.Equal(new Point(1920 - 780 - 12, 30), GuideWindow.PlaceBeside(new Rect(0, 0, 1920, 1040), new Rect(0, 0, 1920, 1040), size));
        // Never off the top or the bottom of the screen.
        Assert.Equal(1040 - 820, GuideWindow.PlaceBeside(new Rect(0, 600, 800, 400), new Rect(0, 0, 1920, 1040), size).Y);
        Assert.Equal(0, GuideWindow.PlaceBeside(new Rect(0, -200, 800, 400), new Rect(0, 0, 1920, 1040), size).Y);
    }
}
