// Why a thing was not done, as a sentence for the person.
//
// Everything the song may be asked either is done and saved, or is refused and changes
// nothing: not the file, and not the song in memory. A refusal says why in words that
// can be shown as they are. They say plainly what happened and what, if anything, the
// person can do; they promise nothing (AGENTS.md).
//
// The sentences are those of spec/05-song-and-saving.md ("What a refusal says"), word
// for word, so that the two apps say the same. The Mac's is Refusal.swift.
//
// A refusal is thrown, as on the Mac: whoever asks the song for a change either gets
// the change or catches this, and shows its sentence.

namespace Pro7ChordEditor.Song;

public sealed class Refusal : Exception
{
    public enum Why
    {
        /// <summary>The file could not be read as a presentation.</summary>
        CannotBeRead,

        /// <summary>The song was opened to be looked at, and was given no way to save.</summary>
        OnlyForReading,

        /// <summary>There is no slide of that number.</summary>
        SlideGone,

        /// <summary>The slide has no text box of that id.</summary>
        TextBoxGone,

        /// <summary>The box has no line of that number.</summary>
        LineGone,

        /// <summary>What was given is no key.</summary>
        NotAKey,

        /// <summary>The song states no key, so it cannot be shown in another.</summary>
        NoKeyToShowAnother,

        /// <summary>The line asked for is not a line of chords alone: it has words, or is empty.</summary>
        NotChordsAlone,

        /// <summary>The stand-ins a chord with no words hangs on could not be written into the box's RTF and read back as meant.</summary>
        CannotBeWritten,

        /// <summary>A blank slide was to be given chords, and the song has no text box with words to model its own on.</summary>
        NoBoxToModelOn,

        /// <summary>A slide that was to be given a text box for its chords has one by now.</summary>
        NoLongerBlank,

        ProPresenterIsRunning,

        /// <summary>The file on disk is no longer the one that was read.</summary>
        ChangedByAnother,

        /// <summary>The copy that is kept before a first change could not be made.</summary>
        CopyNotKept,

        /// <summary>The file could not be written, or did not read back as written.</summary>
        NotSaved,

        /// <summary>A copy's chords were to be put into the song, and the two have not the same words.</summary>
        WordsDiffer,

        /// <summary>A file that was to be read as a ChordPro song is not one: it cannot be read, is too large, or has no words in it.</summary>
        NotASong,
    }

    private Refusal(Why reason, string sentence)
        : base(sentence)
    {
        Reason = reason;
    }

    public Why Reason { get; }

    /// <summary>The sentence to show.</summary>
    public string Sentence => Message;

    internal static Refusal CannotBeRead(string name, string why) =>
        new(Why.CannotBeRead, name + " could not be read as a ProPresenter presentation: " + why);

    internal const string NoPresentation = "it is not a presentation, or it is damaged.";

    internal static Refusal OnlyForReading => new(Why.OnlyForReading, "This song was opened only to be looked at. Nothing was changed.");

    // (Of the song shown as it would be with its chords moved: SongAsItWouldBe.cs.)
    internal static Refusal ShownAsItWouldBe => new(
        Why.OnlyForReading,
        "This is the song as it would be with its chords moved, shown to be looked at. Nothing was changed.");

    // (Of a ChordPro file: ChordProFile.cs. The reference's three sentences, with the
    // file's name first as this app's others have it.)
    internal static Refusal FileCannotBeRead(string name, string why) => new(Why.NotASong, name + " could not be read: " + why);

    internal static Refusal TooLargeToBeASong(string name) => new(Why.NotASong, name + " is too large to be a song.");

    internal static Refusal NoWordsInIt(string name) => new(Why.NotASong, name + " has no words in it.");

    internal static Refusal FileNotWritten(string name, string why) => new(Why.NotSaved, name + " could not be written: " + why);

    internal static Refusal WordsDiffer => new(
        Why.WordsDiffer,
        "The two songs have not the same words, so the chords of the one cannot be put into the other. Nothing was changed.");

    internal static Refusal SlideGone => new(Why.SlideGone, "That slide is no longer in the song. Nothing was changed.");

    internal static Refusal TextBoxGone => new(Why.TextBoxGone, "That text box is no longer on the slide. Nothing was changed.");

    internal static Refusal LineGone => new(Why.LineGone, "The text box has no such line. Nothing was changed.");

    // (The key is set between the quotation marks that curl, U+201C and U+201D, as the
    // page has it.)
    internal static Refusal NotAKey(string key) =>
        new(Why.NotAKey, (char)0x201C + key + (char)0x201D + " is not a key. Nothing was changed.");

    internal static Refusal NoKeyToShowAnother => new(
        Why.NoKeyToShowAnother,
        "This song does not say what key it is in. Set its key first, and then the key ProPresenter is to show it in.");

    internal static Refusal NotChordsAlone => new(Why.NotChordsAlone, "That is not a line of chords with no words. Nothing was changed.");

    internal static Refusal CannotBeWritten => new(
        Why.CannotBeWritten,
        "A chord could not be added to this line, or taken from it: its text box is written in a way that cannot be changed here safely. Nothing was changed.");

    internal static Refusal NoBoxToModelOn => new(
        Why.NoBoxToModelOn,
        "This song has no text box with words in it to model one on, so chords cannot be put on a slide that has none. Nothing was changed.");

    internal static Refusal NoLongerBlank => new(Why.NoLongerBlank, "That slide is no longer blank. Nothing was changed.");

    internal static Refusal ProPresenterIsRunning => new(Why.ProPresenterIsRunning, "ProPresenter is running. Quit it to change chords here. Nothing was changed.");

    internal static Refusal ChangedByAnother => new(
        Why.ChangedByAnother,
        "This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now.");

    internal static Refusal CopyNotKept(string why) =>
        new(Why.CopyNotKept, "A copy of the song's file could not be kept first, so the song was not changed: " + why);

    internal static Refusal NotSaved(string why) => new(Why.NotSaved, "The song could not be saved, and is as it was: " + why);
}
