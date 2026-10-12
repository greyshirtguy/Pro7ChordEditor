// What the app says of itself (AboutTheApp, and AboutSettings, the settings' fourth
// section). spec/11-plan.md, milestone M10. The words are a proposal, held here word
// for word so that a change to them is a change someone meant.

using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class AboutTests
{
    private static readonly string Apostrophe = ((char)0x2019).ToString();

    [Fact]
    public void TheVersionIsTheBuildsOwnNumberAndWhatItWasBuiltFromIsSaidShort()
    {
        const string written = "0.1.0+15ac783999e3e9d6987a53dcfc1abef64f387a3c";
        Assert.Equal(("Version 0.1.0", "Built from 15ac783 of its source."), (AboutTheApp.VersionOf(written), AboutTheApp.BuiltFromOf(written)));
        // A build that does not say what it was built from says only its number.
        Assert.Equal(("Version 1.2", ""), (AboutTheApp.VersionOf("1.2"), AboutTheApp.BuiltFromOf("1.2")));
        // What is after the plus sign and is no commit is not said to be one.
        Assert.Equal(("Version 1.0", ""), (AboutTheApp.VersionOf("1.0+local"), AboutTheApp.BuiltFromOf("1.0+local")));
        Assert.Equal("", AboutTheApp.BuiltFromOf("1.0+abc12"));
        Assert.Equal("Built from abcdef0 of its source.", AboutTheApp.BuiltFromOf("1.0+abcdef0"));
        // And one that says nothing at all has nothing said of it.
        Assert.Equal(("", ""), (AboutTheApp.VersionOf(""), AboutTheApp.BuiltFromOf("")));

        // This build's own: the number that Directory.Build.props gives every project.
        var props = File.ReadAllText(Path.Combine(TestData.Repository, "windows", "Directory.Build.props"));
        var number = Regex.Match(props, "<Version>([^<]+)</Version>").Groups[1].Value;
        Assert.Matches(@"^\d+\.\d+(\.\d+)?$", number);
        Assert.Equal("Version " + number, AboutTheApp.Version);
        Assert.Matches(@"^(Built from [0-9a-f]{7} of its source\.)?$", AboutTheApp.BuiltFrom);
    }

    [Fact]
    public void TheSettingsSayWhatTheAppIsWhoseItIsAndWhereItsSourceIs() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var about = new AboutSettings();
        Look.Wear(about, dark: true);
        Pictures.LayOut(about, 960, 600);

        Assert.Equal(
            [
                "Pro7 Chord Editor",
                AboutTheApp.Version,
                AboutTheApp.BuiltFrom,
                "It puts chords on the songs of a ProPresenter library, so that ProPresenter shows them on a stage screen. "
                    + "It opens the library where it lies and changes the songs" + Apostrophe + " own files.",
                "It is one person" + Apostrophe + "s hobby. Nobody supports it and nothing is promised. "
                    + "It is not ProPresenter and has nothing to do with Renewed Vision, who make ProPresenter.",
                "What it is made from can be read at https://github.com/greyshirtguy/Pro7ChordEditor",
            ],
            about.Says);
        Assert.Equal(new Uri("https://github.com/greyshirtguy/Pro7ChordEditor"), about.LinksTo);
        Assert.StartsWith("Version ", about.Says[1], StringComparison.Ordinal);

        // It promises nothing, and calls the app nothing but what it is (AGENTS.md).
        var all = string.Join(" ", about.Says);
        foreach (var word in (string[])["product", "help", "guarantee", "warrant", "official"])
        {
            Assert.DoesNotContain(word, all, StringComparison.OrdinalIgnoreCase);
        }
        // Nothing of it is wider than its column, or has no height.
        Assert.All(about.Says.Where(line => line.Length > 0), line => Assert.True(line.Length < 220));
        var column = (FrameworkElement)about.Content;
        Assert.True(column.ActualWidth is > 0 and <= 560 && column.ActualHeight > 120);
        return Task.CompletedTask;
    });
}
