// A new song, made from a ChordPro file and from a song that is known to be good.
// spec/09-chordpro-files.md, rules 8 to 16.
//
// THE PART TO BE CAREFUL WITH. Simple Presenter builds a presentation from nothing
// (reference/simple-presenter/src/songimport.cpp, `build`), on a guess that ProPresenter
// will open it. The page says not to do it that way, and this does not: the new song is
// a COPY of one that ProPresenter itself wrote (the person's own, or the one the app
// carries), with its groups and slides taken out, and for each slide wanted a copy of
// one slide of that song, whose text box is given the slide's words and chords.
// Nothing about how a slide looks is written here from what this app knows of the
// format: it all comes across with the copy.
//
// WHAT THE PAGE SAYS, and what is done to its letter:
//   - the copy has a new id and name, no groups and no slides (rule 11);
//   - a slide is a copy of the model's slide with new ids (rule 11);
//   - the words are written after the RTF's own opening and read back (rule 12:
//     RtfWords.WithWords);
//   - a group for each part, by name, with no colour of its own, holding its slides'
//     ids in order (rule 13);
//   - the key, both parts, if the file states one; title, artist, copyright and a CCLI
//     number where ProPresenter keeps a song's (rule 14);
//   - no `multi_tracks_licensing` (rule 15);
//   - the file is "<name>.pro" in the library, with a number after the name if that is
//     taken, and a slash in the name is a hyphen (rule 16).
//
// WHERE THE PAGE IS SILENT, and what was decided here. Each is in windows/NOTES.md for
// the maintainer to say and for the Mac, and none is known to be what ProPresenter
// wants: no song made here has been opened in ProPresenter yet.
//
//   1. WHICH SLIDE is "the model's first". The first, in the order the song is shown,
//      that has a text box with words to read; failing that, the first that has a line
//      of chords alone. (A song often begins with an empty slide, which has no text
//      to be made like.) A song with neither cannot be a model, and that is refused.
//
//   2. A SLIDE IS COPIED AS IT LOOKS, AND DOES NOTHING ELSE. In the file a slide is a
//      cue, and a cue may also start a video, run a macro, go on to the next slide by
//      itself after a time, answer to a key, and carry a label and notes. Those are
//      what that one slide of the model song DOES and SAYS, and the new song would do
//      them on every one of its slides. So of the cue only its slide is copied; and
//      its label's words, its notes, its chord chart, its key and its going on are
//      left out.
//
//   3. WHAT IS THE MODEL SONG'S OWN is not the new song's: its notes, its chord chart,
//      its timeline, its arrangements, when it was last used, a Bible reference, a
//      slide show's timing, the old way of writing a key; and of its CCLI block
//      everything but whether it is to be shown (its author, its year, its album and
//      its artwork are another song's). A key the file does not state is not stated:
//      the model's is not kept.
//
//   4. WHICH TEXT BOX. The slide's first that shows words of its own and has words.
//      Any OTHER text box of the slide that shows words of its own is given none: they
//      are the model song's words (a second language, a title), and would be on every
//      slide. Its RTF keeps its opening, so words typed into it in ProPresenter are
//      set as they were. A box that shows something else by a link (the next slide's
//      words, a clock) is left exactly as it is.
//
//   5. WHAT IS SAID OF STRETCHES OF THE WORDS. A text box keeps, beside its RTF, a list
//      of things said of stretches of its words that RTF cannot say: that they are in
//      capitals, and the like (the chords are in the same list). Each names its
//      stretch by places in the MODEL's words. In the one file to hand that
//      ProPresenter wrote, every slide has one of these, over exactly the whole of
//      that slide's own words. So here: one that covered the model's first character
//      is given the whole of the new words, as the RTF's opening sets all of them as
//      the model's first; any other is left out, the words it was over being gone.
//      (Left as they were, a slide of more words than the model's would have its
//      first fifty-four characters in capitals and the rest not, if ProPresenter goes
//      by them.) THIS IS NOT what a text box given stand-ins on a blank slide gets
//      (spec/01-file-format.md, "Chords onto a blank slide", which leaves them).
//
//   6. THE IDS. New for the cue, its action, its slide, each element and each
//      element's builds; and every other id inside the cue that names one of those is
//      changed to match (a text box's own id is named by the link that says when it is
//      seen, in ProPresenter's own file, on every slide). An id that names something
//      outside the slide is left.
//
//   7. THE NAME on Windows. A file's name cannot have \ : * ? " < > | in it, nor end
//      in a full stop or a space, nor be one of the names Windows keeps for devices.
//      Each such character is a hyphen, as a slash is; a device's name is taken to be
//      in use already, so that the song is "Con 2"; and since "Nul.x" is the device
//      too, a full stop after such a name is a hyphen.
//
// AND WHAT IS CHECKED before a byte is written: the presentation that was made is
// read as any song is read (Reading.cs), and must be the parts, slides, words and
// chords that were asked for and nothing else. Then the file is written beside where
// it is to be and moved into place, never over a file that is there, and read back.
//
// Nothing is written while ProPresenter is running: it is a file in ProPresenter's
// own library (AGENTS.md, "What must stay true", 1).
//
// (The ids are found by going through the message field by field, as the reader of
// the format describes it to itself. Nothing else here does that.)

