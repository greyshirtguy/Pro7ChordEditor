// The copies kept: before the app changes a file it keeps a copy of it as it was, out
// of sight, and can put any copy back. That is what makes saving every change at once a
// safe thing to do to somebody's Sunday library. spec/07-backups.md.
//
// NEW: Simple Presenter keeps a copy the first time its editor changes a file and has no
// way to look at them or put one back. This is that idea finished. The Mac's is
// Copies.swift, and this is ported from it.
//
// Where. In the app's own folder (AppData), under `Backups`, in folders that follow
// where the file was: the place, then the library, then the file's name without its
// ending. So the copies of `Songs\Amazing Grace.pro` in the place "Documents" are in
// `Backups\Documents\Songs\Amazing Grace\`. A song is known by where it is: renamed or
// moved in ProPresenter it starts afresh, and its earlier copies stay under its old
// name, where All Copies finds them.
//
// What. A copy is the file's bytes, unchanged. It is named for when it was made, to the
// second, in the computer's own time: `2026-10-11 09.14.03.pro`. Beside the copies is
// `index.json`, with an entry for each: why it was made, the whole path of the file it
// is a copy of, the song's id and name as they were inside it, how many chords it had,
// its key, its size, and the app's version.
//
// The index is a help, not the truth. If it is lost or damaged the copies are still
// there, named by their time, and are listed from the folder; what the index would have
// said of one is then read from the copy itself, or not known.
//
// What is thrown away, and how carefully. Only `Thin` deletes anything, only inside one
// song's folder of copies, and only files that are named as copies are. The oldest copy
// of a file, the file as it was before this app ever touched it, is NEVER thrown away.
// (And `Keep` clears away the one file it has just made itself, if that did not come
// out whole.)

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <summary>
/// A song that has copies kept, known by where it is: its place, its library and its
/// file's name without the ending.
/// </summary>
public sealed record KeptSong(string Place, string Library, string Name)
{
    public string Id => Place + "/" + Library + "/" + Name;
}

/// <summary>One copy of a song's file.</summary>
/// <param name="Path">Where the copy is.</param>
/// <param name="Made">When it was made, by the clock at Greenwich, to the second.</param>
/// <param name="Why">Why. Not known if the index is lost.</param>
/// <param name="CopyOf">The whole path of the file it is a copy of. Empty if the index is lost.</param>
/// <param name="SongId">The song's id as it was inside it.</param>
/// <param name="SongName">The song's name as it was inside it.</param>
/// <param name="Chords">How many chords it had.</param>
/// <param name="Key">Its key; "" if it stated none.</param>
public sealed record KeptCopy(string Path, DateTime Made, CopyReason? Why, string CopyOf, string SongId, string SongName, int Chords, string Key, long Size)
{
    /// <summary>Whether it is the oldest copy there is of its file: the one never thrown away.</summary>
    public bool IsTheFirst { get; init; }

    /// <summary>A copy is known, among its song's, by its file's name.</summary>
    public string Id => System.IO.Path.GetFileName(Path);
}

public sealed class Copies
{
    private const string Ending = ".pro";
    private const string NameFormat = "yyyy-MM-dd HH.mm.ss";
    private const string IndexName = "index.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <param name="folder">The <c>Backups</c> folder.</param>
    /// <param name="appVersion">The app's version, for the index.</param>
    /// <param name="timeZone">The computer's own time zone. A test may give another.</param>
    public Copies(string folder, string appVersion = "", TimeZoneInfo? timeZone = null)
    {
        Folder = folder;
        AppVersion = appVersion;
        TimeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public string Folder { get; }

    public string AppVersion { get; }

    public TimeZoneInfo TimeZone { get; }

    /// <summary>A copy could not be made: why, in words that can end a sentence.</summary>
    public sealed class CannotBeKept(string why) : Exception(why);

    /// <summary>The three reasons, as the index writes them (spec/07, rule 8).</summary>
    internal static string Said(CopyReason why) => why switch
    {
        CopyReason.BeforePuttingBack => "before putting back a copy",
        CopyReason.BeforeMending => "before mending",
        _ => "before a change",
    };

    private static CopyReason? Read(string? why) => why switch
    {
        "before a change" => CopyReason.BeforeAChange,
        "before putting back a copy" => CopyReason.BeforePuttingBack,
        "before mending" => CopyReason.BeforeMending,
        _ => null,
    };

    // What the index says of one copy.
    internal sealed record Entry
    {
        public string File { get; init; } = "";

        public DateTime Made { get; init; }

        public string Why { get; init; } = "";

        public string CopyOf { get; init; } = "";

        [JsonPropertyName("songID")]
        public string SongId { get; init; } = "";

        public string SongName { get; init; } = "";

        public int Chords { get; init; }

        public string Key { get; init; } = "";

        public long Size { get; init; }

        public string AppVersion { get; init; } = "";
    }

    // ---- Where

    /// <summary>
    /// A name made fit to be one folder's name: a slash either way, a colon and a line
    /// end in it become a hyphen (spec/07, rule 7). And, Windows' own: so do the other
    /// marks Windows will not have in a name, and a full stop or a space at its end,
    /// which Windows would drop and so take two names for one.
    /// </summary>
    public static string FolderName(string name)
    {
        var fit = new char[name.Length];
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            fit[i] = c is '/' or ':' or (char)0x5C or '<' or '>' or '"' or '|' or '?' or '*' || c < ' ' || c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029 ? '-' : c;
        }
        for (var i = fit.Length - 1; i >= 0 && fit[i] is '.' or ' '; i--)
        {
            fit[i] = '-';
        }
        return fit.Length == 0 ? "-" : new string(fit);
    }

