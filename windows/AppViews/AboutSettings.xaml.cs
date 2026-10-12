// The code beside AboutSettings.xaml, which says what the view is. Here it is given
// its words, which are AboutTheApp's, and a click on the address opens it.

using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Pro7ChordEditor.AppViews;

/// <summary>About: what the app says of itself.</summary>
public partial class AboutSettings : UserControl
{
    public AboutSettings()
    {
        InitializeComponent();
        Named.Text = AboutTheApp.Name;
        Version.Text = AboutTheApp.Version;
        BuiltFrom.Text = AboutTheApp.BuiltFrom;
        // (A build that does not say what it was built from has no line for it.)
        BuiltFrom.Visibility = AboutTheApp.BuiltFrom.Length > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        WhatItIs.Text = AboutTheApp.WhatItIs;
        WhoseItIs.Text = AboutTheApp.WhoseItIs;
        WhereItsSourceIs.Text = AboutTheApp.WhereItsSourceIs;
        Address.Text = AboutTheApp.Address;
        Source.NavigateUri = new Uri(AboutTheApp.Address);
    }

    /// <summary>What the view says, top to bottom. For a test.</summary>
    internal IReadOnlyList<string> Says =>
        [Named.Text, Version.Text, BuiltFrom.Visibility == System.Windows.Visibility.Visible ? BuiltFrom.Text : "", WhatItIs.Text, WhoseItIs.Text, WhereItsSourceIs.Text + " " + Address.Text];

    /// <summary>Where the link goes. For a test.</summary>
    internal Uri LinksTo => Source.NavigateUri;

    // The address is opened by whatever the person has set to open one. If nothing
    // will, nothing is done: the address is there to be read.
    private void TheAddressWasClicked(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
        }
        e.Handled = true;
    }
}
