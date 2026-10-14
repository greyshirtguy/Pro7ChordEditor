// Writing: a box's chords into the message, and the message into the song's file.
// spec/01-file-format.md, "Chords" (writing a box's chords) and "Writing". The Mac's is
// Writing.swift.
//
// This is the part of the app that can hurt somebody's library, so it does as little as
// can be done and checks what it did:
//
//   - A box's chord entries are replaced and nothing else in the box is touched. Every
//     custom attribute that is not a chord stays, in its order; the chords go after
//     them, in order of place, each with the range the rules give it (Chords.Ranges).
//     In every song from Multitracks looked at, that is the order the file has them in
//     already, so a box given the chords it had is, entry for entry, what it was.
//     (One thing of those other attributes does change, and in one case only: where a
//     line of chords alone is made longer or shorter, the ends of their ranges go
//     with the words. MoveOtherRanges, below, which says why.)
//   - The file is never half written. The whole message is written to a new file beside
//     the song's own, pushed out of the system's keeping onto the disk, and put in the
//     place of the old one in one step. Then it is read back, and must be byte for byte
//     what was meant.
//
// The RTF of a box is not written here. The one case in which it is written at all,
// the stand-ins of chords that have no words, is OpenSong's to ask and RtfWords' to do
// (spec/01-file-format.md, "Chords with no words"): bytes changed in place, and read
// back before anything is saved.

using Pro7ChordEditor.ChordRules;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

internal static class Writing
{
    /// <summary>
    /// Replaces a box's chord entries with these chords.
    ///
    /// A TEXT THAT HAS NO SETTINGS AT ALL WRITTEN IS GIVEN A SET, EMPTY IF IT IS GIVEN
    /// NO CHORDS (spec/05-song-and-saving.md, under the three requests). The chords
    /// are kept in the settings, so the settings are made before anything else, as
    /// the reference's `writeChords` begins with `mutable_attributes()` and the Mac's
    /// port reaches into them. An empty set reads as none does, and is two bytes more
    /// in the file: so such a box given no chords is a change, saved, with a copy kept
    /// and a step of undo, in all three apps. Until 2026-10-14 this app left such a
    /// box alone; the maintainer said to do as the other two ("yes change windows to
    /// match"). No file to hand has such a text box: windows/NOTES.md, "At M2".
    /// </summary>
    internal static void WriteChords(IEnumerable<Chord> chords, RV.Graphics.Types.Text text)
    {
        var ranges = Chords.Ranges(Reading.WordsOf(text), chords);
        text.Attributes ??= new RV.Graphics.Types.Text.Types.Attributes();
        var entries = text.Attributes.CustomAttributes.Where(attribute => !Reading.IsAChord(attribute)).ToList();
        foreach (var range in ranges)
        {
            entries.Add(new RV.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute
            {
                Range = new RV.IntRange { Start = range.Start, End = range.End },
                Chord = range.Name,
            });
        }
        text.Attributes.CustomAttributes.Clear();
        text.Attributes.CustomAttributes.AddRange(entries);
    }

    /// <summary>
    /// What a box says of a range of its words, and is not a chord, goes with the
    /// words when a line of chords alone in it is made longer or shorter:
    /// <paramref name="lineStart"/> is where that line began, <paramref name="was"/>
    /// how long it was and <paramref name="now"/> how long it is.
    ///
    /// A text box has custom attributes that are no chords: "these are in capitals", a
    /// gradient. Most boxes that hold a line of chords alone, as Multitracks makes
    /// them, say of all their words that they are in capitals. Left as it was, that
    /// range would stop short of a stand-in put in, and with a chord taken off would
    /// end past the last of the words. So each end of each such range is moved as
    /// RtfWords.PlaceAfter says, which is what Simple Presenter does and A GUESS at
    /// what ProPresenter does (decided by the maintainer on 2026-10-13: "let's copy
    /// simple presenter for now"; spec/01-file-format.md, "The box's other ranges").
    /// Until then this app left them where they were.
    ///
    /// Nothing else of such an entry changes, none is added, taken away or put in
    /// another place in the list, and one that has no range is not given one. An end
    /// that is where it was is not written again. (The chords' own ranges are written
    /// afresh by WriteChords, after this.)
    /// </summary>
    internal static void MoveOtherRanges(RV.Graphics.Types.Text text, int lineStart, int was, int now)
    {
        if (was == now || text.Attributes is null)
        {
            return;
        }
        foreach (var entry in text.Attributes.CustomAttributes)
        {
            if (Reading.IsAChord(entry) || entry.Range is null)
            {
                continue;
            }
            var start = RtfWords.PlaceAfter(entry.Range.Start, lineStart, was, now);
            var end = RtfWords.PlaceAfter(entry.Range.End, lineStart, was, now);
            if (start != entry.Range.Start)
            {
                entry.Range.Start = start;
            }
            if (end != entry.Range.End)
            {
                entry.Range.End = end;
            }
        }
    }
}

