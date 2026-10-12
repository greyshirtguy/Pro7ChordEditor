// What the app says of itself: its name, its version, what it is, and whose
// (spec/11-plan.md, milestone M10, "the About text").
//
// THE WORDS ARE A PROPOSAL, for the maintainer to read and for the Mac to have the
// same: no page gives them. They are the ones the Mac's read-me for its early build
// already says of the app (mac/dmg/Read Me First.txt, "What it is"), which are
// AGENTS.md's own: what it does, that it is one person's hobby that nobody supports,
// and that it is not ProPresenter. They promise nothing and call the app nothing but
// what it is.
//
// The version is the build's own (<Version> in Directory.Build.props), which .NET
// writes into each library with, after a plus sign, the commit it was built from. Both
// are said: the number, and enough of the commit to tell one build from another.
//
// THE WINDOW'S TITLE is made here too: the app's name and, after it in square
// brackets, the number (spec/06-library.md, rule 6d: "Pro7 Chord Editor [0.2]"). The
// window only shows it (LibraryWindow.xaml.cs). The Mac's is `AppTitle`.

using System.Reflection;

namespace Pro7ChordEditor.AppViews;

public static class AboutTheApp
{
    public const string Name = "Pro7 Chord Editor";

    /// <summary>Where what the app is made from can be read.</summary>
    public const string Address = "https://github.com/greyshirtguy/Pro7ChordEditor";

    private static readonly string Apostrophe = ((char)0x2019).ToString();

    /// <summary>What the build says it is: "0.2+15ac783999e3...", or nothing if it does not say.</summary>
    internal static string Written { get; } =
        typeof(AboutTheApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

    /// <summary>The version's number as it is written, "0.2"; or nothing, if the build says none.</summary>
    public static string Number => NumberOf(Written);

    /// <summary>"Version 0.2".</summary>
    public static string Version => VersionOf(Written);

    /// <summary>What the window's title bar says: "Pro7 Chord Editor [0.2]".</summary>
    public static string Title => TitleOf(Number);

    /// <summary>"Built from 15ac783 of its source.", or nothing where the build does not say what it was built from.</summary>
    public static string BuiltFrom => BuiltFromOf(Written);

    public static string WhatItIs { get; } =
        "It puts chords on the songs of a ProPresenter library, so that ProPresenter shows them on a stage screen. "
        + "It opens the library where it lies and changes the songs" + Apostrophe + " own files.";

    public static string WhoseItIs { get; } =
        "It is one person" + Apostrophe + "s hobby. Nobody supports it and nothing is promised. "
        + "It is not ProPresenter and has nothing to do with Renewed Vision, who make ProPresenter.";

    public const string WhereItsSourceIs = "What it is made from can be read at";

    internal static string NumberOf(string written)
    {
        var plus = written.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? written : written[..plus];
    }

    internal static string VersionOf(string written) => NumberOf(written) is { Length: > 0 } number ? "Version " + number : "";

    /// <summary>
    /// The window's title for a version: the name and, after it in square brackets, the
    /// number. A build that knows of no version says the name alone (rule 6d).
    /// </summary>
    internal static string TitleOf(string number) => number.Length == 0 ? Name : Name + " [" + number + "]";

    internal static string BuiltFromOf(string written)
    {
        var plus = written.IndexOf('+', StringComparison.Ordinal);
        var commit = plus < 0 ? "" : written[(plus + 1)..];
        // (A commit is named by its first seven characters, as git itself shortens one.
        // What is after the plus sign and is no commit is not said to be one.)
        return commit.Length >= 7 && commit.All(Uri.IsHexDigit) ? "Built from " + commit[..7] + " of its source." : "";
    }
}
