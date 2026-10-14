// The "(Chords)" copies the first Windows editor saved, and what putting one back into
// the song it was saved beside would do. spec/08-repair.md, D.
//
// That editor never saved over the song it was given. It saved a new file beside it,
// "<the name inside the song> (Chords).pro", with the same name and id inside. So a
// library that was chorded in it has pairs, "X.pro" and "X (Chords).pro", of which the
// first is the one ProPresenter's playlists point at and has no chords, and the second
// has the chords and is in no playlist. (And chains, "X (Chords) (Chords).pro" beside
// "X (Chords).pro", where a copy was itself opened and saved.)
//
// FOLDING a copy back puts its chords and its key into the song it was saved beside. It
// is offered only where the two have the same words, box for box, in the same order:
// then a chord of the copy means the same character in the original. A box here is what
// it is to the checks (Checks.cs): every text of the file's slides that has RTF, in the
// file's order.
//
// NEW: Simple Presenter has nothing of this, and nor has the Mac app yet. Nothing here
// changes anything: this finds a copy's original and says what folding would do
// (`Plan`). The doing is OpenSong's (`FoldIn`), which is asked by the window's model
// once the person has read what is about to be done and said yes; and what then
// becomes of the copy's own file is asked after, and is the model's too.

using Google.Protobuf;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <summary>What putting a copy's chords into the song it was saved beside would do, worked out before anything is done.</summary>
/// <param name="SameWords">Whether the two have the same words, box for box, in the same order. Only then is it offered.</param>
/// <param name="CopysChords">How many chords the copy has, in every text box of its file.</param>
/// <param name="OriginalsChords">How many the original has of its own, which the copy's would take the place of.</param>
/// <param name="CopysKey">The key the copy states, or "" if it states none: what the original's would become.</param>
/// <param name="OriginalsKey">The key the original states, or "".</param>
/// <param name="NothingToDo">Whether the original has the copy's chords and its key already, so that folding would change nothing: it was folded before.</param>
/// <param name="CopysLikelierKey">Where the copy's own key does not fit its chords (check 3), the key they look like; else "". That editor wrote a wrong key into some of its copies, and folding makes the copy's key the song's.</param>
public sealed record FoldPlan(
    bool SameWords,
    int CopysChords,
    int OriginalsChords,
    string CopysKey,
    string OriginalsKey,
    bool NothingToDo = false,
    string CopysLikelierKey = "");

public static class Folding
{
    /// <summary>
    /// The name of the song a copy was saved beside: the copy's own name less the
    /// ending that editor gave it. Nothing, for a name that has not that ending, or
    /// that is nothing but it.
    /// </summary>
    public static string? OriginalsName(string copysName) =>
        copysName.EndsWith(Marks.OldEditorsEnding, StringComparison.Ordinal) && copysName.Length > Marks.OldEditorsEnding.Length
            ? copysName[..^Marks.OldEditorsEnding.Length]
            : null;

    /// <summary>
    /// The song a copy was saved beside, if it is still there: the one of that name in
    /// the same folder, among the songs listed. (Of "X (Chords) (Chords)" that is
    /// "X (Chords)", which is itself a copy, of "X".)
    /// </summary>
    public static ListedSong? OriginalOf(ListedSong copy, IEnumerable<ListedSong> songs)
    {
        if (OriginalsName(copy.Name) is not { } name || Path.GetDirectoryName(copy.Path) is not { } folder)
        {
            return null;
        }
        var beside = Path.Combine(folder, name + Path.GetExtension(copy.Path));
        return songs.FirstOrDefault(song => Paths.Same(song.Id, Paths.IdOf(beside)));
    }

    /// <summary>What folding this copy into this original would do. Both files are read, and nothing else.</summary>
    /// <exception cref="Refusal">One of the two cannot be read, or is no presentation.</exception>
    public static FoldPlan Plan(string copyPath, string originalPath)
    {
        var copy = Read(copyPath);
        var original = Read(originalPath);
        var folded = Folded(original, copy);
        var found = Checks.Of(copy);
        return new FoldPlan(
            folded is not null,
            Checks.Boxes(copy).Sum(box => box.Chords.Count),
            Checks.Boxes(original).Sum(box => box.Chords.Count),
            copy.Music is null ? "" : Reading.KeyName(copy.Music.Original),
            original.Music is null ? "" : Reading.KeyName(original.Music.Original),
            folded is not null && folded.Equals(original),
            found.Key.Doubtful ? found.Key.Suggested : "");
    }

    /// <summary>
    /// A song as folding a copy into it would leave it: each of its text boxes with the
    /// chords of the box in its place in the copy, written as chords are written here,
    /// and its `music` the copy's. Nothing, if the two have not the same words. Neither
    /// of the two is changed.
    /// </summary>
    internal static RV.Presentation? Folded(RV.Presentation original, RV.Presentation copy)
    {
        if (!SameWords(original, copy))
        {
            return null;
        }
        var folded = original.Clone();
        var mine = Checks.Texts(folded).ToList();
        var theirs = Checks.Texts(copy).ToList();
        for (var box = 0; box < mine.Count; box++)
        {
            var chords = Reading.ChordsOf(theirs[box]);
            // (A box with no settings written, that the copy has no chords for, is left
            // as it is. Writing it no chords would give it an empty set of settings, as
            // an editor's request does since 2026-10-14 (Writing.WriteChords); but that
            // is no part of what the copy has to give, "mending changes nothing else in
            // the file" (spec/08-repair.md), and a song that has such a box would never
            // be found to be folded already. MINE: no page has the case, and the Mac
            // app has no mending yet to do the same.)
            if (mine[box].Attributes is null && chords.Count == 0)
            {
                continue;
            }
            Writing.WriteChords(chords, mine[box]);
        }
        folded.Music = copy.Music?.Clone();
        return folded;
    }

    /// <summary>
    /// Whether two songs have the same words, box for box, in the same order: as many
    /// text boxes, and each with the words of the one that is in its place in the other.
    /// </summary>
    internal static bool SameWords(RV.Presentation one, RV.Presentation other) =>
        Checks.Texts(one).Select(Reading.WordsOf).SequenceEqual(Checks.Texts(other).Select(Reading.WordsOf), StringComparer.Ordinal);

    private static RV.Presentation Read(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw Refusal.CannotBeRead(Path.GetFileName(path), exception.Message);
        }
        try
        {
            return RV.Presentation.Parser.ParseFrom(bytes);
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.CannotBeRead(Path.GetFileName(path), Refusal.NoPresentation);
        }
    }
}