/// <summary>A song's file on disk.</summary>
internal static class SongFile
{
    /// <summary>
    /// What tells one state of a file from another without reading it: its size and its
    /// time of last change, as finely as the file system keeps it.
    /// </summary>
    internal readonly record struct Stamp(long Size, DateTime LastChanged);

    /// <summary>What went wrong with a file, in words that can end a sentence.</summary>
    internal sealed class Failure(string why) : Exception(why);

    internal static Stamp StampOf(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            throw new Failure("the file is not there.");
        }
        return new Stamp(file.Length, file.LastWriteTimeUtc);
    }

    /// <summary>Puts these bytes in the place of the file, whole or not at all.</summary>
    internal static void Write(byte[] bytes, string path)
    {
        // Beside the song, so that the swap is within one disk and is one step. Its name
        // is the song's with more after it, so that it is not a presentation to
        // ProPresenter for the moment it is there, and no two saves can pick the same
        // one.
        var beside = path + "." + Guid.NewGuid().ToString("N") + ".saving";
        try
        {
            using (var file = new FileStream(beside, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                // Onto the disk itself, before it takes the song's place: after a power
                // cut the song is then the old one or the new one, and never an empty
                // file.
                file.Flush(flushToDisk: true);
            }
            // (This keeps the old file's date of making, and who may read and change it.)
            File.Replace(beside, path, destinationBackupFileName: null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            // What was being written is of no use now. The song's own file was not touched.
            try
            {
                File.Delete(beside);
            }
            catch (Exception stillThere) when (stillThere is IOException or UnauthorizedAccessException)
            {
                // It could not be cleared away either. It is named so that it can be
                // told for what it is, and it is not the song.
            }
            throw new Failure(exception.Message);
        }
        byte[] back;
        try
        {
            back = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new Failure("the file could not be read back: " + exception.Message);
        }
        if (!back.AsSpan().SequenceEqual(bytes))
        {
            throw new Failure("the file did not read back as it was written.");
        }
    }

    /// <summary>
    /// Makes a NEW file of these bytes, whole or not at all, and never in the place of
    /// one that is there: if something has that name already, nothing is written and
    /// the answer is no. (A song made from a ChordPro file: NewSong.cs.)
    ///
    /// As a save is done: written beside where it is to be, under a name that is no
    /// presentation's, pushed onto the disk, and then moved to its name in one step,
    /// which the system refuses if the name is taken. Then read back.
    /// </summary>
    internal static bool WriteNew(byte[] bytes, string path)
    {
        var beside = path + "." + Guid.NewGuid().ToString("N") + ".saving";
        try
        {
            using (var file = new FileStream(beside, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            try
            {
                // (Not asked first whether the name is free: the moving is the asking,
                // and is one step, so nothing can take the name in between.)
                File.Move(beside, path, overwrite: false);
            }
            catch (IOException) when (File.Exists(path) || Directory.Exists(path))
            {
                // The name is taken. What is there is not this app's, and is left.
                ClearAway(beside);
                return false;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            ClearAway(beside);
            throw new Failure(exception.Message);
        }
        byte[] back;
        try
        {
            back = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new Failure("the file could not be read back: " + exception.Message);
        }
        if (!back.AsSpan().SequenceEqual(bytes))
        {
            throw new Failure("the file did not read back as it was written.");
        }
        return true;
    }

    // What was being written beside a new file is of no use now. (It is this app's
    // own, by the name it was given a moment ago, and nothing else is touched.)
    private static void ClearAway(string beside)
    {
        try
        {
            File.Delete(beside);
        }
        catch (Exception stillThere) when (stillThere is IOException or UnauthorizedAccessException)
        {
            // It is named so that it can be told for what it is, and it is no song.
        }
    }
}
