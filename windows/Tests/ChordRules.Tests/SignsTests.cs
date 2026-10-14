// Sharps and flats drawn as signs (Signs.cs), held to signs.json, whose reference is
// tools/signs.py. Every case of both its lists is run (Vectors.cs).
//
// And, mine and not a vector, the one thing about it the sheet leans on: a name drawn is
// as long as the name, in units, and reads back as it was.

using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class SignsTests
{
    [Fact]
    public void WhichSharpsAndFlatsAreDrawnAsSigns()
    {
        var held = new Held("signs.json");
        using var vector = Held.Read("signs.json");
        foreach (var one in vector.RootElement.GetProperty("withSigns").EnumerateArray())
        {
            var name = Held.Text(one, "in");
            held.Case($"WithSigns({Held.Show(name)}), {Held.Text(one, "about")}", Chords.WithSigns(name), Held.Text(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void TheSignsReadBackAsSharpsAndFlats()
    {
        var held = new Held("signs.json");
        using var vector = Held.Read("signs.json");
        foreach (var one in vector.RootElement.GetProperty("withoutSigns").EnumerateArray())
        {
            var text = Held.Text(one, "in");
            held.Case($"WithoutSigns({Held.Show(text)}), {Held.Text(one, "about")}", Chords.WithoutSigns(text), Held.Text(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void ANameDrawnIsAsLongAsTheNameAndReadsBackAsItWas()
    {
        using var vector = Held.Read("signs.json");
        foreach (var one in vector.RootElement.GetProperty("withSigns").EnumerateArray())
        {
            var name = Held.Text(one, "in");
            var shown = Chords.WithSigns(name);
            Assert.Equal(name.Length, shown.Length);
            Assert.Equal(Chords.WithoutSigns(name), Chords.WithoutSigns(shown));
        }
    }
}