using System.Collections;
using System.Globalization;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Pro7ChordEditor.ChordRules;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <summary>One part of a song found in a ChordPro file: its name, and how many lines it has.</summary>
public sealed record FoundPart(string Name, int Lines);

/// <summary>
/// What was found in a ChordPro file (rule 8), for the person to see before anything
/// is made: its title, artist and key, each part with how many lines it has, and how
/// many chords in all.
/// </summary>
/// <param name="Chords">The chords as they will be kept: two written on one place of a line are one.</param>
public sealed record FoundInAFile(string Title, string Artist, string Key, IReadOnlyList<FoundPart> Parts, int Chords)
{
    public int Lines => Parts.Sum(part => part.Lines);

    /// <summary>How many slides so many lines to a slide makes of it (rule 9).</summary>
    public int Slides(int linesToASlide)
    {
        var each = Math.Max(1, linesToASlide);
        return Parts.Sum(part => (part.Lines + each - 1) / each);
    }
}

/// <summary>The song a new one is made to look like: its name, for a sentence, and its file's bytes.</summary>
public sealed record ModelSong(string Name, ReadOnlyMemory<byte> Bytes)
{
    private const string Carried = "Pro7ChordEditor.Song.CarriedModel.pro";

    /// <summary>
    /// The song the app carries (rule 10), for when the person picks none of their
    /// own: the one file to hand that ProPresenter itself wrote. Whether it is the one
    /// to carry is the maintainer's to say (NOTES.md): it is their own library's look.
    /// </summary>
    public static ModelSong OfTheApp
    {
        get
        {
            using var from = typeof(ModelSong).Assembly.GetManifestResourceStream(Carried)
                ?? throw new InvalidOperationException("The song this app carries is not in it: " + Carried);
            using var bytes = new MemoryStream();
            from.CopyTo(bytes);
            return new ModelSong("", bytes.ToArray());
        }
    }