    /// <summary>The folder a song's copies are kept in.</summary>
    public string FolderOf(KeptSong song) =>
        System.IO.Path.Combine(Folder, FolderName(song.Place), FolderName(song.Library), FolderName(song.Name));

    /// <summary>
    /// When a copy of this name was made, if it is named as a copy is:
    /// "2026-10-11 09.14.03.pro", or with " 2" before the ending for a second copy made
    /// in the same second. Null for any other name.
    /// </summary>
    internal DateTime? TimeOfCopyNamed(string name) => TimeAndTurn(name)?.Time;

    // The same, and which copy of its second it is: 1 for the first, 2 for " 2".
    private (DateTime Time, int Turn)? TimeAndTurn(string name)
    {
        if (!name.EndsWith(Ending, StringComparison.Ordinal))
        {
            return null;
        }
        var stem = name[..^Ending.Length];
        var turn = 1;
        var space = stem.LastIndexOf(' ');
        if (space == NameFormat.Length && int.TryParse(stem[(space + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var again) && again >= 2)
        {
            stem = stem[..space];
            turn = again;
        }
        if (stem.Length != NameFormat.Length
            || !DateTime.TryParseExact(stem, NameFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return null;
        }
        // (An hour that the clocks skip in spring has no copies made in it. A name that
        // says one is taken as the hour after, and not as no copy at all.)
        if (TimeZone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }
        return (TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeZone), turn);
    }

    private string Named(DateTime made) =>
        TimeZoneInfo.ConvertTimeFromUtc(made, TimeZone).ToString(NameFormat, CultureInfo.InvariantCulture);

    // ---- Keeping

    /// <summary>
    /// Keeps a copy of a file as it is on disk now. Answers with the copy, or with
    /// nothing if the newest copy kept of that file is already byte for byte the same,
    /// in which case no second one is made.
    /// </summary>
    /// <exception cref="CannotBeKept">The copy cannot be made: the change that wanted it is then refused.</exception>
    public KeptCopy? Keep(string file, KeptSong song, CopyReason why, DateTime? at = null)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(file);
        }
        catch (Exception exception) when (Stops(exception))
        {
            throw new CannotBeKept(exception.Message);
        }
        var home = FolderOf(song);
        if (CopiesOf(song).FirstOrDefault() is { } newest && Same(newest.Path, bytes))
        {
            return null;
        }
        // To the second, as the copy's name is: so that a copy says the same of when it
        // was made whether or not the index is there to say it.
        var asked = (at ?? DateTime.UtcNow).ToUniversalTime();
        var made = new DateTime(asked.Ticks - (asked.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        string name;
        try
        {
            Directory.CreateDirectory(home);
            // Named for the second it was made. Another of the same second, which can
            // only be other bytes, has " 2" after the time, and so on.
            var stem = Named(made);
            name = stem + Ending;
            for (var again = 2; File.Exists(System.IO.Path.Combine(home, name)); again++)
            {
                name = stem + " " + again.ToString(CultureInfo.InvariantCulture) + Ending;
            }
            var path = System.IO.Path.Combine(home, name);
            // The file's bytes, unchanged: not read as a song and written again. Never
            // over a copy that is there (the file is made new, or not at all).
            using (var copy = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                copy.Write(bytes);
                copy.Flush(flushToDisk: true);
            }
            // One that did not come out whole is no copy, and is cleared away: it is the
            // file made on the lines before, and no other.
            if (!Same(path, bytes))
            {
                File.Delete(path);
                throw new CannotBeKept("the copy did not read back as it was written.");
            }
        }
        catch (Exception exception) when (Stops(exception))
        {
            throw new CannotBeKept(exception.Message);
        }
        var entry = Describe(bytes, new Entry
        {
            File = name,
            Made = made,
            Why = Said(why),
            CopyOf = Paths.IdOf(file),
            Size = bytes.Length,
            AppVersion = AppVersion,
        });
        // (The index is a help. If it cannot be written, the copy is still kept.)
        WriteIndex([.. Index(home), entry], home);
        return CopiesOf(song).FirstOrDefault(copy => copy.Id == name);
    }

    private static bool Stops(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Security.SecurityException;

    // Whether a file is these bytes. One that cannot be read is not.
    private static bool Same(string path, byte[] bytes)
    {
        try
        {
            return File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes);
        }
        catch (Exception exception) when (Stops(exception))
        {
            return false;
        }
    }

    // What can be said of a song from its file's bytes, for the index. Of bytes that are
    // no song, nothing.
    private static Entry Describe(byte[] bytes, Entry entry)
    {
        RV.Presentation presentation;
        try
        {
            presentation = RV.Presentation.Parser.ParseFrom(bytes);
        }
        catch (InvalidProtocolBufferException)
        {
            return entry;
        }
        var chords = 0;
        foreach (var row in Reading.SlideRows(presentation))
        {
            foreach (var element in Reading.ElementsOf(row, presentation))
            {
                if (Reading.ShowsItsOwnWords(element))
                {
                    chords += Reading.ChordsOf(element.Element_?.Text).Count;
                }
            }
        }
        return entry with
        {
            SongId = presentation.Uuid?.String ?? "",
            SongName = presentation.Name,
            Key = presentation.Music is null ? "" : Reading.KeyName(presentation.Music.Original),
            Chords = chords,
        };
    }

    private static List<Entry> Index(string home)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Entry>>(File.ReadAllBytes(System.IO.Path.Combine(home, IndexName)), Json) ?? [];
        }
        catch (Exception exception) when (Stops(exception) || exception is JsonException)
        {
            return [];
        }
    }

