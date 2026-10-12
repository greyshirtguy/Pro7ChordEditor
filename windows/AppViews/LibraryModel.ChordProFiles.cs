// The window's model, the part of it that is about ChordPro files: a new song made from
// one, and the song that is open written as one. spec/09-chordpro-files.md; the rules of
// it are in the layer below (Song: ChordProFile.cs and NewSong.cs).
//
// NEW to both apps: the reference has an import with no summary of this kind, and the
// Mac app has nothing of it yet. The page says WHAT is shown before a song is made
// (rule 8) and gives no sentence; every sentence here is this app's own, and is listed
// in NOTES.md for the maintainer to read and for the Mac to have the same.
//
// How it goes:
//
//   1. "New Song from ChordPro File..." asks the system for a file. The file is read,
//      and nothing is made: a panel over the window says what was found in it (its
//      title, its artist, its key, each part with how many lines it has, and how many
//      chords in all), and with that the name the song will have, the library it goes
//      into, which is the one that is open, how many lines to a slide, which is two
//      unless changed, and the song it is to be made to look like.
//   2. A file that is no song (it cannot be read, is too large, has no words in it) says
//      so in the same panel, and there is nothing to do there but close it.
//   3. "Make it look like": the song that is open, if one is; else the song the app
//      carries. Any song of the open library can be chosen. (The page has the person
//      pick, "failing that" the app's own. That the song that is open is offered first
//      is this session's: it is the one the person is looking at.)
//   4. "Make the Song" makes it (NewSong.Make), lists it, opens it, and says what was
//      made. What was refused is said in the panel, which stays, so that another name
//      or another song to look like can be tried.
//   5. Nothing is made while ProPresenter is running, and the panel says so beforehand.
//
// And "Save Song as ChordPro File...": the song that is open, with what was being
// typed in it saved first, written where the person says. It changes nothing in the
// library.

