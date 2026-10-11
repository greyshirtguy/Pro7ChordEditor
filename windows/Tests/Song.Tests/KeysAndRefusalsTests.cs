// The song's two keys (spec/05-song-and-saving.md, rules 4 to 8), and every refusal
// ("What a refusal says"): that each says the page's sentence, word for word, and
// changes nothing, in the file or in the song as the editors see it.
//
// "A save with the file changed underneath it, and one with the copy impossible to
// make, are refused and leave everything as it was" is the last of the page's proofs.

using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class KeysAndRefusalsTests : IDisposable
{
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    // ---- The keys

    [Fact]
    public void ASongThatStatesNoKeyIsGivenBothKeysTogether()
    {
        var song = bench.Open("Holy Holy Holy.pro");
        var asItWas = File.ReadAllBytes(song.Path);
        Assert.Equal("", song.Key);
        Assert.Equal("", song.ShownKey);
        Assert.Null(Workbench.Read(song.Path).Music);
        var chords = song.UsedChords;

        song.SetKey("F#m");

        Assert.Equal("F#m", song.Key);
        Assert.Equal("F#m", song.ShownKey);
        var music = Workbench.Read(song.Path).Music;
        Assert.Equal(MusicKeyScale.Types.MusicKey.FSharp, music.Original.MusicKey);
        Assert.Equal(MusicKeyScale.Types.MusicScale.Minor, music.Original.MusicScale);
        Assert.Equal(music.Original, music.User);
        // It changes no chord: it says what key the chords that are there are in.
        Assert.Equal(chords, song.UsedChords);

        // The key is a step of undo like any other, and undone the song states none
        // again: the file is what it was, to the byte.
        song.Undo();
        Assert.Equal("", song.Key);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        song.Redo();
        Assert.Equal("F#m", song.Key);
    }

    [Fact]
    public void ASongWhoseTwoKeysAreTheSameKeepsThemTheSame()
    {
        var song = bench.Open("Amazing Grace.pro");
        Assert.Equal("G", song.Key);
        Assert.Equal("G", song.ShownKey);

        song.SetKey("Bb");

        Assert.Equal("Bb", song.Key);
        Assert.Equal("Bb", song.ShownKey);
    }

    // "If they differed, someone chose to show the song in another key and that is left
    // alone."
    [Fact]
    public void ASongShownInAnotherKeyKeepsThatKeyWhenItsOwnIsSet()
    {
        var song = bench.Open("Amazing Grace.pro");

        song.SetShownKey("E");
        Assert.Equal("G", song.Key);
        Assert.Equal("E", song.ShownKey);

        song.SetKey("A");
        Assert.Equal("A", song.Key);
        Assert.Equal("E", song.ShownKey);

        // Each was a step of its own.
        song.Undo();
        Assert.Equal("G", song.Key);
        Assert.Equal("E", song.ShownKey);
        song.Undo();
        Assert.Equal("G", song.ShownKey);
        Assert.False(song.CanUndo);
    }

    // "A `music` with an `original` and no `user` in it is taken as two keys the same:
    // nobody chose another." A guess, the page says: no song looked at has one. Made here.
    [Fact]
    public void ASongWithNoShownKeyWrittenIsTakenAsOneWhoseTwoKeysAreTheSame()
    {
        var presentation = Workbench.Read(bench.CopyOf("Amazing Grace.pro"));
        presentation.Music.User = null;
        var song = bench.OpenAt(bench.FileOf("no-user-key.pro", presentation.ToByteArray()));
        Assert.Equal("G", song.Key);
        // (A key that is not written reads as its nought, which is A flat.)
        Assert.Equal("Ab", song.ShownKey);

        song.SetKey("D");

        Assert.Equal("D", song.Key);
        Assert.Equal("D", song.ShownKey);
    }

    // A key's note and scale are all that setting it writes: the other two things a
    // `music` can hold, and anything in it that the descriptions have no name for, stay.
    [Fact]
    public void SettingAKeyLeavesTheRestOfWhatSaysTheKeyAsItWas()
    {
        var presentation = Workbench.Read(bench.CopyOf("Amazing Grace.pro"));
        presentation.Music.OriginalMusicKey = "kept";
        var song = bench.OpenAt(bench.FileOf("more-in-music.pro", presentation.ToByteArray()));

        song.SetKey("Em");

        var music = Workbench.Read(song.Path).Music;
        Assert.Equal("kept", music.OriginalMusicKey);
        Assert.Equal(MusicKeyScale.Types.MusicKey.E, music.Original.MusicKey);
        Assert.Equal(MusicKeyScale.Types.MusicScale.Minor, music.User.MusicScale);
    }

    [Fact]
    public void WhatIsNoKeyIsRefusedAndSoIsAShownKeyForASongThatStatesNone()
    {
        var song = bench.Open("Holy Holy Holy.pro");

        Refused(song, () => song.SetKey("H"), Refusal.Why.NotAKey, (char)0x201C + "H" + (char)0x201D + " is not a key. Nothing was changed.");
        Refused(song, () => song.SetKey(""), Refusal.Why.NotAKey, (char)0x201C + "" + (char)0x201D + " is not a key. Nothing was changed.");
        Refused(song, () => song.SetShownKey("Hm"), Refusal.Why.NotAKey, (char)0x201C + "Hm" + (char)0x201D + " is not a key. Nothing was changed.");
        Refused(
            song,
            () => song.SetShownKey("G"),
            Refusal.Why.NoKeyToShowAnother,
            "This song does not say what key it is in. Set its key first, and then the key ProPresenter is to show it in.");
    }

    // ---- What is refused, in the order a save asks

    [Fact]
    public void ASongOpenedOnlyToBeLookedAtChangesNothing()
    {
        var song = new OpenSong(bench.CopyOf("Amazing Grace.pro"));
        var box = song.TextBoxes[0];

        Refused(song, () => song.SetChords(box.Slide, box.Element, []), Refusal.Why.OnlyForReading, "This song was opened only to be looked at. Nothing was changed.");
        Refused(song, () => song.SetKey("D"), Refusal.Why.OnlyForReading, "This song was opened only to be looked at. Nothing was changed.");
    }

    [Fact]
    public void NothingIsSavedWhileProPresenterIsRunning()
    {
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];
        bench.ProPresenterIsRunning = true;

        Refused(song, () => song.SetChords(box.Slide, box.Element, []), Refusal.Why.ProPresenterIsRunning, "ProPresenter is running. Quit it to change chords here. Nothing was changed.");
        Refused(song, () => song.SetKey("D"), Refusal.Why.ProPresenterIsRunning, "ProPresenter is running. Quit it to change chords here. Nothing was changed.");

        // It is asked before every save: once ProPresenter has quit, the change is made.
        bench.ProPresenterIsRunning = false;
        song.SetChords(box.Slide, box.Element, []);
        Assert.Empty(song.TextBoxes[0].Chords);
        // And started again, undo is a save too, and is refused.
        bench.ProPresenterIsRunning = true;
        Assert.Throws<Refusal>(song.Undo);
        Assert.Empty(song.TextBoxes[0].Chords);
        Assert.True(song.CanUndo);
    }

    // The file on disk is no longer the one that was read: in its size, in its time of
    // last change, or in its bytes alone (a file brought down by a syncing service can
    // come with its old time on it).
    [Theory]
    [InlineData("longer")]
    [InlineData("touched")]
    [InlineData("other bytes, same size and time")]
    [InlineData("gone")]
    public void NothingIsSavedOverAFileThatSomethingElseHasChanged(string how)
    {
        const string sentence = "This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now.";
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];
        var asItWas = File.ReadAllBytes(song.Path);
        var written = File.GetLastWriteTimeUtc(song.Path);
        switch (how)
        {
            case "longer":
                File.WriteAllBytes(song.Path, [.. asItWas, 0xF8, 0x7F, 0x01]);
                break;
            case "touched":
                File.SetLastWriteTimeUtc(song.Path, written.AddSeconds(2));
                break;
            case "other bytes, same size and time":
                // Another song's name inside, of the same length.
                var other = Workbench.Read(song.Path);
                other.Name = new string('x', other.Name.Length);
                File.WriteAllBytes(song.Path, other.ToByteArray());
                File.SetLastWriteTimeUtc(song.Path, written);
                Assert.Equal(asItWas.Length, new FileInfo(song.Path).Length);
                break;
            case "gone":
                File.Delete(song.Path);
                break;
        }
        var onDisk = File.Exists(song.Path) ? File.ReadAllBytes(song.Path) : null;
        Assert.False(song.FileIsAsLastRead);

        var refusal = Assert.Throws<Refusal>(() => song.SetChords(box.Slide, box.Element, []));

        Assert.Equal(Refusal.Why.ChangedByAnother, refusal.Reason);
        Assert.Equal(sentence, refusal.Sentence);
        // Nothing was saved over what the other left, and no copy was made of it.
        Assert.Equal(onDisk, File.Exists(song.Path) ? File.ReadAllBytes(song.Path) : null);
        Assert.Empty(bench.Copies);
        Assert.Equal(box.Chords, song.TextBoxes[0].Chords);
        Assert.False(song.CanUndo);
    }

    [Fact]
    public void AFileThatCouldNotFirstBeCopiedIsNotChanged()
    {
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];
        bench.KeepingACopyFails = "the disk is full.";

        Refused(
            song,
            () => song.SetChords(box.Slide, box.Element, []),
            Refusal.Why.CopyNotKept,
            "A copy of the song's file could not be kept first, so the song was not changed: the disk is full.");

        // Once a copy can be kept, the change is made, and the copy with it.
        bench.KeepingACopyFails = null;
        song.SetChords(box.Slide, box.Element, []);
        Assert.Single(bench.Copies);
    }

    // The file may not be changed (it is marked so), or another program has it open and
    // will not share it. What was being written beside it is cleared away, and a copy
    // HAS been kept by then, which is as it should be: the copy comes before the write.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFileThatCannotBeWrittenIsLeftAsItWasAndTheSongWithIt(bool markedNotToBeChanged)
    {
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];
        var asItWas = File.ReadAllBytes(song.Path);
        FileStream? held = null;
        if (markedNotToBeChanged)
        {
            File.SetAttributes(song.Path, FileAttributes.ReadOnly);
        }
        else
        {
            held = new FileStream(song.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        try
        {
            var refusal = Assert.Throws<Refusal>(() => song.SetChords(box.Slide, box.Element, []));

            Assert.Equal(Refusal.Why.NotSaved, refusal.Reason);
            Assert.StartsWith("The song could not be saved, and is as it was: ", refusal.Sentence, StringComparison.Ordinal);
        }
        finally
        {
            held?.Dispose();
            File.SetAttributes(song.Path, FileAttributes.Normal);
        }
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Equal([Path.GetFileName(song.Path)], Directory.EnumerateFileSystemEntries(bench.Folder).Select(Path.GetFileName));
        Assert.Equal(box.Chords, song.TextBoxes[0].Chords);
        Assert.False(song.CanUndo);
        Assert.Single(bench.Copies);

        // And when it can be written again, it is, with no second copy asked for.
        song.SetChords(box.Slide, box.Element, []);
        Assert.Empty(new OpenSong(song.Path).TextBoxes[0].Chords);
        Assert.Single(bench.Copies);
    }

    // ---- What names something that is not there

    [Fact]
    public void ASlideABoxOrALineThatIsNotThereIsRefused()
    {
        var song = bench.Open("Chords With No Words.pro");
        var alone = song.TextBoxes.First(box => !box.Blank && Chords.IsPlaceholders(box.Words));
        var words = song.TextBoxes.First(box => !box.Blank && !Chords.IsPlaceholders(Words.Lines(box.Words)[0]));

        Refused(song, () => song.SetChords(99, alone.Element, []), Refusal.Why.SlideGone, "That slide is no longer in the song. Nothing was changed.");
        Refused(song, () => song.SetChords(-1, alone.Element, []), Refusal.Why.SlideGone, "That slide is no longer in the song. Nothing was changed.");
        Refused(song, () => song.SetChordsAlone(99, alone.Element, 0, ["G"]), Refusal.Why.SlideGone, "That slide is no longer in the song. Nothing was changed.");
        Refused(song, () => song.SetChords(alone.Slide, "no-such-box", []), Refusal.Why.TextBoxGone, "That text box is no longer on the slide. Nothing was changed.");
        // (A box of another slide is not this slide's: a change names the slide too.)
        Refused(song, () => song.SetChords(words.Slide, alone.Element, []), Refusal.Why.TextBoxGone, "That text box is no longer on the slide. Nothing was changed.");
        Refused(song, () => song.SetChordsAlone(alone.Slide, "no-such-box", 0, ["G"]), Refusal.Why.TextBoxGone, "That text box is no longer on the slide. Nothing was changed.");
        Refused(song, () => song.SetChordsAlone(alone.Slide, alone.Element, 7, ["G"]), Refusal.Why.LineGone, "The text box has no such line. Nothing was changed.");
        Refused(song, () => song.SetChordsAlone(alone.Slide, alone.Element, -1, ["G"]), Refusal.Why.LineGone, "The text box has no such line. Nothing was changed.");
        // Only a line of chords alone is ever made stand-ins: the words of a song are
        // not this app's to change.
        Refused(song, () => song.SetChordsAlone(words.Slide, words.Element, 0, ["G"]), Refusal.Why.NotChordsAlone, "That is not a line of chords with no words. Nothing was changed.");
    }

    // ---- A file that is no song

    [Fact]
    public void AFileThatCannotBeReadSaysSo()
    {
        var missing = Path.Combine(bench.Folder, "Not There.pro");
        var refusal = Assert.Throws<Refusal>(() => new OpenSong(missing));
        Assert.Equal(Refusal.Why.CannotBeRead, refusal.Reason);
        Assert.StartsWith("Not There.pro could not be read as a ProPresenter presentation: ", refusal.Sentence, StringComparison.Ordinal);

        // A file cut short, as the first editor could leave one (spec/08, H): it says it
        // holds more than it has.
        var whole = File.ReadAllBytes(bench.CopyOf("Amazing Grace.pro"));
        var cut = bench.FileOf("Cut Short.pro", whole[..(whole.Length / 2)]);
        refusal = Assert.Throws<Refusal>(() => new OpenSong(cut));
        Assert.Equal(Refusal.Why.CannotBeRead, refusal.Reason);
        Assert.Equal("Cut Short.pro could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", refusal.Sentence);
    }

    // A refusal says its sentence, and changes nothing: not the file, not the song as
    // the editors see it, not what can be undone; and (before a copy is due) no copy.
    private void Refused(OpenSong song, System.Action ask, Refusal.Why why, string sentence)
    {
        var bytes = File.ReadAllBytes(song.Path);
        var boxes = song.TextBoxes.Select(box => (box.Element, box.Words, string.Join(" ", box.Chords))).ToList();
        var (key, shown, canUndo, copies) = (song.Key, song.ShownKey, song.CanUndo, bench.Copies.Count);

        var refusal = Assert.Throws<Refusal>(ask);

        Assert.Equal(why, refusal.Reason);
        Assert.Equal(sentence, refusal.Sentence);
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
        Assert.Equal(boxes, song.TextBoxes.Select(box => (box.Element, box.Words, string.Join(" ", box.Chords))));
        Assert.Equal((key, shown, canUndo), (song.Key, song.ShownKey, song.CanUndo));
        Assert.Equal(copies, bench.Copies.Count);
        // And nothing that was being written beside the song is left there.
        Assert.DoesNotContain(Directory.EnumerateFiles(bench.Folder), path => path.EndsWith(".saving", StringComparison.Ordinal));
    }
}
