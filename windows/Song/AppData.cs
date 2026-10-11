// What the app keeps of its own, and where. spec/06-library.md, "What the app keeps of
// its own".
//
// On a PC it is all in one folder, `Pro7ChordEditor` in the person's local application
// data (%LOCALAPPDATA%). Nothing of the app's is ever put in ProPresenter's folders, or
// beside a song.
//
//   settings.json   the folders chosen by hand, the place and library last open, and
//                   where the bar between the two lists was left
//   marks.json      the marks worked out for each song (Marks.cs)
//   log.txt         a line for each thing the app did
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

        // (A list is the same as another when it has the same things in it, which is
        // not what a record makes of one by itself.)
        public bool Equals(Settings? other) =>
            other is not null && ChosenFolders.SequenceEqual(other.ChosenFolders) && LastPlace == other.LastPlace
            && LastLibrary == other.LastLibrary && ListsSplit == other.ListsSplit;

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

    // How marks.json is written: each file's path, and what was kept of it.
    private sealed record WrittenMarks(Dictionary<string, KeptMarks.Entry> Entries);

    /// <summary>The marks as they were last kept, or none.</summary>
    public KeptMarks ReadMarks() =>
        Read<WrittenMarks>("marks.json") is { Entries: { } entries } ? new KeptMarks(entries) : new KeptMarks();

    public void Keep(KeptMarks marks) => Write(new WrittenMarks(new Dictionary<string, KeptMarks.Entry>(marks.Entries)), "marks.json");

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
