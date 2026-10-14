// What the app keeps of its own, and where. spec/06-library.md, "What the app keeps of
// its own".
//
// On a PC it is all in one folder, `Pro7ChordEditor` in the person's local application
// data (%LOCALAPPDATA%). Nothing of the app's is ever put in ProPresenter's folders, or
// beside a song.
//
//   settings.json   the folders chosen by hand, the place and library last open,
//                   where the bar between the two lists was left, the sizes of the
//                   chord sheet and of the ChordPro text, and the colours chosen for a
//                   chord in each
//   marks.json      the marks worked out for each song (Marks.cs)
//   log.txt         a line for each thing the app did
//
//   Backups\        the copies of files kept before a change (Copies.cs)
//
// The log is for working out what happened when something has gone wrong. It has the
// names of files in it and NEVER the words of songs, nor the names of chords.
//
// Losing any of these loses nothing of anyone's: the settings are chosen again, the
// marks are worked out again. So a file here that cannot be read is taken as not there,
// and a failure to write one is not an error to trouble the person with.
//
// The files are JSON, written by .NET's own writer of it. They are this app's alone:
// the Mac app keeps the same things under the same names, in its own folder and its
// own hand, and neither reads the other's.
//
// The Mac's is AppData.swift.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pro7ChordEditor.Song;

public sealed class AppData(string folder)
{
    /// <summary>When the log has grown past this many bytes it is put aside and a new one begun.</summary>
    internal const long LongLog = 1_000_000;

    private static readonly JsonSerializerOptions Written = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Folder { get; } = folder;

    /// <summary>The app's own folder on this PC. It is made when something is first kept in it.</summary>
    public static AppData OnThisPC() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pro7ChordEditor"));

    /// <summary>What the app remembers from one start to the next.</summary>
    public sealed record Settings
    {
        /// <summary>The folders chosen by hand, as paths, in the order they were chosen.</summary>
        public IReadOnlyList<string> ChosenFolders { get; init; } = [];

        /// <summary>The place and the library last open, by their ids.</summary>
        public string? LastPlace { get; init; }

        public string? LastLibrary { get; init; }

        /// <summary>
        /// How much of the height of the two lists at the window's left the libraries
        /// have, from nought to one; nothing if the bar between them was never moved.
        /// </summary>
        public double? ListsSplit { get; init; }

        /// <summary>
        /// How many times its usual size the ChordPro text is; nothing if it was never
        /// changed (spec/04-chordpro-editor.md, rule 25).
        /// </summary>
        public double? TextZoom { get; init; }

        /// <summary>
        /// How many times its usual size the chord sheet is drawn; nothing if it was
        /// never changed (spec/03-chord-sheet.md, rule 14c).
        /// </summary>
        public double? SheetZoom { get; init; }

        /// <summary>
        /// The colour of a chord's name on the sheet, and of a chord in the ChordPro
        /// text, each as "#rrggbb"; nothing where the usual one is kept
        /// (spec/06-library.md, "Settings").
        /// </summary>
        public string? SheetChordColour { get; init; }

        public string? TextChordColour { get; init; }

        /// <summary>
        /// The colour of the words of the line the person is at on the sheet, the
        /// highlighted line, the same way (spec/03, rule 53f; spec/06, rule 20a).
        /// </summary>
        public string? SheetLineColour { get; init; }

        /// <summary>
        /// Whether the chord sheet draws a chord's sharps and flats as the music
        /// signs (spec/03-chord-sheet.md, rule 10a): false where it was turned off,
        /// nothing where they are drawn, as they are as the app comes.
        /// </summary>
        public bool? ChordSigns { get; init; }

        /// <summary>
        /// How wide the two lists at the window's left are; nothing if the edge between
        /// them and the song was never moved (spec/06, rule 6a).
        /// </summary>
        public double? ListsWidth { get; init; }

        /// <summary>
        /// Whether the guide has opened by itself, as it does once: at the first start
        /// of the app on this PC (spec/13-guide.md, rule 2). Nothing if it never has.
        /// </summary>
        public bool? GuideShown { get; init; }

