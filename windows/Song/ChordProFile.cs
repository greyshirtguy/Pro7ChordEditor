// A ChordPro file: one read from a disk as a song, and a song written to one.
// spec/09-chordpro-files.md, rules 1 and 7 (reading a file), and 17 to 19 (writing).
//
// What a ChordPro file says is the rules' to read (`Chords.ParseSong`, held to
// testdata/vectors/parse-song.json since M1). Here is only what is about the FILE:
//
//   - its bytes as text: UTF-8, with or without a mark at its start; and if they are not
//     valid UTF-8, Latin-1, which at least keeps every accented letter of western Europe
//     (an old file, typed on Windows);
//   - that anything over two megabytes is not a song, and is refused unread;
//   - that a file with no part in it has no words, and is refused;
//   - and a song written the other way, as text and as a file.
//
// The reference is `readFile` of reference/simple-presenter/src/songimport.cpp for the
// reading. The writing is NEW: Simple Presenter writes no ChordPro file.
//
// WRITING, to the letter of rule 18: a heading ({title}, and {key} if the song states
// one); then for each group an empty line and {comment: <its name>}; then each line of
// each of its slides' text boxes as `ToChordPro` writes it, with an empty line between
// one slide and the next. It is the text the ChordPro editor shows (SongText.cs), with
// the heading on it and a group's name written out in full.
//
// WHAT COMES BACK. The page has it that such a file, read again, "gives the same parts,
// lines and chords", and for a song whose groups are named as parts are named (Verse 1,
// Chorus, Bridge and their like) it does: the tests hold that. A group with any other
// name does not: by the rules of reading, a {comment} begins a part only if what it says
// is the name of one (rule 3), so a group called "The Middle Eight" reads back as more
// of the part before it. One of the test songs has such a group. That is a thing found,
// and is in NOTES.md for the maintainer: nothing here goes round it.
//
// Nothing here touches a library. Writing a file writes where the person said, and
// reading one changes nothing.

using System.Text;
using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

public static class ChordProFile
{
    /// <summary>Anything larger than this is not a song (rule 1): two megabytes.</summary>
    public const int MostBytes = 2 * 1024 * 1024;

    /// <summary>The ending a ChordPro file is saved with (rule 19).</summary>
    public const string Ending = ".cho";

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Reads a song from a ChordPro file. Nothing is changed.</summary>
    /// <exception cref="Refusal">The file cannot be read, is too large to be a song, or has no words in it.</exception>
    public static ParsedSong Read(string path)
    {
        var name = Path.GetFileName(path);
        byte[] bytes;
        try
        {
            // (Asked of the file system before any of it is read: a file of gigabytes is
            // not read to find out that it is not a song.)
            if (new FileInfo(path).Length > MostBytes)
            {
                throw Refusal.TooLargeToBeASong(name);
            }
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw Refusal.FileCannotBeRead(name, exception.Message);
        }
        if (bytes.Length > MostBytes)
        {
            throw Refusal.TooLargeToBeASong(name);
        }
        var song = Chords.ParseSong(TextOf(bytes));
        return song.Sections.Count > 0 ? song : throw Refusal.NoWordsInIt(name);
    }

    /// <summary>
    /// A file's bytes as text: UTF-8, with or without a mark at its start; Latin-1 if
    /// they are not valid UTF-8.
    /// </summary>
    internal static string TextOf(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> mark = [0xEF, 0xBB, 0xBF];
        var marked = bytes.StartsWith(mark);
        try
        {
            return StrictUtf8.GetString(marked ? bytes[mark.Length..] : bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    /// <summary>
    /// A song as ChordPro text (rules 17 and 18): its name, its key if it states one,
    /// and its text boxes as the editors are handed them.
    /// </summary>
    public static string TextOf(string name, string key, IReadOnlyList<TextBox> boxes)
    {
        var text = new StringBuilder();
        text.Append("{title: ").Append(OnOneLine(name)).Append("}\n");
        if (key.Length > 0)
        {
            text.Append("{key: ").Append(key).Append("}\n");
        }
        // (Whether anything has been written under the heading yet: what comes first is
        // parted from the heading by one empty line.)
        var begun = false;
        foreach (var line in SongText.Lines(boxes))
        {
            switch (line.Kind)
            {
                case SongLineKind.Group:
                    // (The empty line before it is the gap before its first slide; or,
                    // for the song's first group, the one after the heading.)
                    if (!begun)
                    {
                        text.Append('\n');
                    }
                    text.Append("{comment: ").Append(OnOneLine(line.Group)).Append("}\n");
                    begun = true;
                    break;
                case SongLineKind.Gap:
                    text.Append('\n');
                    begun = true;
                    break;
                default:
                    if (!begun)
                    {
                        // A song whose first slide is in no group: its lines are parted
                        // from the heading as a group's are.
                        text.Append('\n');
                        begun = true;
                    }
                    text.Append(line.Text).Append('\n');
                    break;
            }
        }
        return text.ToString();
    }

    /// <summary>Writes ChordPro text as a file, where the person said: UTF-8, with no mark at its start (rule 19).</summary>
    /// <exception cref="Refusal">The file could not be written.</exception>
    public static void Write(string path, string text)
    {
        try
        {
            File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw Refusal.FileNotWritten(Path.GetFileName(path), exception.Message);
        }
    }

    // What goes inside a directive is on one line, and cannot close the directive.
    private static string OnOneLine(string said) =>
        said.Replace('\r', ' ').Replace('\n', ' ').Replace('}', ')').Replace('{', '(');
}
