// How the tests of the rules read a vector file and say what went wrong.
//
// spec/02-chord-rules.md, "The vectors": a test runs EVERY case of a file and says, for
// a failure, which file, which case and what was got; and no case is copied into the
// test's code, so that when the vectors are made again the tests are held to the new
// ones without a line changing. So each test here walks a whole list of a file, hands
// each case to `Held`, and asks at the end whether all of them were as expected. One
// wrong case does not hide the next: all that are wrong are counted, and the first few
// are shown.
//
// Text is compared unit by unit, never by how it prints, and is shown with every
// character outside plain ASCII written as its number (a backslash, a "u" and four
// digits): the two characters these rules turn on, the zero-width space and the em
// quad, cannot be seen at all, and two strings can print alike and not be the same.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

/// <summary>What a vector file's cases came to: every one of them tried, and what was wrong said.</summary>
internal sealed class Held(string file)
{
    private readonly List<string> wrong = [];
    private int tried;

    /// <summary>A vector file, read.</summary>
    internal static JsonDocument Read(string file) => JsonDocument.Parse(TestData.Vector(file));

    /// <summary>One answer of one case: what was got, and what the file expects.</summary>
    internal void Case(string which, string got, string expected)
    {
        tried++;
        if (!string.Equals(got, expected, StringComparison.Ordinal))
        {
            wrong.Add($"{which}\n      got      {Show(got)}\n      expected {Show(expected)}");
        }
    }

    internal void Case(string which, bool got, bool expected) => Case(which, got ? "true" : "false", expected ? "true" : "false");

    internal void Case(string which, int got, int expected) =>
        Case(which, got.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture));

    internal void Case(string which, IEnumerable<string> got, IEnumerable<string> expected) => Case(which, Listed(got), Listed(expected));

    internal void Case(string which, IEnumerable<Chord> got, IEnumerable<Chord> expected) => Case(which, Listed(got), Listed(expected));

    /// <summary>
    /// Fails if any case was not as expected, saying how many and showing the first
    /// few; and if there were no cases at all, since a file that tried nothing has held
    /// nothing.
    /// </summary>
    internal void Done()
    {
        Assert.True(tried > 0, $"{file}: no cases were found to try");
        Assert.True(
            wrong.Count == 0,
            $"{file}: {wrong.Count} of {tried} answers are not what the file expects. The first:\n  " + string.Join("\n  ", wrong.Take(5)));
    }

    // ---- A case's fields, read

    internal static string Text(JsonElement of, string name) => of.GetProperty(name).GetString()!;

    internal static IReadOnlyList<string> Texts(JsonElement of, string name) =>
        of.GetProperty(name).EnumerateArray().Select(one => one.GetString()!).ToList();

    /// <summary>A list of chords, each written as its place and its name.</summary>
    internal static IReadOnlyList<Chord> Chords(JsonElement of, string name) =>
        of.GetProperty(name).EnumerateArray().Select(one => new Chord(one.GetProperty("at").GetInt32(), one.GetProperty("name").GetString()!)).ToList();

    // ---- Shown

    /// <summary>Text with every character outside plain ASCII as its number, in quotes.</summary>
    internal static string Show(string text)
    {
        var shown = new StringBuilder("\"");
        foreach (var c in text)
        {
            if (c == '\n')
            {
                shown.Append("\\n");
            }
            else if (c < ' ' || c > '~')
            {
                shown.Append('\\').Append('u').Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                shown.Append(c);
            }
        }
        return shown.Append('"').ToString();
    }

    // One piece of text for a list, such that two lists are the same only if their
    // pieces are, in order: each given its length first, so that no name can be taken
    // for two.
    private static string Listed(IEnumerable<string> list) =>
        string.Join(" ", list.Select(one => one.Length.ToString(CultureInfo.InvariantCulture) + ":" + one));

    private static string Listed(IEnumerable<Chord> chords) =>
        Listed(chords.Select(chord => chord.At.ToString(CultureInfo.InvariantCulture) + "=" + chord.Name));
}