        /// <summary>
        /// Whether a song whose chords look as if they were put on by hand in the first
        /// Windows editor is marked, and offered to have them moved (spec/08-repair.md,
        /// check 2). Nothing, which is no, unless it was switched on: that editor's
        /// fault has not been seen in a real file yet.
        /// </summary>
        public bool? OffersToMoveBack { get; init; }

        // (A list is the same as another when it has the same things in it, which is
        // not what a record makes of one by itself.)
        public bool Equals(Settings? other) =>
            other is not null && ChosenFolders.SequenceEqual(other.ChosenFolders) && LastPlace == other.LastPlace
            && LastLibrary == other.LastLibrary && ListsSplit == other.ListsSplit && TextZoom == other.TextZoom && SheetZoom == other.SheetZoom
            && SheetChordColour == other.SheetChordColour && TextChordColour == other.TextChordColour && OffersToMoveBack == other.OffersToMoveBack
            && SheetLineColour == other.SheetLineColour && ListsWidth == other.ListsWidth && GuideShown == other.GuideShown
            && ChordSigns == other.ChordSigns;

        public override int GetHashCode() => HashCode.Combine(ChosenFolders.Count, LastPlace, LastLibrary, ListsSplit);
    }

    private T? Read<T>(string name)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllBytes(Path.Combine(Folder, name)), Written);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
            // Not there, or not to be read: taken as not there.
            return null;
        }
    }

    // Written beside itself and then put in the old one's place, so that a write cut
    // short leaves the old one. A failure is not said: nothing of anyone's is lost.
    private void Write<T>(T value, string name)
    {
        var file = Path.Combine(Folder, name);
        var beside = file + ".writing";
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(beside, JsonSerializer.SerializeToUtf8Bytes(value, Written));
            File.Move(beside, file, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            try
            {
                File.Delete(beside);
            }
            catch (Exception again) when (again is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
            }
        }
    }

    /// <summary>The settings as they were last kept, or as they come if none were.</summary>
    public Settings ReadSettings() => Read<Settings>("settings.json") ?? new Settings();

    public void Keep(Settings settings) => Write(settings, "settings.json");

    // How marks.json is written: each file's path and what was kept of it, and which
    // reading of a file made the marks (Marks.Version). A file written before that
    // number was kept has none, which reads as 0.
    private sealed record WrittenMarks(Dictionary<string, KeptMarks.Entry> Entries, int Version = 0);

    /// <summary>The copies kept, which are in <c>Backups</c> here. The app's version is for their index.</summary>
    public Copies Copies(string appVersion = "") => new(Path.Combine(Folder, "Backups"), appVersion);

    /// <summary>
    /// The marks as they were last kept, or none. Marks made by another reading of the
    /// files than this build's are none: they are worked out again.
    /// </summary>
    public KeptMarks ReadMarks() =>
        Read<WrittenMarks>("marks.json") is { Entries: { } entries, Version: Marks.Version } ? new KeptMarks(entries) : new KeptMarks();

    public void Keep(KeptMarks marks) => Write(new WrittenMarks(new Dictionary<string, KeptMarks.Entry>(marks.Entries), Marks.Version), "marks.json");

    /// <summary>
    /// Adds a line to the log, with the time before it (by the clock at Greenwich, so
    /// that a log sent from anywhere reads the same). When the log has grown past a
    /// megabyte it is put aside as <c>log-before.txt</c>, in place of the one put aside
    /// before, and a new one begun.
    /// </summary>
    public void Log(string line, DateTime? at = null)
    {
        try
        {
            var file = Path.Combine(Folder, "log.txt");
            Directory.CreateDirectory(Folder);
            if (File.Exists(file) && new FileInfo(file).Length > LongLog)
            {
                File.Move(file, Path.Combine(Folder, "log-before.txt"), overwrite: true);
            }
            var stamp = (at ?? DateTime.UtcNow).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            var oneLine = line.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Replace('\r', ' ');
            File.AppendAllText(file, stamp + "  " + oneLine + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // The log is for the person's sake, and is not worth stopping them for.
        }
    }
}