    // Written beside itself and then put in the old one's place. A failure is not said:
    // the copies are the truth, and they are there.
    private static void WriteIndex(List<Entry> entries, string home)
    {
        var file = System.IO.Path.Combine(home, IndexName);
        var beside = file + ".writing";
        try
        {
            File.WriteAllBytes(beside, JsonSerializer.SerializeToUtf8Bytes(entries, Json));
            File.Move(beside, file, overwrite: true);
        }
        catch (Exception exception) when (Stops(exception))
        {
            try
            {
                File.Delete(beside);
            }
            catch (Exception again) when (Stops(again))
            {
            }
        }
    }

    // ---- Listing

    /// <summary>
    /// The copies kept of a song, the newest first. Listed from the folder: every file
    /// in it that is named as a copy is one, whether or not the index knows of it.
    /// </summary>
    public IReadOnlyList<KeptCopy> CopiesOf(KeptSong song)
    {
        var home = FolderOf(song);
        List<string> names;
        try
        {
            names = [.. Directory.EnumerateFiles(home).Select(path => System.IO.Path.GetFileName(path))];
        }
        catch (Exception exception) when (Stops(exception))
        {
            return [];
        }
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var entry in Index(home))
        {
            entries[entry.File] = entry;
        }
        var copies = new List<(KeptCopy Copy, int Turn)>();
        foreach (var name in names)
        {
            if (TimeAndTurn(name) is not (DateTime made, int turn))
            {
                continue;
            }
            var path = System.IO.Path.Combine(home, name);
            if (entries.TryGetValue(name, out var known))
            {
                copies.Add((new KeptCopy(path, known.Made.ToUniversalTime(), Read(known.Why), known.CopyOf, known.SongId, known.SongName, known.Chords, known.Key, known.Size), turn));
                continue;
            }
            // Not in the index: what can be read from the copy itself, and no more.
            var read = new Entry { File = name, Made = made };
            try
            {
                var bytes = File.ReadAllBytes(path);
                read = Describe(bytes, read with { Size = bytes.Length });
            }
            catch (Exception exception) when (Stops(exception))
            {
            }
            copies.Add((new KeptCopy(path, made, null, "", read.SongId, read.SongName, read.Chords, read.Key, read.Size), turn));
        }
        // The newest first; of those made in one second, the later named first.
        var sorted = copies.OrderByDescending(one => one.Copy.Made).ThenByDescending(one => one.Turn).Select(one => one.Copy).ToList();
        if (sorted.Count > 0)
        {
            sorted[^1] = sorted[^1] with { IsTheFirst = true };
        }
        return sorted;
    }

    // The folders in a folder, not hidden, by name as the system sorts names. None, if
    // it is not there.
    private static List<string> Folders(string folder)
    {
        try
        {
            return Listing.Sorted(
                new DirectoryInfo(folder).EnumerateDirectories().Where(one => !one.Attributes.HasFlag(FileAttributes.Hidden) && !one.Name.StartsWith('.')).Select(one => one.Name),
                name => name);
        }
        catch (Exception exception) when (Stops(exception))
        {
            return [];
        }
    }

    /// <summary>
    /// Every song that has copies, by place and library: those whose song is no longer
    /// where it was included.
    /// </summary>
    public IReadOnlyList<KeptSong> Songs()
    {
        var found = new List<KeptSong>();
        foreach (var place in Folders(Folder))
        {
            foreach (var library in Folders(System.IO.Path.Combine(Folder, place)))
            {
                foreach (var song in Folders(System.IO.Path.Combine(Folder, place, library)))
                {
                    var kept = new KeptSong(place, library, song);
                    if (CopiesOf(kept).Count > 0)
                    {
                        found.Add(kept);
                    }
                }
            }
        }
        return found;
    }

    /// <summary>The names of the songs of one library that have copies: for the mark in the list.</summary>
    public IReadOnlySet<string> NamesWithCopies(string place, string library)
    {
        var home = System.IO.Path.Combine(Folder, FolderName(place), FolderName(library));
        return Folders(home).Where(name => CopiesOf(new KeptSong(place, library, name)).Count > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>How much room all the copies take, in bytes.</summary>
    public long Size => Songs().Sum(song => CopiesOf(song).Sum(copy => copy.Size));

    // ---- How long

    /// <summary>How many of a song's newest copies are kept whatever their age.</summary>
    public const int NewestKept = 20;

    /// <summary>How many days' copies are all kept.</summary>
    public const int DaysKept = 30;

    /// <summary>
    /// Throws away the copies that are kept no longer, and answers with how many went.
    /// Of each song's copies: the oldest is never thrown away; everything from the last
    /// thirty days is kept; and the twenty newest, whatever their age. The rest go. Done
    /// when the app starts.
    /// </summary>
    public int Thin(DateTime? asOf = null)
    {
        var recent = (asOf ?? DateTime.UtcNow).ToUniversalTime().AddDays(-DaysKept);
        var thrownAway = 0;
        foreach (var song in Songs())
        {
            var all = CopiesOf(song);
            var going = all.Where((copy, n) => !copy.IsTheFirst && n >= NewestKept && copy.Made < recent).ToList();
            if (going.Count == 0)
            {
                continue;
            }
            var home = FolderOf(song);
            foreach (var copy in going)
            {
                // Only a file in this song's folder of copies, and named as a copy is.
                if (!Paths.Same(Paths.IdOf(System.IO.Path.GetDirectoryName(copy.Path) ?? ""), Paths.IdOf(home)) || TimeOfCopyNamed(copy.Id) is null)
                {
                    continue;
                }
                try
                {
                    File.Delete(copy.Path);
                    thrownAway++;
                }
                catch (Exception exception) when (Stops(exception))
                {
                }
            }
            var left = CopiesOf(song).Select(copy => copy.Id).ToHashSet(StringComparer.Ordinal);
            WriteIndex([.. Index(home).Where(entry => left.Contains(entry.File))], home);
        }
        return thrownAway;
    }

    // ---- Saying when

    /// <summary>
    /// When a copy was made, as the list of earlier versions says it: "Today 09:14",
    /// "Sunday 4 Oct" for the six days before, "12 Sep" earlier in the year, and
    /// "2 Aug 2025" before that. The names of days and months are the person's own
    /// language's, unless another is given.
    /// </summary>
    public string When(DateTime made, DateTime? asOf = null, CultureInfo? culture = null)
    {
        var language = culture ?? CultureInfo.CurrentCulture;
        var then = TimeZoneInfo.ConvertTimeFromUtc(made.ToUniversalTime(), TimeZone);
        var now = TimeZoneInfo.ConvertTimeFromUtc((asOf ?? DateTime.UtcNow).ToUniversalTime(), TimeZone);
        var days = (now.Date - then.Date).Days;
        if (days == 0)
        {
            return "Today " + then.ToString("HH':'mm", language);
        }
        if (days > 0 && days < 7)
        {
            return then.ToString("dddd d MMM", language);
        }
        return then.ToString(then.Year == now.Year ? "d MMM" : "d MMM yyyy", language);
    }
}