    /// <summary>A song of the person's own, read from its file. Nothing is changed.</summary>
    /// <exception cref="Refusal">The file cannot be read.</exception>
    public static ModelSong Read(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        try
        {
            return new ModelSong(name, File.ReadAllBytes(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw Refusal.CannotBeRead(Path.GetFileName(path), exception.Message);
        }
    }
}

/// <summary>A song that was made: where its file is, the name it has, and what is in it.</summary>
public sealed record MadeSong(string Path, string Name, int Parts, int Slides, int Chords);

public static class NewSong
{
    /// <summary>How many lines go on a slide unless the person says otherwise (rule 8).</summary>
    public const int UsualLinesToASlide = 2;

    /// <summary>
    /// The most lines to a slide that the window lets be chosen. (More than this is no
    /// slide to be read; and a part of fewer lines is one slide.)
    /// </summary>
    public const int MostLinesToASlide = 12;

    private const string Ending = ".pro";

    // ---- Before it is brought in (rule 8)

    /// <summary>What is in a song read from a ChordPro file.</summary>
    public static FoundInAFile Found(ParsedSong song) => new(
        song.Title,
        song.Artist,
        Chords.KeyNumber(song.Key) < 0 ? "" : song.Key,
        [.. song.Sections.Select(part => new FoundPart(part.Name, part.Lines.Count))],
        song.Sections.Sum(part => part.Lines.Sum(line => Chords.Tidy(line.Text, line.Chords).Count)));

    /// <summary>
    /// The name a song made from this file would have, unless the person gives it
    /// another: its title, or if it states none the file's own name without its ending.
    /// </summary>
    public static string NameFor(ParsedSong song, string file) =>
        Tidied(song.Title.Length > 0 ? song.Title : Path.GetFileNameWithoutExtension(file));

    /// <summary>
    /// A name made fit to be a file's (rule 16, and the reference's `importFile`): white
    /// space of any kind is one space, and none at its ends; a slash is a hyphen, and
    /// so is each of the other characters that a file's name cannot have on Windows; it
    /// begins with no full stop (a Mac hides such a file) and ends with none; and a
    /// name with nothing left in it is "Song".
    ///
    /// And one thing more that is Windows' own: a name Windows keeps for a device
    /// (CON, NUL, COM1 and the like) is that device whatever follows a full stop after
    /// it, so in "Nul.x" the full stop is a hyphen too. (Such a name with no full stop
    /// is left, and is given a number as a name in use is: <see cref="FreeName"/>.)
    /// </summary>
    public static string Tidied(string name)
    {
        var tidy = new StringBuilder(name.Length);
        var gap = false;
        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c))
            {
                gap = true;
                continue;
            }
            // (White space before the first character is a space here too, and is
            // taken off below with whatever else is at the ends.)
            if (gap)
            {
                tidy.Append(' ');
            }
            gap = false;
            tidy.Append(CannotBeInAName(c) ? '-' : c);
        }
        var made = tidy.ToString().Trim('.', ' ');
        var stop = made.IndexOf('.', StringComparison.Ordinal);
        if (stop > 0 && IsADevice(made[..stop]))
        {
            made = made[..stop] + "-" + made[(stop + 1)..];
        }
        return made.Length == 0 ? "Song" : made;
    }

    // What a file's name cannot have in it on Windows. (Said here, and not asked of
    // the system, so that a name is tidied the same wherever this is run.)
    private static bool CannotBeInAName(char c) => c < ' ' || c is '/' or (char)0x5C or ':' or '*' or '?' or '"' or '<' or '>' or '|';

    /// <summary>
    /// The name a new song will have in this library: the name, tidied, or that with
    /// " 2", " 3" and so on after it until no file of the library has it.
    /// </summary>
    public static string FreeName(string name, string libraryFolder)
    {
        var tidied = Tidied(name);
        var candidate = tidied;
        for (var n = 2; IsTaken(candidate, libraryFolder); n++)
        {
            candidate = tidied + " " + n.ToString(CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    private static bool IsTaken(string name, string folder)
    {
        var path = Path.Combine(folder, name + Ending);
        return File.Exists(path) || Directory.Exists(path) || IsADevice(name);
    }

    // The names Windows keeps for devices: no file can be called by one, whatever its
    // ending. (CON.pro is the console.) A tidied name has no full stop after one, so
    // such a name with a number after it is no device's.
    private static bool IsADevice(string name)
    {
        var first = name.TrimEnd(' ').ToUpperInvariant();
        return first is "CON" or "PRN" or "AUX" or "NUL"
            || (first.Length == 4 && (first.StartsWith("COM", StringComparison.Ordinal) || first.StartsWith("LPT", StringComparison.Ordinal))
                && (char.IsAsciiDigit(first[3]) || first[3] is (char)0xB9 or (char)0xB2 or (char)0xB3));
    }

    // ---- Making it

    /// <summary>
    /// Makes the song, as a new file in the library. Nothing that is there is changed.
    /// </summary>
    /// <param name="name">The name the person gave it. It is tidied, and given a number if the library has it already.</param>
    /// <exception cref="Refusal">Nothing was made, and its sentence says why.</exception>
    public static MadeSong Make(ParsedSong song, string name, int linesToASlide, ModelSong model, string libraryFolder, Func<bool> proPresenterIsRunning)
    {
        if (proPresenterIsRunning())
        {
            throw Refusal.ProPresenterIsRunningSoNoSongIsMade;
        }
        if (!Paths.IsFolder(libraryFolder))
        {
            throw Refusal.NoLibraryToPutItIn;
        }
        if (song.Sections.Count == 0)
        {
            throw Refusal.NoWordsInIt(name);
        }
        RV.Presentation like;
        try
        {
            like = RV.Presentation.Parser.ParseFrom(model.Bytes.Span);
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.CannotBeRead(model.Name + Ending, Refusal.NoPresentation);
        }
        var each = Math.Max(1, linesToASlide);
        // (A name is taken between its being looked for and its being written only if
        // something else is writing into the library at that moment: then the next
        // number is tried, and nothing that is there is ever written over.)
        for (var tries = 0; tries < 50; tries++)
        {
            var free = FreeName(name, libraryFolder);
            var made = Made(song, free, each, like, model.Name, DateTimeOffset.UtcNow);
            var bytes = made.ToByteArray();
            Check(bytes, song, free, each);
            var path = Path.Combine(libraryFolder, free + Ending);
            try
            {
                if (SongFile.WriteNew(bytes, path))
                {
                    return new MadeSong(path, free, made.CueGroups.Count, made.Cues.Count, Found(song).Chords);
                }
            }
            catch (SongFile.Failure failure)
            {
                throw Refusal.SongNotMade(failure.Message);
            }
        }
        throw Refusal.SongNotMade("no name could be found for it that the library has not.");
    }

    /// <summary>
    /// The presentation: the model, copied, with the song's parts and slides in the
    /// place of its own. What the file's top says is at the top of this file.
    /// </summary>
    /// <param name="modelName">The model's name, for a refusal's sentence. None, for the song the app carries.</param>
    internal static RV.Presentation Made(ParsedSong song, string name, int linesToASlide, RV.Presentation model, string modelName, DateTimeOffset now)
    {
        var slide = SlideToModelOn(model) ?? throw Refusal.NoSlideToModelOn(modelName);
        var made = model.Clone();
        made.Uuid = new RV.UUID { String = OpenSong.NewId(model.Uuid?.String ?? "") };
        made.Name = name;
        made.LastModifiedDate = new RV.Timestamp { Seconds = now.ToUnixTimeSeconds() };

        // What is the model song's own (3, above).
        made.LastDateUsed = null;
        made.Notes = "";
        made.ChordChart = Emptied(made.ChordChart);
        made.SelectedArrangement = null;
        made.Arrangements.Clear();
        made.BibleReference = null;
        made.Timeline = made.Timeline is null ? null : new RV.Presentation.Types.Timeline();
        made.MultiTracksLicensing = null;
        made.MusicKey = "";
        made.ClearSlideShow();

        // Rule 14. (The CCLI block is made anew, so that nothing of the model song's
        // is left in it; only whether it is to be shown is kept.)
        var saysWhatItIs = song.Title.Length > 0 || song.Artist.Length > 0 || song.Copyright.Length > 0 || song.Ccli.Length > 0;
        if (made.Ccli is not null || saysWhatItIs)
        {
            var ccli = new RV.Presentation.Types.CCLI
            {
                Display = made.Ccli?.Display ?? false,
                SongTitle = song.Title,
                ArtistCredits = song.Artist,
                Publisher = song.Copyright,
            };
            if (uint.TryParse(song.Ccli, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                ccli.SongNumber = number;
            }
            made.Ccli = ccli;
        }
        var key = Chords.KeyNumber(song.Key);
        made.Music = key < 0 ? null : new RV.Presentation.Types.Music { Original = KeyOf(key, song.Key), User = KeyOf(key, song.Key) };

        // Rules 11 and 13.
        made.CueGroups.Clear();
        made.Cues.Clear();
        var writtenAs = model.Uuid?.String ?? "";
        foreach (var part in song.Sections)
        {
            var group = new RV.Presentation.Types.CueGroup
            {
                // (A part the file gave no name has a group with none, which ProPresenter
                // shows as plain slides: naming it a verse would be a guess. As the
                // reference.)
                Group = new RV.Group { Uuid = new RV.UUID { String = OpenSong.NewId(writtenAs) }, Name = part.Name, HotKey = new RV.HotKey() },
            };
            foreach (var lines in Chords.SlidesOf(part, linesToASlide))
            {
                var (words, chords) = WordsAndChords(lines);
                var cue = Copied(slide.Cue);
                GiveWords(cue.Actions[0].Slide.Presentation.BaseSlide.Elements, slide.WordsBox, words, chords, modelName);
                made.Cues.Add(cue);
                group.CueIdentifiers.Add(new RV.UUID { String = cue.Uuid.String });
            }
            made.CueGroups.Add(group);
        }
        return made;
    }

    // The words of a slide, its lines parted by line breaks, and its chords at their
    // places along the whole of them (as the reference's `build`).
    private static (string Words, List<Chord> Chords) WordsAndChords(IReadOnlyList<Line> lines)
    {
        var chords = new List<Chord>();
        var start = 0;
        foreach (var line in lines)
        {
            chords.AddRange(line.Chords.Select(chord => chord with { At = start + chord.At }));
            start += line.Text.Length + 1;
        }
        return (string.Join(Words.LineBreak, lines.Select(line => line.Text)), chords);
    }

    private static RV.MusicKeyScale KeyOf(int number, string key) => new()
    {
        MusicKey = (RV.MusicKeyScale.Types.MusicKey)number,
        MusicScale = Chords.KeyIsMinor(key) ? RV.MusicKeyScale.Types.MusicScale.Minor : RV.MusicKeyScale.Types.MusicScale.Major,
    };

    // A chord chart that names no file: as ProPresenter writes one for a song that has
    // none, which says only what kind of computer wrote it.
    private static RV.URL? Emptied(RV.URL? chart) => chart is null ? null : new RV.URL { Platform = chart.Platform };

    // ---- The slide that is copied

    // The model's slide as it is copied: its cue, with nothing in it but its slide
    // (2, above), and which of the slide's elements takes the words.
    private sealed record ModelSlide(RV.Cue Cue, int WordsBox);

    private static ModelSlide? SlideToModelOn(RV.Presentation model)
    {
        var rows = Reading.SlideRows(model);
        foreach (var withWordsToRead in (bool[])[true, false])
        {
            foreach (var row in rows)
            {
                var elements = Reading.ElementsOf(row, model);
                for (var i = 0; i < elements.Count; i++)
                {
                    if (CanBeGivenWords(elements[i], withWordsToRead))
                    {
                        return new ModelSlide(AsItLooks(model.Cues[row.Cue], row.Action), i);
                    }
                }
            }
        }
        return null;
    }

    // Whether a text box can be the one a slide's words are put into: it shows words of
    // its own, it has words to read (or, failing any such box in the song, a line of
    // chords alone), and its RTF has an opening that words can be written after.
    private static bool CanBeGivenWords(RV.Slide.Types.Element element, bool withWordsToRead)
    {
        if (!Reading.HasText(element) || !Reading.ShowsItsOwnWords(element))
        {
            return false;
        }
        var text = element.Element_.Text;
        var words = Reading.WordsOf(text);
        return (withWordsToRead ? HasWordsToRead(words) : words.Length > 0) && RtfWords.WithWords(text.RtfData.Span, "A") is not null;
    }

    // Something other than white space and the characters a chord with no words hangs on.
    private static bool HasWordsToRead(string words) =>
        !Words.IsBlank(words.Replace(Words.ZeroWidthSpace, ' ').Replace(Words.EmQuad, ' '));

    // A cue with nothing in it but its slide, which does nothing but be shown (2, above).
    private static RV.Cue AsItLooks(RV.Cue cue, int slideAction)
    {
        var action = cue.Actions[slideAction].Clone();
        var looks = cue.Clone();
        looks.Actions.Clear();
        looks.Actions.Add(action);
        looks.Name = "";
        looks.CompletionTargetType = RV.Cue.Types.CompletionTargetType.None;
        looks.CompletionTargetUuid = null;
        looks.CompletionActionType = RV.Cue.Types.CompletionActionType.First;
        looks.CompletionActionUuid = null;
        looks.CompletionTime = 0;
        looks.TriggerTime = null;
        looks.HotKey = looks.HotKey is null ? null : new RV.HotKey();
        looks.PendingImports.Clear();
        action.DelayTime = 0;
        if (action.Label is { } label)
        {
            // (As ProPresenter writes a slide that has no label: one with no words, and
            // a colour that is none.)
            action.Label = new RV.Action.Types.Label { Color = label.Color is null ? null : new RV.Color() };
        }
        var shown = action.Slide.Presentation;
        shown.Notes = null;
        shown.ChordChart = Emptied(shown.ChordChart);
        return looks;
    }

    // A copy of the model's slide with new ids (6, above).
    private static RV.Cue Copied(RV.Cue model)
    {
        var cue = model.Clone();
        var renewed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Renew(RV.UUID? id)
        {
            if (id is { String.Length: > 0 } && !renewed.ContainsKey(id.String))
            {
                renewed[id.String] = OpenSong.NewId(id.String);
            }
        }

        var writtenAs = cue.Uuid?.String ?? "";
        Renew(cue.Uuid);
        var action = cue.Actions[0];
        Renew(action.Uuid);
        var slide = action.Slide.Presentation.BaseSlide;
        Renew(slide.Uuid);
        foreach (var element in slide.Elements)
        {
            Renew(element.Element_?.Uuid);
            Renew(element.BuildIn?.Uuid);
            Renew(element.BuildOut?.Uuid);
            foreach (var build in element.ChildBuilds)
            {
                Renew(build.Uuid);
            }
        }
        EachId(cue, id =>
        {
            if (renewed.TryGetValue(id.String, out var now))
            {
                id.String = now;
            }
        });
        // (A cue must have an id for its group to name it by. One whose model had none
        // is given one.)
        if (cue.Uuid is not { String.Length: > 0 })
        {
            cue.Uuid = new RV.UUID { String = OpenSong.NewId(writtenAs) };
        }
        return cue;
    }

    // Every id inside a message, wherever it is: the message is gone through field by
    // field, as its own description of itself lists them.
    private static void EachId(IMessage message, Action<RV.UUID> visit)
    {
        if (message is RV.UUID id)
        {
            visit(id);
            return;
        }
        foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsMap)
            {
                foreach (var value in ((IDictionary)field.Accessor.GetValue(message)).Values)
                {
                    if (value is IMessage inside)
                    {
                        EachId(inside, visit);
                    }
                }
            }
            else if (field.FieldType is not (FieldType.Message or FieldType.Group))
            {
                continue;
            }
            else if (field.IsRepeated)
            {
                foreach (var item in (IEnumerable)field.Accessor.GetValue(message))
                {
                    if (item is IMessage inside)
                    {
                        EachId(inside, visit);
                    }
                }
            }
            else if (field.Accessor.GetValue(message) is IMessage inside)
            {
                EachId(inside, visit);
            }
        }
    }

    // ---- The words and chords of a slide (rule 12; 4 and 5, above)

    private static void GiveWords(IList<RV.Slide.Types.Element> elements, int wordsBox, string words, List<Chord> chords, string modelName)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            if (!Reading.HasText(elements[i]) || !Reading.ShowsItsOwnWords(elements[i]))
            {
                continue;
            }
            var text = elements[i].Element_.Text;
            if (i == wordsBox)
            {
                text.RtfData = ByteString.CopyFrom(RtfWords.WithWords(text.RtfData.Span, words) ?? throw Refusal.WordsCannotBeWritten(modelName));
                if (text.Attributes is { } attributes)
                {
                    var whole = attributes.CustomAttributes
                        .Where(said => !Reading.IsAChord(said) && (said.Range?.Start ?? 0) <= 0 && (said.Range?.End ?? 0) > 0)
                        .Select(said =>
                        {
                            var stretched = said.Clone();
                            stretched.Range = new RV.IntRange { Start = 0, End = words.Length };
                            return stretched;
                        })
                        .ToList();
                    attributes.CustomAttributes.Clear();
                    attributes.CustomAttributes.AddRange(whole);
                }
                Writing.WriteChords(chords, text);
                continue;
            }
            // Another text box of the slide: the model song's words in it are not the
            // new song's, nor are its chords.
            if (!Words.IsBlank(Reading.WordsOf(text)))
            {
                text.RtfData = ByteString.CopyFrom(RtfWords.WithWords(text.RtfData.Span, "") ?? throw Refusal.WordsCannotBeWritten(modelName));
                text.Attributes?.CustomAttributes.Clear();
            }
            else if (text.Attributes is { } attributes)
            {
                var others = attributes.CustomAttributes.Where(said => !Reading.IsAChord(said)).ToList();
                if (others.Count != attributes.CustomAttributes.Count)
                {
                    attributes.CustomAttributes.Clear();
                    attributes.CustomAttributes.AddRange(others);
                }
            }
        }
    }

    // ---- What is checked before anything is written

    // The bytes that would be written are read as any song's are, and must be the song
    // that was asked for: its name and key, a group for each part, each slide with one
    // text box that is shown, with the slide's words and its chords; and no id twice.
    private static void Check(byte[] bytes, ParsedSong song, string name, int linesToASlide)
    {
        RV.Presentation made;
        try
        {
            made = RV.Presentation.Parser.ParseFrom(bytes);
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.SongNotMade(NotAsMeant);
        }
        var rows = Reading.SlideRows(made);
        var slide = 0;
        var key = Chords.KeyNumber(song.Key);
        var good = string.Equals(made.Name, name, StringComparison.Ordinal)
            && string.Equals(made.Music is null ? "" : Reading.KeyName(made.Music.Original), key < 0 ? "" : Chords.KeyName(key, Chords.KeyIsMinor(song.Key)), StringComparison.Ordinal)
            && string.Equals(made.Music is null ? "" : Reading.KeyName(made.Music.User), key < 0 ? "" : Chords.KeyName(key, Chords.KeyIsMinor(song.Key)), StringComparison.Ordinal)
            && made.CueGroups.Count == song.Sections.Count
            && made.Cues.Select(cue => cue.Uuid?.String ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() == made.Cues.Count;
        foreach (var part in song.Sections)
        {
            var first = true;
            foreach (var lines in Chords.SlidesOf(part, linesToASlide))
            {
                if (!good || slide >= rows.Count)
                {
                    throw Refusal.SongNotMade(NotAsMeant);
                }
                var (words, chords) = WordsAndChords(lines);
                var boxes = Reading.TextBoxes(slide, made, rows);
                good = boxes.Count == 1
                    && !boxes[0].Blank
                    && string.Equals(boxes[0].Group, part.Name, StringComparison.Ordinal)
                    && boxes[0].StartsItsGroup == first
                    && string.Equals(boxes[0].Words, words, StringComparison.Ordinal)
                    && boxes[0].Chords.SequenceEqual(Chords.Tidy(words, chords));
                first = false;
                slide++;
            }
        }
        if (!good || slide != rows.Count || rows.Count != made.Cues.Count)
        {
            throw Refusal.SongNotMade(NotAsMeant);
        }
    }

    private const string NotAsMeant = "what was made did not read back as the song that was asked for.";
}
