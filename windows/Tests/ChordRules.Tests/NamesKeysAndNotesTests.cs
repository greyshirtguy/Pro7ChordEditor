// The rules of a chord's name, of keys, of moving a chord to another key and of the
// four notations, held to their vector files: keys.json, chord-names.json,
// transposed.json, notated.json and shown.json. Every case of each is run
// (Vectors.cs says how they are compared and what is said of one that fails).

using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class NamesKeysAndNotesTests
{
    [Fact]
    public void Keys()
    {
        var held = new Held("keys.json");
        using var vector = Held.Read("keys.json");
        var keys = vector.RootElement;

        foreach (var name in keys.GetProperty("names").EnumerateArray())
        {
            var number = name.GetProperty("number").GetInt32();
            held.Case($"KeyName({number}, major)", Chords.KeyName(number, false), Held.Text(name, "major"));
            held.Case($"KeyName({number}, minor)", Chords.KeyName(number, true), Held.Text(name, "minor"));
        }
        foreach (var name in keys.GetProperty("outOfRange").EnumerateArray())
        {
            var number = name.GetProperty("number").GetInt32();
            held.Case($"KeyName({number}, major)", Chords.KeyName(number, false), Held.Text(name, "major"));
            held.Case($"KeyName({number}, minor)", Chords.KeyName(number, true), Held.Text(name, "minor"));
        }
        foreach (var one in keys.GetProperty("numbers").EnumerateArray())
        {
            var key = Held.Text(one, "key");
            held.Case($"KeyNumber({Held.Show(key)})", Chords.KeyNumber(key), one.GetProperty("number").GetInt32());
            held.Case($"KeyIsMinor({Held.Show(key)})", Chords.KeyIsMinor(key), one.GetProperty("minor").GetBoolean());
        }
        held.Case("MajorKeys()", Chords.MajorKeys(), Held.Texts(keys, "majorKeys"));
        held.Case("MinorKeys()", Chords.MinorKeys(), Held.Texts(keys, "minorKeys"));
        held.Done();
    }

    [Fact]
    public void ChordNames()
    {
        var held = new Held("chord-names.json");
        using var vector = Held.Read("chord-names.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var typed = Held.Text(one, "in");
            var parts = Chords.Parts(typed);
            held.Case($"Parts({Held.Show(typed)}).Valid", parts.Valid, one.GetProperty("valid").GetBoolean());
            held.Case($"Parts({Held.Show(typed)}).Root", parts.Root, Held.Text(one, "root"));
            held.Case($"Parts({Held.Show(typed)}).Quality", parts.Quality, Held.Text(one, "quality"));
            held.Case($"Parts({Held.Show(typed)}).Bass", parts.Bass, Held.Text(one, "bass"));
            held.Case($"Tidied({Held.Show(typed)})", Chords.Tidied(typed), Held.Text(one, "tidied"));
            held.Case($"CouldBecome({Held.Show(typed)})", Chords.CouldBecome(typed), one.GetProperty("couldBecome").GetBoolean());
            held.Case($"IsChord({Held.Show(typed)})", Chords.IsChord(typed), one.GetProperty("isChord").GetBoolean());
        }
        held.Done();
    }

    [Fact]
    public void Transposed()
    {
        var held = new Held("transposed.json");
        using var vector = Held.Read("transposed.json");
        var chords = Held.Texts(vector.RootElement, "chords");

        // Every pair of the forty-two keys, for each of the chords, in their order.
        foreach (var row in vector.RootElement.GetProperty("rows").EnumerateArray())
        {
            var from = Held.Text(row, "from");
            var to = Held.Text(row, "to");
            held.Case($"Transposed(each chord, {from}, {to})", chords.Select(chord => Chords.Transposed(chord, from, to)), Held.Texts(row, "out"));
        }
        // And what is given back for a chord that cannot be read or a key that is no key.
        foreach (var one in vector.RootElement.GetProperty("odd").EnumerateArray())
        {
            var (chord, from, to) = (Held.Text(one, "chord"), Held.Text(one, "from"), Held.Text(one, "to"));
            held.Case($"Transposed({Held.Show(chord)}, {Held.Show(from)}, {Held.Show(to)})", Chords.Transposed(chord, from, to), Held.Text(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void Notated()
    {
        var held = new Held("notated.json");
        using var vector = Held.Read("notated.json");
        var chords = Held.Texts(vector.RootElement, "chords");

        foreach (var row in vector.RootElement.GetProperty("rows").EnumerateArray())
        {
            var key = Held.Text(row, "key");
            var notation = row.GetProperty("notation").GetInt32();
            held.Case($"Notated(each chord, {key}, {notation})", chords.Select(chord => Chords.Notated(chord, key, notation)), Held.Texts(row, "out"));
        }
        // The kinds of chord that numerals write differently.
        foreach (var one in vector.RootElement.GetProperty("kinds").EnumerateArray())
        {
            var (chord, key, notation) = (Held.Text(one, "chord"), Held.Text(one, "key"), one.GetProperty("notation").GetInt32());
            held.Case($"Notated({Held.Show(chord)}, {key}, {notation})", Chords.Notated(chord, key, notation), Held.Text(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void Shown()
    {
        var held = new Held("shown.json");
        using var vector = Held.Read("shown.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var (chord, original, key) = (Held.Text(one, "chord"), Held.Text(one, "originalKey"), Held.Text(one, "key"));
            var notation = one.GetProperty("notation").GetInt32();
            held.Case(
                $"Shown({Held.Show(chord)}, {Held.Show(original)}, {Held.Show(key)}, {notation})",
                Chords.Shown(chord, original, key, notation),
                Held.Text(one, "out"));
        }
        held.Done();
    }
}
