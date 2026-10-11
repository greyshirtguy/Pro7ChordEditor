// The system's own colour picker, asked for a colour (spec/06-library.md, "Settings":
// each of a chord's two colours "is chosen with the system's own colour picker").
//
// WPF has no door to it of its own. The other part of .NET for windows has, and to
// take that on for one dialog would be to carry a second way of making windows in the
// app. So the system is asked directly: one function of comdlg32, which has been in
// Windows as long as there has been a colour to choose.
//
// The picker has sixteen places of its own for colours a person mixes. What is put in
// them is kept while the app runs, so that a colour mixed for the sheet can be picked
// again for the text; it is not kept from one start to the next.
//
// A test cannot answer a dialog. So the view that asks does it through a function it
// can be handed another of (ChordColourSettings.xaml.cs), and what is held here by a
// test is only how a colour is written for the system and read back.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

internal static class ColourPicker
{
    // The picker opens with the colour it is given, with its mixing part open, and
    // lets any colour be chosen.
    private const int StartWithTheColourGiven = 0x1;
    private const int MixingPartOpen = 0x2;
    private const int AnyColour = 0x100;

    // The colours a person has mixed, as the system writes them: white until then.
    private static readonly int[] Mixed = [.. Enumerable.Repeat(0x00FFFFFF, 16)];

    /// <summary>
    /// Asks the person for a colour, over a window, starting from one. Nothing, if they
    /// chose none.
    /// </summary>
    public static Color? Ask(Window? owner, Color from)
    {
        var mixed = Marshal.AllocCoTaskMem(Mixed.Length * sizeof(int));
        try
        {
            Marshal.Copy(Mixed, 0, mixed, Mixed.Length);
            var asked = new Asked
            {
                Size = Marshal.SizeOf<Asked>(),
                Owner = owner is null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle,
                Colour = AsTheSystemWrites(from),
                Mixed = mixed,
                How = StartWithTheColourGiven | MixingPartOpen | AnyColour,
            };
            if (!ChooseColor(ref asked))
            {
                return null;
            }
            Marshal.Copy(mixed, Mixed, 0, Mixed.Length);
            return FromTheSystem(asked.Colour);
        }
        finally
        {
            Marshal.FreeCoTaskMem(mixed);
        }
    }

    /// <summary>A colour as the system writes one: its blue, its green and its red in one number, the red lowest.</summary>
    internal static int AsTheSystemWrites(Color colour) => colour.R | (colour.G << 8) | (colour.B << 16);

    /// <summary>A colour the system wrote, as one to draw with.</summary>
    internal static Color FromTheSystem(int written) =>
        Color.FromRgb((byte)(written & 0xFF), (byte)((written >> 8) & 0xFF), (byte)((written >> 16) & 0xFF));

    // What the function is handed, laid out as the system has it (CHOOSECOLORW).
    [StructLayout(LayoutKind.Sequential)]
    private struct Asked
    {
        public int Size;
        public IntPtr Owner;
        public IntPtr Instance;
        public int Colour;
        public IntPtr Mixed;
        public int How;
        public IntPtr Data;
        public IntPtr Hook;
        public IntPtr Template;
    }

    [DllImport("comdlg32.dll", EntryPoint = "ChooseColorW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseColor(ref Asked asked);
}
