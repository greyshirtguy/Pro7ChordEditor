// The guide: one page that says how chords are put on a song. spec/13-guide.md.
//
// Its words are not written here. They are in `Guide/guide.json`, beside this file,
// which is a copy, the same to the byte, of `spec/guide.json`: the one place the words
// are, so that the Windows app and the Mac app say the same. A test holds the copy to
// the original (GuideTests). It is a copy because the app carries its words inside
// itself, and can carry only what is in its own folder.
//
// What is Windows' own in the words is the names of its keys, Ctrl where the Mac has
// Command. The file has both platforms' names, each under a short name of its own, and
// the words have that short name in curly brackets wherever a key is named: `SaidOn`
// puts the platform's in.
//
// The pictures are beside the words, in the same folder: each a picture of this app's
// own sheet, drawn by the tests (GuidePictureTests) from a test song and kept in the
// repository. The words and the pictures are carried inside the library itself
// (AppViews.csproj), so they are wherever the app is: in the one file it is given out
// as, and under the tests. `GuideView.cs` shows all of it.
//
// No window in this file: what the guide says can be held to the spec by a test that
// draws nothing. The Mac's is Guide.swift.

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pro7ChordEditor.AppViews;

/// <summary>The guide's words, as their file has them or as a platform says them.</summary>
public sealed record Guide(
    string Title,
    Guide.FirstThings First,
    IReadOnlyList<Guide.Section> Sections,
    string Foot,
    IReadOnlyDictionary<string, string> Pictures,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Names)
{
    /// <summary>The platform this app is for, as the file names it.</summary>
    public const string Windows = "windows";

    private static readonly JsonSerializerOptions AsWritten = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    /// <summary>
    /// What stands at the top, before any way of putting a chord on: the few things that
    /// matter more than any of them.
    /// </summary>
    public sealed record FirstThings(string Heading, IReadOnlyList<string> Points);

    /// <summary>One way of doing a thing: a few words that name it, and what it is. Some have a picture under them.</summary>
    public sealed record Item(string Lead, string Text, string? Picture = null);

    /// <param name="Heading">What the section is called.</param>
    /// <param name="Lead">The one sentence of the section that is written large; only the first section has one.</param>
    /// <param name="Picture">The name of its picture, if it has one.</param>
    /// <param name="Paragraphs">Its paragraphs.</param>
    /// <param name="Items">Its ways of doing a thing.</param>
    /// <param name="Sample">A line of ChordPro text, to be shown as the ChordPro editor shows one.</param>
    public sealed record Section(
        string Heading,
        string? Lead = null,
        string? Picture = null,
        IReadOnlyList<string>? Paragraphs = null,
        IReadOnlyList<Item>? Items = null,
        string? Sample = null);

    /// <summary>The names of the pictures the words ask for, in the order they come.</summary>
    public IReadOnlyList<string> PicturesNamed =>
        [.. Sections.SelectMany(section => new[] { section.Picture }.Concat((section.Items ?? []).Select(item => item.Picture))).OfType<string>()];

    /// <summary>Every sentence there is to read, in the order it is read.</summary>
    public IReadOnlyList<string> EverySentence
    {
        get
        {
            List<string> all = [Title, First.Heading, .. First.Points];
            foreach (var section in Sections)
            {
                all.Add(section.Heading);
                if (section.Lead is { } lead)
                {
                    all.Add(lead);
                }
                all.AddRange(section.Paragraphs ?? []);
                foreach (var item in section.Items ?? [])
                {
                    all.Add(item.Lead);
                    all.Add(item.Text);
                }
                if (section.Sample is { } sample)
                {
                    all.Add(sample);
                }
            }
            all.Add(Foot);
            return all;
        }
    }

    /// <summary>
    /// The guide with a platform's own names for the keys put in, wherever a short name
    /// stands in curly brackets. A short name the platform has no name for is left
    /// standing, brackets and all, to be seen and put right; the tests see that there
    /// is none.
    /// </summary>
    public Guide SaidOn(string platform)
    {
        var own = Names.TryGetValue(platform, out var names) ? names : new Dictionary<string, string>();
        string Put(string text) => PutInto(own, text);
        return this with
        {
            Title = Put(Title),
            First = new FirstThings(Put(First.Heading), [.. First.Points.Select(Put)]),
            Sections =
            [
                .. Sections.Select(section => section with
                {
                    Heading = Put(section.Heading),
                    Lead = section.Lead is null ? null : Put(section.Lead),
                    Paragraphs = section.Paragraphs?.Select(Put).ToList(),
                    Items = section.Items?.Select(item => item with { Lead = Put(item.Lead), Text = Put(item.Text) }).ToList(),
                }),
            ],
            Foot = Put(Foot),
            Pictures = Pictures.ToDictionary(pair => pair.Key, pair => Put(pair.Value), StringComparer.Ordinal),
        };
    }

    /// <summary>A text with each "{name}" that <paramref name="names"/> knows made its word.</summary>
    internal static string PutInto(IReadOnlyDictionary<string, string> names, string text)
    {
        var result = new StringBuilder();
        var from = 0;
        while (from < text.Length)
        {
            var open = text.IndexOf('{', from);
            var close = open < 0 ? -1 : text.IndexOf('}', open);
            if (close < 0)
            {
                break;
            }
            result.Append(text, from, open - from);
            var name = text[(open + 1)..close];
            result.Append(names.TryGetValue(name, out var word) ? word : text[open..(close + 1)]);
            from = close + 1;
        }
        return result.Append(text, from, text.Length - from).ToString();
    }

    /// <summary>The guide as its file has it. Nothing if the bytes are not the guide, or a part of it is left out.</summary>
    public static Guide? Read(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var read = JsonSerializer.Deserialize<Guide>(bytes, AsWritten);
            var whole = read is { Title: not null, First: { Heading: not null, Points: not null }, Sections: not null, Foot: not null, Pictures: not null, Names: not null }
                && read.Sections.All(section => section is { Heading: not null } && (section.Items ?? []).All(item => item is { Lead: not null, Text: not null }));
            return whole ? read : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Where the guide's words and pictures are: inside this library, which carries them
/// (AppViews.csproj), under the names "Guide/guide.json" and "Guide/typing.png".
/// </summary>
public static class GuideFiles
{
    public const string WordsName = "guide.json";

    private static readonly Assembly Here = typeof(GuideFiles).Assembly;

    /// <summary>The bytes of one of the guide's files, by its name; nothing if the library has none of that name.</summary>
    public static byte[]? Bytes(string name)
    {
        using var stream = Here.GetManifestResourceStream("Guide/" + name);
        if (stream is null)
        {
            return null;
        }
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>The names of every file of the guide that the library carries, in order.</summary>
    public static IReadOnlyList<string> Names =>
        [.. Here.GetManifestResourceNames().Where(name => name.StartsWith("Guide/", StringComparison.Ordinal)).Select(name => name["Guide/".Length..]).Order(StringComparer.Ordinal)];

    /// <summary>The guide, in Windows' own words; nothing if it cannot be found or read.</summary>
    public static Guide? Guide { get; } = Bytes(WordsName) is { } bytes ? AppViews.Guide.Read(bytes)?.SaidOn(AppViews.Guide.Windows) : null;

    /// <summary>The bytes of the picture of that name, if it is there.</summary>
    public static byte[]? Picture(string name) => Bytes(name + ".png");
}
