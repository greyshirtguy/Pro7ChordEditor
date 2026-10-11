// What the window shows, and what a click in it does, with no drawing in it: the places
// found, the libraries of the one chosen, the songs of the library chosen, the song that
// is open. spec/06-library.md, "The window".
//
// The views only show what is here and pass on what the person does. Everything that is
// a rule is in the layers below (Song: Places, Listing, Marks, SongText), which this
// calls; so that the rules are proved by `dotnet test` with no window, and are the same
// in the Mac app. The words the person reads in the lists are here too, and not in the
// views, so that a test can hold them.
//
// So far (milestone M3) a song is opened only to be looked at: it is handed no
// safeguards, and so cannot be changed (Song/Safeguards.cs). The copies of M4 and the
// editors of M5 and M6 are to come.
//
// It is handed what it would otherwise ask the PC for (where the app keeps its own
// things, how to find places, whether ProPresenter is running), so that tests can hand
// it made-up ones. `OnThisPC` hands it the real ones.
//
// ONE THREAD. Everything here is to be asked from one thread, the window's. A song's
// marks are worked out on another, since a big library takes a while, and come back to
// the window's thread to be shown: which they do by themselves where that thread has a
// queue of its own, as a window's has. (The tests give theirs one: OnTheWindowsThread.)
//
// The Mac's is LibraryModel.swift.

using System.ComponentModel;
using System.Globalization;
using System.IO;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed class LibraryModel : INotifyPropertyChanged
{
    /// <summary>How much of the height of the two lists the libraries have, unless the bar between them was moved.</summary>
    public const double UsualListsSplit = 0.36;

    // How many songs' marks are worked out at a time before they are shown.
    private const int AFew = 20;

    private readonly AppData appData;
    private readonly Func<IReadOnlyList<string>, PlaceFinder> finder;
    private readonly Func<bool> askWhetherProPresenterIsRunning;
    private bool started;
    private KeptMarks keptMarks = new();
    private Task marking = Task.CompletedTask;
    private CancellationTokenSource? markingStop;
    private List<string> chosenFolders = [];
    private Dictionary<string, Marks> marks = new(Paths.ById);
    private OpenSong? openSong;
    private string typed = "";
    private MarkFilter filter = MarkFilter.All;

    public LibraryModel(AppData appData, Func<IReadOnlyList<string>, PlaceFinder> finder, Func<bool> proPresenterIsRunning)
    {
        this.appData = appData;
        this.finder = finder;
        askWhetherProPresenterIsRunning = proPresenterIsRunning;
    }

    public static LibraryModel OnThisPC() => new(AppData.OnThisPC(), chosen => PlaceFinder.OnThisPC(chosen), ProPresenterApp.IsRunning);

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
    public sealed record SongRow(string Id, string Name, Marks? Marks)
    {
        public bool CannotBeRead => Marks?.CannotBeRead == true;

        public bool IsOldEditorsCopy => Marks?.IsOldEditorsCopy == true;

        public bool HasChords => Marks?.HasChords == true;

        public static string CannotBeReadTip => "This file could not be read as a ProPresenter presentation.";

        // (The copies' ending is set between the quotation marks that curl, with the
        // three dots that stand for the song's name before it.)
        public static string OldEditorsCopyTip =>
            "A copy saved by the first Windows chord editor, which named its copies " + (char)0x201C + (char)0x2026 + Marks.OldEditorsEnding + (char)0x201D + ".";

        public string ChordsTip => Marks is null ? "" : Marks.Chords == 1 ? "Has 1 chord." : "Has " + Number(Marks.Chords) + " chords.";
    }

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

    public bool ProPresenterIsRunning { get; private set; }

    /// <summary>
    /// How much of the height of the two lists at the window's left the libraries have,
    /// the songs having the rest: what the bar between them is dragged to.
    /// </summary>
    public double ListsSplit { get; private set; } = UsualListsSplit;

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
    public IReadOnlyList<SongRow> Rows => [.. ListedSongs.Select(song => new SongRow(song.Id, song.Name, marks.GetValueOrDefault(song.Id)))];

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
    /// The quiet line under the song: what is there to be seen. Nothing here is a
    /// promise of what the app will do.
    /// </summary>
    public string Notice
    {
        get
        {
            if (Shown is not null)
            {
                if (Shown.Problem is not null)
                {
                    return "Nothing was done to this file.";
                }
                var slides = Shown.Slides == 1 ? "1 slide" : Number(Shown.Slides) + " slides";
                var chords = Shown.Chords == 1 ? "1 chord" : Number(Shown.Chords) + " chords";
                return slides + ", " + chords + ". Nothing can be changed here yet.";
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
    /// When the window first shows: what the app kept is read, the places are found, and
    /// the place and library last open are opened again. Asked a second time it does
    /// nothing.
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
        keptMarks = appData.ReadMarks();
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
        var song = SongId;
        FindPlaces(PlaceId, LibraryId);
        ProPresenterIsRunning = askWhetherProPresenterIsRunning();
        // The song that is open stays open if its file is still the one that was read.
        // If something else has changed the file meanwhile, it is read again.
        if (song is not null && Songs.Any(one => Paths.Same(one.Id, song)) && openSong?.FileIsAsLastRead != true)
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
    public void ProPresenterMayHaveChanged() => ProPresenterIs(askWhetherProPresenterIsRunning());

    /// <summary>The same, with the asking done off the window's thread: the system's list of what is running takes a moment to read.</summary>
    public async Task LookForProPresenterAsync() => ProPresenterIs(await Task.Run(askWhetherProPresenterIsRunning));

    private void ProPresenterIs(bool running)
    {
        if (running != ProPresenterIsRunning)
        {
            ProPresenterIsRunning = running;
            appData.Log(running ? "ProPresenter is running." : "ProPresenter is no longer running.");
            Changed();
        }
    }

    public void ChoosePlace(string? id)
    {
        if (Paths.Same(id, PlaceId))
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
        if (Paths.Same(id, LibraryId))
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
    /// to it.
    /// </summary>
    public void Open(string? id)
    {
        SongId = id;
        openSong = null;
        var song = Songs.FirstOrDefault(one => Paths.Same(one.Id, id));
        if (song is null)
        {
            SongId = null;
            Shown = null;
            Changed();
            return;
        }
        var file = Path.GetFileName(song.Path);
        try
        {
            var opened = new OpenSong(song.Path);
            var boxes = opened.TextBoxes;
            openSong = opened;
            Shown = new ShownSong(song.Name)
            {
                Key = opened.Key,
                ShownKey = opened.ShownKey,
                Lines = SongText.Lines(boxes),
                Slides = opened.SlideCount,
                Chords = boxes.Sum(box => box.Chords.Count),
            };
            appData.Log("Opened " + file + " to be looked at.");
        }
        catch (Refusal refusal)
        {
            Shown = new ShownSong(song.Name) { Problem = refusal.Sentence };
            appData.Log("Could not read " + file + ".");
        }
        Changed();
    }

    /// <summary>A folder chosen by hand becomes a place, is remembered, and is opened.</summary>
    public void Add(string folder)
    {
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

    // ---- How

    private static double Within(double share) => double.IsNaN(share) ? UsualListsSplit : Math.Min(Math.Max(share, 0.1), 0.9);

    // A number as it is written in a sentence, the same on every PC.
    private static string Number(int number) => number.ToString(CultureInfo.InvariantCulture);

    private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

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
            openSong = null;
        }
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
    });
}
