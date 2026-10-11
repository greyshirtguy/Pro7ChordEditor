// What the window shows, and what a click in it does, with no drawing in it: the places
// found, the libraries of the one chosen, the songs of the library chosen, the song that
// is open in its two editors, and its earlier versions. spec/06-library.md, "The
// window"; spec/03-chord-sheet.md; spec/04-chordpro-editor.md; spec/07-backups.md,
// "Seeing them" and "Getting one back".
//
// The views only show what is here and pass on what the person does. Everything that is
// a rule is in the layers below (Song: Places, Listing, Marks, SongText, Copies,
// OpenSong, ChordProEditor), which this calls; so that the rules are proved by
// `dotnet test` with no window, and are the same in the Mac app. The words the person
// reads are here too, and not in the views, so that a test can hold them.
//
// A song is opened with its real safeguards: nothing is saved while ProPresenter runs,
// nor before a copy of the file is kept. Four things here change a song's file: what is
// done on the chord sheet (spec/03; the rules of that are in Song/ChordSheet.cs, which
// asks the song itself, and this is told when it has), what is typed in the ChordPro
// editor (spec/04; the rules of that are in Song/ChordProEditor.cs, and this only says
// when to save), the two keys in the song's header (spec/05-song-and-saving.md, rules 5
// to 7), and putting an earlier version back, or the song as it was opened (spec/07).
//
// THE TWO EDITORS KEEP TO ONE SONG. Each asks the song, and neither remembers what the
// song has. So when one has changed it the other is made again from it before it is
// next shown: the sheet at once (`Reload`), the text when it is come back to
// (`textIsStale`), since making the text again forgets what its box could undo.
//
// NOTHING IS SAVED BY HAND (spec/06, rule 12a). What is typed is saved a moment after
// the typing stops, and before anything else is done: `Settle` is asked first by
// everything here that leaves the song or does something else to it, and if a save is
// refused, that something is not done (spec/04, rule 19a).
//
// It is handed what it would otherwise ask the PC for (where the app keeps its own
// things, how to find places, whether ProPresenter is running), so that tests can hand
// it made-up ones. `OnThisPC` hands it the real ones.
//
// ONE THREAD. Everything here is to be asked from one thread, the window's. A song's
// marks are worked out on another, since a big library takes a while, and come back to
// the window's thread to be shown; and so do the waits for the typing to stop and for
// what was just said to go: which they do by themselves where that thread has a queue
// of its own, as a window's has. (The tests give theirs one:
// Pictures.OnAThreadOfItsOwn.)
//
// The Mac's is LibraryModel.swift.

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed class LibraryModel : INotifyPropertyChanged
{
    /// <summary>How much of the height of the two lists the libraries have, unless the bar between them was moved.</summary>
    public const double UsualListsSplit = 0.36;

    /// <summary>How many times its usual size the ChordPro text is, unless it was changed.</summary>
    public const double UsualTextZoom = 1.0;

    /// <summary>How many times its usual size the chord sheet is drawn, unless it was changed (spec/03, rule 14a).</summary>
    public const double UsualSheetZoom = 0.8;

    /// <summary>The sizes the text can be, each so many times its usual one (spec/03, rule 14a; spec/04, rule 25).</summary>
    public static readonly IReadOnlyList<double> Zooms = [0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5];

    // How many songs' marks are worked out at a time before they are shown.
    private const int AFew = 20;

    private readonly AppData appData;
    private readonly Copies copies;
    private readonly Func<IReadOnlyList<string>, PlaceFinder> finder;
    private readonly Func<bool> askWhetherProPresenterIsRunning;
    private bool started;
    private KeptMarks keptMarks = new();
    private Task marking = Task.CompletedTask;
    private CancellationTokenSource? markingStop;
    private List<string> chosenFolders = [];
    private Dictionary<string, Marks> marks = new(Paths.ById);
    private IReadOnlySet<string> songsWithCopies = new HashSet<string>();
    private OpenSong? openSong;
    private ChordProEditor? editor;
    private int textVersion;
    private int sitting;
    private string typed = "";
    private MarkFilter filter = MarkFilter.All;

    // The wait for the typing to stop, before what was typed is saved.
    private Task pause = Task.CompletedTask;
    private CancellationTokenSource? pauseStop;

    // The wait for what was just said to go from the quiet line.
    private Task saying = Task.CompletedTask;
    private CancellationTokenSource? sayingStop;

    // Whether what the quiet line says went wrong is that ProPresenter was running: that
    // goes from the line when ProPresenter does.
    private bool refusedForProPresenter;

    // Whether the song refused the last thing the sheet asked of it.
    private bool sheetRefused;

    // Whether the song has been changed otherwise than by typing in the text, since the
    // text was made from it: the text is then made again before it is next shown.
    private bool textIsStale;

    // The song's file as it was when the song was opened here: what Revert puts back.
    private byte[]? asOpened;

    /// <param name="appVersion">The app's version, which a copy's index records.</param>
    public LibraryModel(AppData appData, Func<IReadOnlyList<string>, PlaceFinder> finder, Func<bool> proPresenterIsRunning, string appVersion = "")
    {
        this.appData = appData;
        copies = appData.Copies(appVersion);
        this.finder = finder;
        askWhetherProPresenterIsRunning = proPresenterIsRunning;
    }

    public static LibraryModel OnThisPC() => new(AppData.OnThisPC(), chosen => PlaceFinder.OnThisPC(chosen), ProPresenterApp.IsRunning, AppVersion);

    /// <summary>The version of the app that is running, as "0.1.0"; "" if it has none.</summary>
    public static string AppVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version is { } version ? version.ToString(3) : "";

    /// <summary>
    /// Something here is no longer what it was. It does not say what: whoever shows this
    /// asks again for what it shows.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>A song as it is shown: its lines, or why it could not be read.</summary>
    public sealed record ShownSong(string Name)
    {
        /// <summary>The key its chords are written in, and the key ProPresenter shows it in; "" if it states none.</summary>
        public string Key { get; init; } = "";

        public string ShownKey { get; init; } = "";

        public IReadOnlyList<SongLine> Lines { get; init; } = [];

        public int Slides { get; init; }

        public int Chords { get; init; }

        /// <summary>If it could not be read, the sentence that says so. Then there are no lines.</summary>
        public string? Problem { get; init; }
    }

    /// <summary>A song as the list shows it: its name, and what its marks say in words.</summary>
    public sealed record SongRow(string Id, string Name, Marks? Marks, bool HasCopies = false)
    {
        public bool CannotBeRead => Marks?.CannotBeRead == true;

        public bool IsOldEditorsCopy => Marks?.IsOldEditorsCopy == true;

        public bool HasChords => Marks?.HasChords == true;

        public static string CannotBeReadTip => "This file could not be read as a ProPresenter presentation.";

        // (The copies' ending is set between the quotation marks that curl, with the
        // three dots that stand for the song's name before it.)
        public static string OldEditorsCopyTip =>
            "A copy saved by the first Windows chord editor, which named its copies " + (char)0x201C + (char)0x2026 + Marks.OldEditorsEnding + (char)0x201D + ".";

        public static string HasCopiesTip => "Earlier versions of this song are kept.";

        public string ChordsTip => Marks is null ? "" : Marks.Chords == 1 ? "Has 1 chord." : "Has " + Number(Marks.Chords) + " chords.";

        /// <summary>
        /// The row in words: the song's name, and what each of its marks says. It is
        /// what a screen reader says of the row, which has the marks only as small
        /// pictures.
        /// </summary>
        public string Said
        {
            get
            {
                var said = new List<string> { Name + "." };
                if (CannotBeRead)
                {
                    said.Add(CannotBeReadTip);
                    return string.Join(' ', said);
                }
                if (HasChords)
                {
                    said.Add(ChordsTip);
                }
                if (HasCopies)
                {
                    said.Add(HasCopiesTip);
                }
                if (IsOldEditorsCopy)
                {
                    said.Add(OldEditorsCopyTip);
                }
                return string.Join(' ', said);
            }
        }
    }

    /// <summary>The copies of the song that is open, and the one picked to be looked at.</summary>
    public sealed record EarlierVersions(IReadOnlyList<KeptCopy> Copies)
    {
        public string? PickedId { get; init; }

        /// <summary>The copy picked, as it was: its lines, to look at and not to change.</summary>
        public ShownSong? Picked { get; init; }

        /// <summary>One line that sets it against the song as it is now: how many chords each has, and their keys.</summary>
        public string Comparison { get; init; } = "";

        /// <summary>If putting the picked one back wants asking first (spec/07, rule 23): the question.</summary>
        public string? Question { get; init; }
    }

    /// <summary>
    /// What was just put back, for the few seconds in which the line under the song
    /// offers to undo it.
    /// </summary>
    /// <param name="Sentence">"Put back the version of 4 Oct."</param>
    /// <param name="Before">The copy that was made just before: putting THAT back is the undoing.</param>
    public sealed record PutBack(string Sentence, KeptCopy Before);

    /// <summary>The song as ChordPro text, as the editor shows it.</summary>
    /// <param name="Text">The text, as it last passed the editor's rule.</param>
    /// <param name="Version">
    /// Counted up each time the text is set from here and not typed: the view then puts
    /// it in place of what it has, and forgets its own undo.
    /// </param>
    /// <param name="Sitting">
    /// Counted up each time a song is opened or put back: the view then starts at the
    /// top of the text. (Otherwise the caret stays where it was.)
    /// </param>
    /// <param name="Barred">Why the song can be read here and not changed, as a sentence; null if it can.</param>
    public sealed record ChordProShown(string Text, int Version, int Sitting, string? Barred);

    /// <summary>
    /// A question put to the person before a song is reverted: its two sentences, and
    /// what the button that says yes says.
    /// </summary>
    public sealed record Question(string Title, string Message, string Yes);

    /// <summary>The two ways a song is worked on (spec/06, rule 12).</summary>
    public enum Editor
    {
        /// <summary>The chord sheet: chords put on with the mouse and the keys.</summary>
        Sheet,

        /// <summary>The song as ChordPro text, typed in.</summary>
        ChordPro,
    }

    /// <summary>
    /// What the quiet line says while the sheet is shown: what the keys do
    /// (spec/10-platforms.md has the words, with Ctrl where the Mac has its own keys).
    /// </summary>
    public static string SheetHelp { get; } =
        "1 to 7: the key" + (char)0x2019 + "s chords  " + (char)0xB7 + "  A to G: type one, and a click on the next place keeps it  " + (char)0xB7 + "  "
        + "Drag moves a chord, with Ctrl copies it  " + (char)0xB7 + "  Delete removes  " + (char)0xB7 + "  Ctrl+C and Ctrl+V: a line" + (char)0x2019 + "s chords onto another";

    // ---- What is shown

    public IReadOnlyList<Place> Places { get; private set; } = [];

    public string? PlaceId { get; private set; }

    public IReadOnlyList<Library> Libraries { get; private set; } = [];

    public string? LibraryId { get; private set; }

    public IReadOnlyList<ListedSong> Songs { get; private set; } = [];

    /// <summary>The marks of the songs listed, by the song's id, as far as they are worked out.</summary>
    public IReadOnlyDictionary<string, Marks> Marks => marks;

    public string? SongId { get; private set; }

    public ShownSong? Shown { get; private set; }

    /// <summary>The song as ChordPro text; null if no song is open, or it could not be read.</summary>
    public ChordProShown? ChordPro { get; private set; }

    /// <summary>Which of the two editors the song is shown in (spec/06, rule 12).</summary>
    public Editor EditorShown { get; private set; } = Editor.Sheet;

    /// <summary>
    /// The song as the chord sheet has it; null if no song is open, or it could not be
    /// read. Its view reads it and tells it what was done; what it asks of the song
    /// comes back here.
    /// </summary>
    public ChordSheet? Sheet { get; private set; }

    /// <summary>
    /// Goes up whenever the sheet was changed from outside its own view, which is then to
    /// lay it out and draw it again.
    /// </summary>
    public int SheetVersion { get; private set; }

    /// <summary>
    /// How many times its usual size the sheet is drawn: one of <see cref="Zooms"/>. The
    /// words are smaller or larger by it, and so the slides' cards, and so how many of
    /// them there are to a row (spec/03, "Zooming").
    /// </summary>
    public double SheetZoom { get; private set; } = UsualSheetZoom;

    /// <summary>
    /// The colour of a chord's name on the sheet, and of a chord in the ChordPro text,
    /// each as "#rrggbb"; nothing where the usual colour is kept (spec/06, "Settings").
    /// Only how chords look in this app: nothing in any song's file says a colour.
    /// </summary>
    public string? SheetChordColour { get; private set; }

    public string? TextChordColour { get; private set; }

    public bool ProPresenterIsRunning { get; private set; }

    /// <summary>The panel of earlier versions, while it is open.</summary>
    public EarlierVersions? Earlier { get; private set; }

    public PutBack? JustPutBack { get; private set; }

    /// <summary>What went wrong with the last thing asked, as a sentence; nothing if nothing did.</summary>
    public string? Refused { get; private set; }

    /// <summary>
    /// Whether that was that the file has been changed by something else, so that the
    /// song is offered read again.
    /// </summary>
    public bool OffersOpeningAgain { get; private set; }

    /// <summary>What was just said ("Saved. Every change here ..."), for a few seconds.</summary>
    public string? Said { get; private set; }

    /// <summary>What Revert asks before it does anything, while it is asking.</summary>
    public Question? RevertQuestion { get; private set; }

    /// <summary>
    /// Whether the song's file is no longer what it was when the song was opened here:
    /// something has been changed since, and Revert has something to do.
    /// </summary>
    public bool ChangedSinceOpened { get; private set; }

    /// <summary>
    /// How much of the height of the two lists at the window's left the libraries have,
    /// the songs having the rest: what the bar between them is dragged to.
    /// </summary>
    public double ListsSplit { get; private set; } = UsualListsSplit;

    /// <summary>
    /// How many times its usual size the ChordPro text is: one of <see cref="Zooms"/>.
    /// Only the size changes with it: not a character of the text.
    /// </summary>
    public double TextZoom { get; private set; } = UsualTextZoom;

    /// <summary>How long after the typing stops a change is saved (spec/04, rule 13).</summary>
    public TimeSpan PauseBeforeSaving { get; set; } = TimeSpan.FromMilliseconds(700);

    /// <summary>How long what was just said stays on the quiet line.</summary>
    public TimeSpan SayingLasts { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>What is typed in the search box.</summary>
    public string Typed
    {
        get => typed;
        set
        {
            if (!string.Equals(typed, value, StringComparison.Ordinal))
            {
                typed = value;
                Changed();
            }
        }
    }

    /// <summary>What the list is narrowed to by mark.</summary>
    public MarkFilter Filter
    {
        get => filter;
        set
        {
            if (filter != value)
            {
                filter = value;
                Changed();
            }
        }
    }

    public Place? Place => Places.FirstOrDefault(place => Paths.Same(place.Id, PlaceId));

    public Library? Library => Libraries.FirstOrDefault(library => Paths.Same(library.Id, LibraryId));

    /// <summary>
    /// The songs of the library, narrowed by what is typed and by mark. The song that is
    /// open stays in the list whatever it is narrowed to: its first chord must not take
    /// it out of "without chords" from under the person typing it.
    /// </summary>
    public IReadOnlyList<ListedSong> ListedSongs
    {
        get
        {
            var narrowed = Listing.Narrowed(Songs, typed).Where(song => filter.Lets(marks.GetValueOrDefault(song.Id))).Select(song => song.Id).ToHashSet(Paths.ById);
            return [.. Songs.Where(song => narrowed.Contains(song.Id) || Paths.Same(song.Id, SongId))];
        }
    }

    /// <summary>The songs listed, each as its row shows it.</summary>
    public IReadOnlyList<SongRow> Rows => [.. ListedSongs.Select(song => new SongRow(song.Id, song.Name, marks.GetValueOrDefault(song.Id), HasCopies(song)))];

    /// <summary>Whether the song that is open has copies kept.</summary>
    public bool HasEarlierVersions => Songs.FirstOrDefault(song => Paths.Same(song.Id, SongId)) is { } song && HasCopies(song);

    /// <summary>
    /// Whether the song that is open can be changed just now: it was read, and
    /// ProPresenter is not running.
    /// </summary>
    public bool CanChange => openSong is not null && !ProPresenterIsRunning;

    /// <summary>Whether its text can be typed in: that, and the text can say the song exactly (spec/04, rules 20 and 20a).</summary>
    public bool CanType => CanChange && ChordPro is { Barred: null };

    /// <summary>The keys a song can be said to be in, as the pickers list them: the thirteen major, then the thirteen minor.</summary>
    public static IReadOnlyList<string> Keys { get; } = [.. Chords.MajorKeys(), .. Chords.MinorKeys()];

    /// <summary>
    /// The keys a picker lists for a song that says it is in this one: the twenty-six;
    /// before them the song's own key, if it is one of the other sixteen (C# major,
    /// say), so that it can always be seen and chosen (spec/02-chord-rules.md); and
    /// before them all nothing, which a picker shows as "None", if the song states no
    /// key (spec/06, rule 12).
    /// </summary>
    public static IReadOnlyList<string> KeysListedFor(string key) =>
        key.Length == 0 || !Keys.Contains(key) ? [key, .. Keys] : Keys;

    /// <summary>Whether Revert has something to do, and may do it.</summary>
    public bool CanRevert => CanChange && ChangedSinceOpened;

    /// <summary>
    /// What stands at the right of the quiet line, where a Save button would be: there
    /// is nothing to save by hand, since every change is saved as it is made. While
    /// ProPresenter is running nothing is saved at all.
    /// </summary>
    public string SavingCaption => CanChange ? "Autosave on" : "Autosave paused";

    /// <summary>The tip of those words: why there is no Save button (spec/06, rule 12a).</summary>
    public string SavingTip => CanChange
        ? "There is nothing to save by hand: every change is saved as it is made, with a copy of the song as it was kept first."
        : "Nothing is saved while ProPresenter is running.";

    public bool CanZoomTextIn => TextZoom < Zooms[^1];

    public bool CanZoomSheetIn => SheetZoom < Zooms[^1];

    public bool CanZoomSheetOut => SheetZoom > Zooms[0];

    /// <summary>
    /// The song's own history, which every change of either editor is a step of: whether
    /// the last change can be taken back, and put again (spec/03, rule 53).
    /// </summary>
    public bool CanUndo => CanChange && openSong?.CanUndo == true;

    public bool CanRedo => CanChange && openSong?.CanRedo == true;

    public bool CanZoomTextOut => TextZoom > Zooms[0];

    /// <summary>What a place is called where one is picked: its name, and how it was found.</summary>
    public static string Describe(Place place) => place.Kind switch
    {
        PlaceKind.NamedInSettings => place.Name + " (ProPresenter" + (char)0x2019 + "s folder)",
        PlaceKind.Documents => "ProPresenter in Documents",
        _ => place.Name + " (chosen folder)",
    };

    /// <summary>
    /// What stands where the place would be picked, if no place was found. It is not an
    /// error: someone may keep a copy of a library elsewhere, or have no ProPresenter on
    /// this PC at all. Nothing, if there is a place.
    /// </summary>
    public string NoPlaceSentence => started && Places.Count == 0 ? "No ProPresenter library was found on this PC." : "";

    /// <summary>What stands where the libraries would be listed, if the place has none. Nothing, if it has.</summary>
    public string NoLibrariesSentence => Places.Count > 0 && Libraries.Count == 0 ? "This folder has no libraries in it." : "";

    /// <summary>What stands where the song would be, while none is open.</summary>
    public string NoSongSentence =>
        Shown is not null || Library is null ? "" : Songs.Count == 0 ? "There are no songs in this library." : "Choose a song to look at it.";

    /// <summary>
    /// What the bar between the two lists says of the one under it; nothing while no
    /// library is open.
    /// </summary>
    public string SongsCount => Library is null ? "" : ListsLook.Count(ListedSongs.Count, Songs.Count);

    /// <summary>
    /// The quiet line under the song: what went wrong, if something did; what was just
    /// put back, or said; or what is there to be seen. Nothing here is a promise of what
    /// the app will do.
    /// </summary>
    public string Notice
    {
        get
        {
            if (Refused is not null)
            {
                return Refused;
            }
            if (JustPutBack is not null)
            {
                return JustPutBack.Sentence;
            }
            if (Shown is not null)
            {
                if (Shown.Problem is not null)
                {
                    return "Nothing was done to this file.";
                }
                // What was just said (a copy, a paste, Ctrl+S) is said in either editor.
                if (Said is not null)
                {
                    return Said;
                }
                if (EditorShown == Editor.Sheet)
                {
                    return SheetHelp;
                }
                var slides = Shown.Slides == 1 ? "1 slide" : Number(Shown.Slides) + " slides";
                var chords = Shown.Chords == 1 ? "1 chord" : Number(Shown.Chords) + " chords";
                // (There is no Save button to look for, so it says there is no need of
                // one; but only where something can be typed.)
                return slides + ", " + chords + (CanType ? ". A change is saved a moment after it is typed." : ".");
            }
            if (Library is not null)
            {
                var withChords = Number(Songs.Count(song => marks.GetValueOrDefault(song.Id)?.HasChords == true));
                return Songs.Count == 1 ? "1 song, " + withChords + " with chords." : Number(Songs.Count) + " songs, " + withChords + " with chords.";
            }
            return "";
        }
    }

    // ---- What the person does

    /// <summary>
    /// When the window first shows: what the app kept is read, copies that are kept no
    /// longer are thrown away, the places are found, and the place and library last open
    /// are opened again. Asked a second time it does nothing.
    /// </summary>
    public async Task StartAsync()
    {
        if (started)
        {
            return;
        }
        started = true;
        var settings = appData.ReadSettings();
        chosenFolders = [.. settings.ChosenFolders];
        ListsSplit = Within(settings.ListsSplit ?? UsualListsSplit);
        TextZoom = NearestZoom(settings.TextZoom ?? UsualTextZoom);
        SheetZoom = NearestZoom(settings.SheetZoom ?? UsualSheetZoom);
        SheetChordColour = ColourAsKept(settings.SheetChordColour);
        TextChordColour = ColourAsKept(settings.TextChordColour);
        keptMarks = appData.ReadMarks();
        // Copies that are kept no longer go now, when the app starts (spec/07, rule 12).
        var thrownAway = copies.Thin();
        if (thrownAway > 0)
        {
            appData.Log(Number(thrownAway) + " old copies were thrown away.");
        }
        FindPlaces(settings.LastPlace, settings.LastLibrary);
        ProPresenterIsRunning = askWhetherProPresenterIsRunning();
        appData.Log("Started. " + Number(Places.Count) + " places found, " + Number(Libraries.Count) + " libraries in the one open.");
        Changed();
        await MarksAreWorkedOutAsync();
    }

    /// <summary>
    /// When the app comes to the front: the lists are read again, since ProPresenter may
    /// have added songs meanwhile, and whether it is running is asked again.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (!started)
        {
            return;
        }
        Settle();
        var song = SongId;
        FindPlaces(PlaceId, LibraryId);
        ProPresenterIs(askWhetherProPresenterIsRunning());
        // The song that is open stays open, with what was typed in it, if its file is
        // still the one that was read. If something else has changed the file meanwhile,
        // it is read again: nothing could be saved over it anyway. (Not while the panel
        // of earlier versions is open over it: that is put away by the person, not by
        // the app coming to the front.)
        if (song is not null && Songs.Any(one => Paths.Same(one.Id, song)) && Earlier is null && openSong?.FileIsAsLastRead != true)
        {
            Open(song);
        }
        Changed();
        await MarksAreWorkedOutAsync();
    }

    /// <summary>
    /// A moment has gone by. Whether ProPresenter is running is asked again, so that the
    /// band comes and goes by itself and not only when the app is next brought to the
    /// front (spec/06, rule 18): ProPresenter can be started or quit with this app in
    /// front all the while. Windows gives no news of a program starting or stopping that
    /// an app can have for the asking, so the window asks every second or two.
    /// </summary>
    public void ProPresenterMayHaveChanged()
    {
        if (ProPresenterIs(askWhetherProPresenterIsRunning()))
        {
            Changed();
        }
    }

    /// <summary>The same, with the asking done off the window's thread: the system's list of what is running takes a moment to read.</summary>
    public async Task LookForProPresenterAsync()
    {
        if (ProPresenterIs(await Task.Run(askWhetherProPresenterIsRunning)))
        {
            Changed();
        }
    }

    // Answers whether it is another thing than was known.
    private bool ProPresenterIs(bool running)
    {
        if (running == ProPresenterIsRunning)
        {
            return false;
        }
        ProPresenterIsRunning = running;
        appData.Log(running ? "ProPresenter is running." : "ProPresenter is no longer running.");
        if (!running && refusedForProPresenter)
        {
            // What was refused because of it can be done now: the line need not go on
            // saying otherwise.
            Refused = null;
            refusedForProPresenter = false;
        }
        // (The sheet draws what can and cannot be done on it now.)
        SheetVersion++;
        return true;
    }

    public void ChoosePlace(string? id)
    {
        if (Paths.Same(id, PlaceId) || !Settle())
        {
            return;
        }
        PlaceId = id;
        ListLibraries(null);
        Remember();
        Changed();
    }

    public void ChooseLibrary(string? id)
    {
        if (Paths.Same(id, LibraryId) || !Settle())
        {
            return;
        }
        LibraryId = id;
        ListSongs();
        Remember();
        Changed();
    }

    /// <summary>
    /// A click on a song opens it. One that cannot be read says so, and nothing is done
    /// to it. What was being typed in the song that is left is saved first; if that is
    /// refused, the song is not left.
    /// </summary>
    public void Open(string? id)
    {
        if (!Settle())
        {
            return;
        }
        SongId = id;
        Earlier = null;
        JustPutBack = null;
        Refused = null;
        refusedForProPresenter = false;
        OffersOpeningAgain = false;
        Close();
        var song = Songs.FirstOrDefault(one => Paths.Same(one.Id, id));
        if (song is null || KeptSongOf(song) is not { } kept)
        {
            SongId = null;
            Shown = null;
            Changed();
            return;
        }
        var file = Path.GetFileName(song.Path);
        try
        {
            // With its safeguards: ProPresenter is asked after before every save, and a
            // copy is kept before the first.
            var opened = new OpenSong(song.Path, new Safeguards(askWhetherProPresenterIsRunning, (path, why) => copies.Keep(path, kept, why)));
            openSong = opened;
            asOpened = opened.Bytes.ToArray();
            ChangedSinceOpened = false;
            Shown = Show(opened, song.Name);
            Edit(opened);
            appData.Log("Opened " + file + ".");
        }
        catch (Refusal refusal)
        {
            Shown = new ShownSong(song.Name) { Problem = refusal.Sentence };
            appData.Log("Could not read " + file + ".");
        }
        Changed();
    }

    /// <summary>"Open Again": the song read afresh from its file, after something else changed it.</summary>
    public void OpenAgain() => Open(SongId);

    /// <summary>A folder chosen by hand becomes a place, is remembered, and is opened.</summary>
    public void Add(string folder)
    {
        if (!Settle())
        {
            return;
        }
        if (!chosenFolders.Any(one => Paths.Same(Paths.IdOf(one), Paths.IdOf(folder))))
        {
            chosenFolders.Add(folder);
        }
        FindPlaces(Paths.IdOf(folder), null);
        Remember();
        appData.Log("A folder was chosen: " + Paths.NameOf(folder) + ".");
        Changed();
    }

    /// <summary>
    /// The bar between the libraries and the songs was dragged, to where the libraries
    /// have this much of the height; or, with nothing, put back where it usually is. It
    /// is remembered.
    /// </summary>
    public void SetListsSplit(double? share)
    {
        ListsSplit = Within(share ?? UsualListsSplit);
        if (started)
        {
            Remember();
        }
        Changed();
    }

    /// <summary>Waits until the marks of the songs listed have been worked out.</summary>
    public Task MarksAreWorkedOutAsync() => marking;

    // ---- The ChordPro editor (spec/04-chordpro-editor.md)

    /// <summary>
    /// The text box asks: may the text become this? If it may, it is taken, and saved a
    /// moment after the typing stops. If it may not, the text is to stay as it was, and
    /// nothing is said: the caret not moving says it.
    /// </summary>
    public bool TextMayBecome(string text)
    {
        if (editor is null || !CanType || editor.Take(text) == ChordProEditor.Verdict.NotTheWords)
        {
            return false;
        }
        Refused = null;
        refusedForProPresenter = false;
        OffersOpeningAgain = false;
        if (ChordPro is not null)
        {
            ChordPro = ChordPro with { Text = text };
        }
        pauseStop?.Cancel();
        pauseStop = new CancellationTokenSource();
        pause = SettleAfterAPauseAsync(pauseStop.Token);
        Changed();
        return true;
    }

    private async Task SettleAfterAPauseAsync(CancellationToken stop)
    {
        try
        {
            await Task.Delay(PauseBeforeSaving, stop);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (!stop.IsCancellationRequested)
        {
            Settle();
        }
    }

    /// <summary>
    /// Saves what has been typed, without waiting for the typing to pause: before the
    /// song is left for another, before anything else is done to it, and when the app is
    /// left or closed. Answers whether all is saved; false if a save was refused, and
    /// then whatever was to be done next is not done, so that the sentence is seen and
    /// not swept away with the song.
    /// </summary>
    public bool Settle()
    {
        pauseStop?.Cancel();
        pauseStop = null;
        // A chord being typed on the sheet is kept first (spec/03, rule 39).
        if (Sheet is { Typing: true } typing)
        {
            sheetRefused = false;
            typing.Settle();
            SheetVersion++;
            Changed();
            if (sheetRefused)
            {
                return false;
            }
        }
        if (editor is null || openSong is not { } song)
        {
            return true;
        }
        var was = song.Bytes;
        try
        {
            if (editor.Save())
            {
                NoteAChange();
                Changed();
            }
            return true;
        }
        catch (Refusal refusal)
        {
            // What was saved before the refusal stays saved. The text is made again
            // from the song, so that what is seen is what the sentence says: nothing
            // (more) was changed (spec/04, rule 19a).
            Refuse(refusal, "");
            editor.Reload(keepingWhatIsTyped: false);
            ShowText(editor);
            NoteAChange(saved: !was.Span.SequenceEqual(song.Bytes.Span));
            Changed();
            return false;
        }
    }

    /// <summary>Waits until what was typed has been saved, if a save is waiting.</summary>
    public Task WhatWasTypedIsSavedAsync() => pause;

    /// <summary>
    /// Makes the ChordPro text a step larger (1) or smaller (-1), or its usual size (0).
    /// It is remembered. Answers with the size it now is.
    /// </summary>
    public double ZoomText(int direction)
    {
        var now = Zooms.ToList().IndexOf(NearestZoom(TextZoom));
        var wanted = direction == 0 ? UsualTextZoom : Zooms[Math.Min(Math.Max(now + Math.Sign(direction), 0), Zooms.Count - 1)];
        if (wanted != TextZoom)
        {
            TextZoom = wanted;
            if (started)
            {
                Remember();
            }
            Changed();
        }
        return TextZoom;
    }

    // ---- The two keys (spec/05-song-and-saving.md, rules 5 to 7)

    /// <summary>Says what key the song's chords are written in. It changes no chord.</summary>
    public void SetKey(string key) => Change(song => song.SetKey(key));

    /// <summary>Says what key ProPresenter is to show the song's chords in, and that alone.</summary>
    public void SetShownKey(string key) => Change(song => song.SetShownKey(key));

    private void Change(Action<OpenSong> ask)
    {
        if (!Settle() || openSong is not { } song)
        {
            return;
        }
        Refused = null;
        refusedForProPresenter = false;
        OffersOpeningAgain = false;
        try
        {
            ask(song);
            NoteAChange();
        }
        catch (Refusal refusal)
        {
            Refuse(refusal, "");
        }
        Sheet?.Reload();
        SheetVersion++;
        Changed();
    }

    // ---- The chord sheet (spec/03-chord-sheet.md)

    /// <summary>
    /// Shows the song in the other editor. What was being typed in the one that is left
    /// is kept first, and the one that is come to is made again from the song, since the
    /// other may have changed it.
    /// </summary>
    public void Show(Editor wanted)
    {
        if (wanted == EditorShown || !Settle())
        {
            return;
        }
        if (wanted == Editor.ChordPro)
        {
            if (editor is not null && textIsStale)
            {
                editor.Reload(keepingWhatIsTyped: false);
                ShowText(editor);
            }
        }
        else
        {
            Sheet?.Reload();
        }
        EditorShown = wanted;
        SheetVersion++;
        Changed();
    }

    /// <summary>The song's last change taken back (spec/03, rule 53).</summary>
    public void Undo()
    {
        if (openSong?.CanUndo == true)
        {
            Change(song => song.Undo());
            textIsStale = true;
        }
    }

    /// <summary>The change last taken back, made again.</summary>
    public void Redo()
    {
        if (openSong?.CanRedo == true)
        {
            Change(song => song.Redo());
            textIsStale = true;
        }
    }

    /// <summary>
    /// Makes the sheet's words a step larger (1) or smaller (-1), or their usual size
    /// (0). It is remembered. Answers with the size it now is.
    /// </summary>
    public double ZoomSheet(int direction)
    {
        var now = Zooms.ToList().IndexOf(NearestZoom(SheetZoom));
        var wanted = direction == 0 ? UsualSheetZoom : Zooms[Math.Min(Math.Max(now + Math.Sign(direction), 0), Zooms.Count - 1)];
        if (wanted != SheetZoom)
        {
            SheetZoom = wanted;
            SheetVersion++;
            if (started)
            {
                Remember();
            }
            Changed();
        }
        return SheetZoom;
    }

    /// <summary>
    /// Chooses the colour of a chord's name on the sheet: "#rrggbb", or nothing for the
    /// usual one. What is no colour so written is taken as nothing. It is remembered.
    /// </summary>
    public void SetSheetChordColour(string? colour)
    {
        SheetChordColour = ColourAsKept(colour);
        SheetVersion++;
        if (started)
        {
            Remember();
        }
        Changed();
    }

    /// <summary>The same for a chord in the ChordPro text.</summary>
    public void SetTextChordColour(string? colour)
    {
        TextChordColour = ColourAsKept(colour);
        if (started)
        {
            Remember();
        }
        Changed();
    }

    /// <summary>
    /// A colour as it is kept: "#" and six of 0 to 9 and a to f, in small letters.
    /// Nothing for anything else.
    /// </summary>
    internal static string? ColourAsKept(string? written)
    {
        if (written is null || written.Length != 7 || written[0] != '#' || !written.Skip(1).All(char.IsAsciiHexDigit))
        {
            return null;
        }
        return written.ToLowerInvariant();
    }

    /// <summary>
    /// A click on one of the key's seven chords in the strip: on the spot, or where a
    /// chord was being typed, in place of what was typed.
    /// </summary>
    public void PutOfKey(int number)
    {
        if (Sheet is { } sheet)
        {
            sheet.PutOfKey(number);
            SheetVersion++;
            Changed();
        }
    }

    /// <summary>
    /// Copies the chords of the spot's line, or of its whole group, and says so on the
    /// quiet line for a few seconds.
    /// </summary>
    public void CopyChords(bool wholeGroup)
    {
        if (Sheet?.Copy(wholeGroup) is { } sentence)
        {
            Refused = null;
            refusedForProPresenter = false;
            Say(sentence);
        }
    }

    /// <summary>
    /// Puts what was copied on the lines from the spot's on, and says what came of it: a
    /// paste that changed nothing would otherwise look like a key that did not work, and
    /// there are plain reasons it may change nothing.
    /// </summary>
    public void PasteChords()
    {
        if (Sheet is not { } sheet)
        {
            return;
        }
        if (sheet.Copied is null)
        {
            Say("Nothing has been copied yet. Ctrl+C copies the chords of the line the spot is on.");
            return;
        }
        if (sheet.Paste() is not { } pasted)
        {
            return;
        }
        SheetVersion++;
        // (What the song refused has its own sentence on the line already.)
        if (pasted.Refused == 0 && WhatIsSaidOf(pasted) is { } sentence)
        {
            Say(sentence);
        }
        else
        {
            Changed();
        }
    }

    /// <summary>What is said of a paste (spec/03, rule 52a).</summary>
    internal static string? WhatIsSaidOf(ChordSheet.Pasted pasted)
    {
        const string otherKind = "chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none";
        if (pasted.Lines == 0)
        {
            return pasted.PassedOver > 0 ? "Nothing was put here: " + otherKind + "." : null;
        }
        if (pasted.Chords == 0)
        {
            return "None of the copied chords had a word to go on here.";
        }
        var sentence = Number(pasted.Chords) + (pasted.Chords == 1 ? " chord" : " chords") + " put on " + (pasted.Lines == 1 ? "the line" : Number(pasted.Lines) + " lines") + ".";
        if (pasted.LeftOut > 0)
        {
            sentence += " " + Number(pasted.LeftOut) + " had no word to go on, and " + (pasted.LeftOut == 1 ? "was" : "were") + " left out.";
        }
        if (pasted.PassedOver > 0)
        {
            sentence += " " + (pasted.PassedOver == 1 ? "1 line was" : Number(pasted.PassedOver) + " lines were") + " passed over: " + otherKind + ".";
        }
        return sentence;
    }

    // The sheet's song for a song just opened, or put back.
    private ChordSheet MakeSheet(OpenSong song) => new(song)
    {
        PasteKeys = "Ctrl+V",
        Refused = refusal =>
        {
            sheetRefused = true;
            Refuse(refusal, "");
            Changed();
        },
        Changed = SheetChanged,
    };

    // The song took a change the sheet asked for.
    private void SheetChanged()
    {
        Refused = null;
        refusedForProPresenter = false;
        OffersOpeningAgain = false;
        Said = null;
        textIsStale = true;
        NoteAChange();
        Changed();
    }

    // ---- Saving, which is not done by hand (spec/06, rule 12a; spec/07, rule 22a)

    /// <summary>
    /// Ctrl+S, pressed out of habit. What has been typed and not yet saved is saved
    /// without waiting for the pause, and the quiet line says that there was nothing to
    /// do by hand.
    /// </summary>
    public void SaveNow()
    {
        if (openSong is null)
        {
            return;
        }
        if (ProPresenterIsRunning)
        {
            Say("ProPresenter is running. Quit it to change chords here.");
            return;
        }
        if (Settle())
        {
            Say("Saved. Every change here is saved as it is made: there is nothing to save by hand.");
        }
    }

    // Puts a sentence on the quiet line for a few seconds.
    private void Say(string sentence)
    {
        Said = sentence;
        sayingStop?.Cancel();
        sayingStop = new CancellationTokenSource();
        saying = UnsayAfterAWhileAsync(sayingStop.Token);
        Changed();
    }

    private async Task UnsayAfterAWhileAsync(CancellationToken stop)
    {
        try
        {
            await Task.Delay(SayingLasts, stop);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (!stop.IsCancellationRequested)
        {
            Said = null;
            Changed();
        }
    }

    /// <summary>Waits until what was said has gone from the quiet line again.</summary>
    public Task WhatWasSaidIsGoneAsync() => saying;

    /// <summary>
    /// <b>Revert</b> asks first. It takes back everything done to the song since it was
    /// opened, which may be an hour's work, and its name alone could be read as taking
    /// back the last change, which is Undo's. So the question says which: all of them.
    /// (In one sentence, which is the maintainer's: a longer one was not read.)
    /// </summary>
    public void AskToRevert()
    {
        if (!CanRevert || Shown is null)
        {
            return;
        }
        RevertQuestion = new Question(
            "Revert " + (char)0x201C + Shown.Name + (char)0x201D + " to how it was when it was opened?",
            "This takes back EVERY change made to this song since it was opened in this editor.",
            "Revert All Changes");
        Changed();
    }

    /// <summary>The answer was no.</summary>
    public void DontRevert()
    {
        if (RevertQuestion is not null)
        {
            RevertQuestion = null;
            Changed();
        }
    }

    /// <summary>
    /// The answer was yes: the song is made what it was when it was opened here. It is a
    /// putting back like that of an earlier version: what the song is now is kept as a
    /// copy first, and the quiet line offers to undo it.
    /// </summary>
    public void Revert()
    {
        RevertQuestion = null;
        if (asOpened is not { } bytes || !ChangedSinceOpened)
        {
            Changed();
            return;
        }
        PutBackBytes(
            () => bytes,
            undoing: false,
            said: "The song is as it was when it was opened here.",
            logged: file => "Reverted " + file + " to as it was opened.",
            notDone: file => file + " was not reverted.");
    }

    // ---- Earlier versions (spec/07-backups.md, rules 14 to 23)

    /// <summary>Opens the panel, with the newest copy picked. A song with no copies has none to show.</summary>
    public void ShowEarlierVersions()
    {
        if (!Settle() || Songs.FirstOrDefault(one => Paths.Same(one.Id, SongId)) is not { } song || KeptSongOf(song) is not { } kept)
        {
            return;
        }
        var all = copies.CopiesOf(kept);
        if (all.Count == 0)
        {
            return;
        }
        Earlier = new EarlierVersions(all);
        PickCopy(all[0].Id);
    }

    /// <summary>When a copy was made, as the list says it: "Today 09:14", "Sunday 4 Oct".</summary>
    public string When(KeptCopy copy) => copies.When(copy.Made);

    /// <summary>What the list says of why a copy was made: the page's words, and that it is the first if it is.</summary>
    public static string Why(KeptCopy copy) =>
        (copy.IsTheFirst ? "(the first) " : "") + copy.Why switch
        {
            CopyReason.BeforeAChange => "before a change",
            CopyReason.BeforePuttingBack => "before putting back a copy",
            CopyReason.BeforeMending => "before mending",
            _ => "",
        };

    /// <summary>Esc, or a click outside, puts the panel away.</summary>
    public void CloseEarlierVersions()
    {
        if (Earlier is not null)
        {
            Earlier = null;
            Changed();
        }
    }

    /// <summary>Picking one shows it as it was, and one line that sets it against the song now.</summary>
    public void PickCopy(string? id)
    {
        if (Earlier is not { } panel || panel.Copies.FirstOrDefault(one => one.Id == id) is not { } copy)
        {
            return;
        }
        Earlier = panel with
        {
            PickedId = copy.Id,
            Question = null,
            Picked = Looked(copy),
            Comparison = Count(copy.Chords, copy.Key) + ".  Now: " + (Shown is null ? "" : Count(Shown.Chords, Shown.Key)) + ".",
        };
        Changed();
    }

    /// <summary>A copy as it was: its lines, to look at. Or, of one that cannot be read, the sentence that says so.</summary>
    public static ShownSong Looked(KeptCopy copy)
    {
        try
        {
            return Show(new OpenSong(copy.Path), copy.SongName);
        }
        catch (Refusal)
        {
            return new ShownSong(copy.SongName) { Problem = "This copy could not be read as a ProPresenter presentation." };
        }
    }

    /// <summary>
    /// "Put This One Back". A copy that belongs to another song than the one now at that
    /// place (another id inside) asks first, saying both names; <paramref name="sure"/>
    /// is the yes.
    /// </summary>
    public void PutBackPicked(bool sure = false)
    {
        if (Earlier is not { } panel || panel.Copies.FirstOrDefault(one => one.Id == panel.PickedId) is not { } copy || openSong is not { } song)
        {
            return;
        }
        if (!sure && copy.SongId.Length > 0 && !string.Equals(copy.SongId, song.Id, StringComparison.Ordinal))
        {
            Earlier = panel with
            {
                Question = "This copy is of " + (char)0x201C + copy.SongName + (char)0x201D + ", and the song here now is " + (char)0x201C + song.Name + (char)0x201D
                    + ": another song under the same name. Put the copy in its place?",
            };
            Changed();
            return;
        }
        PutBackCopy(copy, undoing: false);
    }

    /// <summary>Undoes the putting back, which is only putting back the copy made just before it.</summary>
    public void TakeBack()
    {
        if (JustPutBack is { } done)
        {
            PutBackCopy(done.Before, undoing: true);
        }
    }

    /// <summary>The offer to undo goes after a few seconds; the copy is in the list all the same.</summary>
    public void ForgetWhatWasPutBack()
    {
        if (JustPutBack is not null)
        {
            JustPutBack = null;
            Changed();
        }
    }

    private void PutBackCopy(KeptCopy copy, bool undoing)
    {
        var when = copies.When(copy.Made);
        PutBackBytes(
            () => File.ReadAllBytes(copy.Path),
            undoing,
            said: "Put back the version of " + when + ".",
            logged: file => "Put back an earlier version of " + file + ", the one of " + when + ".",
            notDone: file => "An earlier version of " + file + " was not put back.");
    }

    // Makes the song's file these bytes again: an earlier version of it, or the song as
    // it was when it was opened. It is a save like any other, and the file as it is now
    // is kept as a copy first, so that this can be taken back.
    private void PutBackBytes(Func<byte[]> read, bool undoing, string said, Func<string, string> logged, Func<string, string> notDone)
    {
        if (!Settle() || openSong is not { } song || Songs.FirstOrDefault(one => Paths.Same(one.Id, SongId)) is not { } listed || KeptSongOf(listed) is not { } kept)
        {
            return;
        }
        var file = Path.GetFileName(listed.Path);
        Refused = null;
        refusedForProPresenter = false;
        OffersOpeningAgain = false;
        try
        {
            byte[] bytes;
            try
            {
                bytes = read();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                throw new CopyCannotBeRead();
            }
            // Putting back what the file already is, is nothing to do (spec/07, rule
            // 21): no copy is made first, and so there is nothing to offer to undo.
            if (bytes.AsSpan().SequenceEqual(song.Bytes.Span))
            {
                Earlier = null;
                Changed();
                return;
            }
            song.PutBack(bytes);
        }
        catch (Exception exception) when (exception is Refusal or CopyCannotBeRead)
        {
            Refuse(exception as Refusal, "The copy could not be read.");
            appData.Log(notDone(file));
            if (Earlier is not null)
            {
                Earlier = Earlier with { Question = null };
            }
            Changed();
            return;
        }
        // The song starts afresh from its file, and so does its text. The newest copy
        // now is the file as it was just before: the one to put back, to take this back.
        Edit(song);
        NoteAChange();
        Earlier = null;
        if (undoing)
        {
            JustPutBack = null;
            appData.Log("Putting back was undone for " + file + ".");
        }
        else if (copies.CopiesOf(kept).FirstOrDefault() is { } before)
        {
            JustPutBack = new PutBack(said, before);
            appData.Log(logged(file));
        }
        Changed();
    }

    // A copy's own file could not be read. (Only ever thrown and caught above.)
    private sealed class CopyCannotBeRead : Exception;

    // ---- How

    private static double Within(double share) => double.IsNaN(share) ? UsualListsSplit : Math.Min(Math.Max(share, 0.1), 0.9);

    private static double NearestZoom(double wanted) =>
        double.IsNaN(wanted) ? UsualTextZoom : Zooms.MinBy(zoom => Math.Abs(zoom - wanted));

    // A number as it is written in a sentence, the same on every PC.
    private static string Number(int number) => number.ToString(CultureInfo.InvariantCulture);

    private static string Count(int chords, string key) =>
        (chords == 1 ? "1 chord" : Number(chords) + " chords") + (key.Length == 0 ? ", no key" : ", in " + key);

    private static ShownSong Show(OpenSong song, string name)
    {
        var boxes = song.TextBoxes;
        return new ShownSong(name)
        {
            Key = song.Key,
            ShownKey = song.ShownKey,
            Lines = SongText.Lines(boxes),
            Slides = song.SlideCount,
            Chords = boxes.Sum(box => box.Chords.Count),
        };
    }

    private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

    // No song is open.
    private void Close()
    {
        pauseStop?.Cancel();
        pauseStop = null;
        sayingStop?.Cancel();
        sayingStop = null;
        openSong = null;
        asOpened = null;
        ChangedSinceOpened = false;
        RevertQuestion = null;
        editor = null;
        ChordPro = null;
        Sheet = null;
        Said = null;
    }

    // The song as text, made afresh from it: a new sitting at the editor.
    private void Edit(OpenSong song)
    {
        editor = new ChordProEditor(song);
        sitting++;
        ShowText(editor);
        Sheet = MakeSheet(song);
        SheetVersion++;
    }

    // Sets the text the view shows from the editor's, in place of whatever was typed.
    private void ShowText(ChordProEditor from)
    {
        textVersion++;
        textIsStale = false;
        ChordPro = new ChordProShown(from.Text, textVersion, sitting, from.Barred);
    }

    // A song as its copies know it: by its place, its library and its name.
    private KeptSong? KeptSongOf(ListedSong song) =>
        Place is { } place && Library is { } library ? new KeptSong(place.Name, library.Name, song.Name) : null;

    // (A song's folder of copies is its name made fit to be a folder's.)
    private bool HasCopies(ListedSong song) => songsWithCopies.Contains(Copies.FolderName(song.Name));

    private void NoteCopies() =>
        songsWithCopies = Place is { } place && Library is { } library ? copies.NamesWithCopies(place.Name, library.Name) : new HashSet<string>();

    // Something was not done: its sentence, and what follows from why.
    private void Refuse(Refusal? refusal, string otherwise)
    {
        Refused = refusal?.Sentence ?? otherwise;
        OffersOpeningAgain = refusal?.Reason == Song.Refusal.Why.ChangedByAnother;
        refusedForProPresenter = refusal?.Reason == Song.Refusal.Why.ProPresenterIsRunning;
        if (refusedForProPresenter)
        {
            // (It was started with this app in front all the while: the band is there
            // from now. spec/06, rule 19.)
            ProPresenterIsRunning = true;
        }
        if (Songs.FirstOrDefault(one => Paths.Same(one.Id, SongId)) is { } listed)
        {
            appData.Log("A change to " + Path.GetFileName(listed.Path) + " was refused.");
        }
    }

    // The song's file has been changed from here, or may have been: what is shown of
    // it, its marks in the list, and whether it has copies, are brought up to date.
    // (The log says "Saved" only of a file that was: after a save that was refused
    // before anything was written, it does not.)
    private void NoteAChange(bool saved = true)
    {
        if (openSong is not { } song || Songs.FirstOrDefault(one => Paths.Same(one.Id, SongId)) is not { } listed)
        {
            return;
        }
        Shown = Show(song, listed.Name);
        ChangedSinceOpened = asOpened is null || !song.Bytes.Span.SequenceEqual(asOpened);
        if (Listing.AsItIsNow(listed) is { } now)
        {
            Songs = [.. Songs.Select(one => Paths.Same(one.Id, now.Id) ? now : one)];
            var found = Song.Marks.Read(now);
            marks[now.Id] = found;
            keptMarks.Keep(found, now);
            appData.Keep(keptMarks);
        }
        NoteCopies();
        if (saved)
        {
            appData.Log("Saved " + Path.GetFileName(listed.Path) + ".");
        }
    }

    private void FindPlaces(string? wanted, string? library)
    {
        Places = finder(chosenFolders).Places();
        PlaceId = (Places.FirstOrDefault(place => Paths.Same(place.Id, wanted)) ?? Places.FirstOrDefault())?.Id;
        ListLibraries(library);
    }

    private void ListLibraries(string? wanted)
    {
        Libraries = Place is { } place ? Listing.Libraries(place) : [];
        LibraryId = (Libraries.FirstOrDefault(library => Paths.Same(library.Id, wanted)) ?? Libraries.FirstOrDefault())?.Id;
        ListSongs();
    }

    private void ListSongs()
    {
        Songs = Library is { } library ? Listing.Songs(library) : [];
        if (!Songs.Any(song => Paths.Same(song.Id, SongId)))
        {
            SongId = null;
            Shown = null;
            Close();
            Earlier = null;
            JustPutBack = null;
            Refused = null;
            refusedForProPresenter = false;
            OffersOpeningAgain = false;
        }
        NoteCopies();
        // What was worked out before, for the files that are as they were then; the
        // rest in the background, a few at a time, so that the list is there at once.
        marks = new Dictionary<string, Marks>(Paths.ById);
        var unknown = new List<ListedSong>();
        foreach (var song in Songs)
        {
            if (keptMarks.MarksFor(song) is { } known)
            {
                marks[song.Id] = known;
            }
            else
            {
                unknown.Add(song);
            }
        }
        markingStop?.Cancel();
        markingStop = null;
        if (unknown.Count == 0)
        {
            marking = Task.CompletedTask;
            return;
        }
        markingStop = new CancellationTokenSource();
        marking = WorkOutAsync(unknown, marks, markingStop.Token);
    }

    // Reads the files a few at a time, off the window's thread, and shows what was found
    // of each few as they come. If the list has been made again meanwhile (`stop`), what
    // was found is for a list that is no longer shown, and is let go.
    private async Task WorkOutAsync(List<ListedSong> unknown, Dictionary<string, Marks> into, CancellationToken stop)
    {
        for (var start = 0; start < unknown.Count; start += AFew)
        {
            var few = unknown.GetRange(start, Math.Min(AFew, unknown.Count - start));
            var found = await Task.Run(() => few.ConvertAll(song => (Song: song, Marks: Song.Marks.Read(song))));
            if (stop.IsCancellationRequested)
            {
                return;
            }
            foreach (var (song, read) in found)
            {
                into[song.Id] = read;
                keptMarks.Keep(read, song);
            }
            Changed();
        }
        appData.Keep(keptMarks);
    }

    private void Remember() => appData.Keep(new AppData.Settings
    {
        ChosenFolders = [.. chosenFolders],
        LastPlace = PlaceId,
        LastLibrary = LibraryId,
        ListsSplit = ListsSplit == UsualListsSplit ? null : ListsSplit,
        TextZoom = TextZoom == UsualTextZoom ? null : TextZoom,
        SheetZoom = SheetZoom == UsualSheetZoom ? null : SheetZoom,
        SheetChordColour = SheetChordColour,
        TextChordColour = TextChordColour,
    });
}