using System.Globalization;
using System.IO;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed partial class LibraryModel
{
    /// <summary>A song the new one can be made to look like: its id among the list's, its name, and its file. With none of the first and last, it is the one the app carries.</summary>
    public sealed record LookAlike(string? Id, string Name, string? Path = null);

    /// <summary>
    /// What is over the window before a song is made from a ChordPro file: what was found
    /// in the file, and what the person may change (spec/09, rule 8). With nothing found,
    /// the file was no song, and <see cref="Problem"/> says why.
    /// </summary>
    /// <param name="File">The file's own name, with its ending.</param>
    /// <param name="Name">The song's name, as it stands in the field.</param>
    /// <param name="WillBeNamed">The name the song will have: that name made fit to be a file's, with a number if the library has it already.</param>
    /// <param name="Library">The name of the library it goes into.</param>
    /// <param name="LookAlikes">The songs it can be made to look like: the app's own first, then the library's.</param>
    /// <param name="Like">Which of those is chosen.</param>
    /// <param name="Problem">Why the song was not made, when it was asked for; or why the file is no song.</param>
    /// <param name="ProPresenterIsRunning">Whether ProPresenter is running, as far as is known: then nothing can be made.</param>
    public sealed record NewSongAsked(
        string File,
        FoundInAFile? Found,
        string Name,
        string WillBeNamed,
        string Library,
        int LinesToASlide,
        IReadOnlyList<LookAlike> LookAlikes,
        LookAlike Like,
        string? Problem = null,
        bool ProPresenterIsRunning = false)
    {
        public string Heading => "New song from " + File;

        /// <summary>What was found, as a name and what is said of it, line by line.</summary>
        public IReadOnlyList<(string What, string Says)> Summary => Found is null
            ? []
            :
            [
                ("Title", Found.Title.Length > 0 ? Found.Title : NoneGiven),
                ("Artist", Found.Artist.Length > 0 ? Found.Artist : NoneGiven),
                ("Key", Found.Key.Length > 0 ? Found.Key : NoneGiven),
                ("Parts", string.Join(", ", Found.Parts.Select(part => (part.Name.Length > 0 ? part.Name : "a part with no name") + " (" + Counted(part.Lines, "line") + ")"))),
                ("Chords", Found.Chords == 0 ? "None" : Counted(Found.Chords, "chord") + " in all"),
            ];

        /// <summary>How many slides the lines make, said after the number of lines to a slide.</summary>
        public string Slides => Found is null ? "" : "which makes " + Counted(Found.Slides(LinesToASlide), "slide");

        /// <summary>
        /// What is said under the name where the song will not be named exactly as the
        /// field has it: because a file cannot be, or because the library has a song of
        /// that name. Nothing, where it will be.
        /// </summary>
        public string NameNote
        {
            get
            {
                if (Found is null)
                {
                    return "";
                }
                var tidied = Song.NewSong.Tidied(Name);
                if (!string.Equals(WillBeNamed, tidied, StringComparison.Ordinal))
                {
                    return Quoted(Library) + " has a song named " + Quoted(tidied) + " already. This one will be named " + Quoted(WillBeNamed) + ".";
                }
                return string.Equals(tidied, Name.Trim(), StringComparison.Ordinal) ? "" : "As a file can be named, that is " + Quoted(WillBeNamed) + ".";
            }
        }

        /// <summary>What is said in place of a refusal while ProPresenter is running.</summary>
        public string? Standing => Problem ?? (ProPresenterIsRunning && Found is not null ? "ProPresenter is running. Quit it to make a song here." : null);

        /// <summary>Whether "Make the Song" can be pressed.</summary>
        public bool CanMake => Found is not null && !ProPresenterIsRunning;

        internal const string NoneGiven = "None given";
    }

    // The file a song is being made from, as it was read.
    private (string Path, ParsedSong Song)? fileBeingBroughtIn;

    /// <summary>The panel of a new song, while it is over the window.</summary>
    public NewSongAsked? NewSong { get; private set; }

    // Whether something is over the window, asking or showing. Nothing else is begun
    // then: the menu is under it, but Alt and a letter still reach a menu.
    private bool SomethingIsOverTheWindow =>
        Asked is not null || MendAllQuestion is not null || MendingAll is not null || Earlier is not null || NewSong is not null;

    /// <summary>Whether a song can be made now: there is a library for it to go into, and nothing is over the window.</summary>
    public bool CanBringInAFile => started && Library is not null && !SomethingIsOverTheWindow;

    /// <summary>Whether the song that is open can be written as a ChordPro file: there is one, it can be read, and nothing is over the window.</summary>
    public bool CanWriteChordPro => openSong is not null && Shown is { Problem: null } && !SomethingIsOverTheWindow;

    /// <summary>The name a ChordPro file of the song that is open is offered: the song's, with the ending.</summary>
    public string NameForAChordProFile => Song.NewSong.Tidied(Shown?.Name ?? "") + ChordProFile.Ending;

    /// <summary>The app's own song, as it is named among those a new song can be made to look like.</summary>
    public const string TheAppsOwn = "A song this app carries";

    /// <summary>
    /// A ChordPro file was chosen: it is read, and what was found is shown. Nothing is
    /// made, and nothing in the library is changed.
    /// </summary>
    public void BringIn(string file)
    {
        if (!CanBringInAFile || Library is not { } library)
        {
            return;
        }
        var name = Path.GetFileName(file);
        LookAlike[] lookAlikes = [new(null, TheAppsOwn), .. Songs.Select(song => new LookAlike(song.Id, song.Name, song.Path))];
        try
        {
            var song = ChordProFile.Read(file);
            fileBeingBroughtIn = (file, song);
            var named = Song.NewSong.NameFor(song, file);
            // (The song that is open is the one to look like, if there is one that can
            // be read; else the app's own.)
            var like = (openSong is not null && Shown is { Problem: null } ? lookAlikes.FirstOrDefault(one => Paths.Same(one.Id, SongId)) : null) ?? lookAlikes[0];
            NewSong = new NewSongAsked(
                name,
                Song.NewSong.Found(song),
                named,
                Song.NewSong.FreeName(named, library.Folder),
                library.Name,
                Song.NewSong.UsualLinesToASlide,
                lookAlikes,
                like,
                ProPresenterIsRunning: ProPresenterIsRunning);
            appData.Log("Read " + name + ".");
        }
        catch (Refusal refusal)
        {
            fileBeingBroughtIn = null;
            NewSong = new NewSongAsked(name, null, "", "", library.Name, Song.NewSong.UsualLinesToASlide, lookAlikes, lookAlikes[0], refusal.Sentence);
            appData.Log("Could not read " + name + ".");
        }
        Changed();
    }

    /// <summary>The name was typed in.</summary>
    public void NameTheNewSong(string name)
    {
        if (NewSong is not { Found: not null } asked || Library is not { } library || string.Equals(name, asked.Name, StringComparison.Ordinal))
        {
            return;
        }
        NewSong = asked with { Name = name, WillBeNamed = Song.NewSong.FreeName(name, library.Folder), Problem = null };
        Changed();
    }

    /// <summary>How many lines go on a slide: one at least, and no more than the window lets be asked for.</summary>
    public void SetLinesToASlide(int lines)
    {
        var each = Math.Clamp(lines, 1, Song.NewSong.MostLinesToASlide);
        if (NewSong is not { Found: not null } asked || each == asked.LinesToASlide)
        {
            return;
        }
        NewSong = asked with { LinesToASlide = each, Problem = null };
        Changed();
    }

    /// <summary>The song it is to look like was chosen: one of the panel's, by its id, or the app's own by none.</summary>
    public void MakeItLookLike(string? id)
    {
        if (NewSong is not { Found: not null } asked || asked.LookAlikes.FirstOrDefault(one => Paths.Same(one.Id, id)) is not { } like || like == asked.Like)
        {
            return;
        }
        NewSong = asked with { Like = like, Problem = null };
        Changed();
    }

    /// <summary>Cancel, or Close: the panel is put away, and nothing was made.</summary>
    public void DontMakeTheSong()
    {
        if (NewSong is null)
        {
            return;
        }
        NewSong = null;
        fileBeingBroughtIn = null;
        Changed();
    }

    /// <summary>
    /// "Make the Song": it is made, as a new file in the library that is open; then it is
    /// listed and opened, and what was made is said. What is refused is said in the
    /// panel, which stays.
    /// </summary>
    public void MakeTheSong()
    {
        if (NewSong is not { Found: not null } asked || fileBeingBroughtIn is not { } file || Library is not { } library)
        {
            return;
        }
        MadeSong made;
        try
        {
            // (A song of the library is read from its file as it is now. If it has
            // been taken away since the panel came, that is what is said.)
            var like = asked.Like.Path is null ? ModelSong.OfTheApp : ModelSong.Read(asked.Like.Path);
            made = Song.NewSong.Make(file.Song, asked.Name, asked.LinesToASlide, like, library.Folder, askWhetherProPresenterIsRunning);
        }
        catch (Refusal refusal)
        {
            var running = refusal.Reason == Refusal.Why.ProPresenterIsRunning;
            if (running)
            {
                // (It was started with this app in front all the while: the band is
                // there from now, and the panel says it without being asked again.)
                ProPresenterIs(true);
            }
            NewSong = asked with { Problem = running ? null : refusal.Sentence, ProPresenterIsRunning = ProPresenterIsRunning };
            appData.Log("A song was not made from " + asked.File + ".");
            Changed();
            return;
        }
        NewSong = null;
        fileBeingBroughtIn = null;
        appData.Log("Made " + Path.GetFileName(made.Path) + " from " + asked.File + ".");
        // It is listed, and is the song that is open: unless what was being typed in the
        // song that was open cannot be saved, and then that song is not left.
        ListSongs();
        Open(Paths.IdOf(made.Path));
        Say(Quoted(made.Name) + " was made from " + asked.File + ": " + Counted(made.Parts, "part") + ", " + Counted(made.Slides, "slide") + ", "
            + (made.Chords == 0 ? "no chords" : Counted(made.Chords, "chord")) + ".");
    }

    /// <summary>
    /// "Save Song as ChordPro File...": the song that is open, written where the person
    /// said. What was being typed in it is saved first, so that the file has it.
    /// </summary>
    public void WriteAsChordPro(string path)
    {
        if (!CanWriteChordPro || !Settle() || openSong is not { } song)
        {
            return;
        }
        var name = Path.GetFileName(path);
        try
        {
            ChordProFile.Write(path, ChordProFile.TextOf(song.Name, song.Key, song.TextBoxes));
        }
        catch (Refusal refusal)
        {
            Refused = refusal.Sentence;
            OffersOpeningAgain = false;
            refusedForProPresenter = false;
            appData.Log("Could not write " + name + ".");
            Changed();
            return;
        }
        appData.Log("Wrote " + name + ".");
        Say(Quoted(song.Name) + " was written as " + name + ".");
    }

    // "1 line", "4 lines".
    private static string Counted(int count, string what) =>
        count == 1 ? "1 " + what : count.ToString(CultureInfo.InvariantCulture) + " " + what + "s";
}
