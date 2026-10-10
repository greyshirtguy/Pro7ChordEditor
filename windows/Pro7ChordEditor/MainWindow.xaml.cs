using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Paragraph = System.Windows.Documents.Paragraph;
using System.Windows.Input;
using System.Windows.Media;
using Rv.Data;
using static Rv.Data.Action;
using System.Text.RegularExpressions;
using System.Reflection;
using static Rv.Data.Graphics.Types.Text.Types.Attributes.Types;
using static Rv.Data.MusicKeyScale.Types;
using System.Diagnostics;
using static Rv.Data.Presentation.Types;

namespace Pro7ChordEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Pro7Presentation SelectedPro7Presentation;
        private readonly List<MusicKeyScale> MusicKeyScales;
        private string LogPath;

        Presentation presentation;

        public List<Pro7Library> Pro7Libraries
        {
            set; get;
        }

        public List<Pro7Presentation> Pro7Presentations
        {
            set; get;
        }

        public MainWindow()
        {
            InitializeComponent();
            LogPath = Path.GetDirectoryName(typeof(App).Assembly.Location);
            MainWindow_Window.Title = $"{MainWindow_Window.Title} ({typeof(App).Assembly.GetName().Version})";
            AddLog(MainWindow_Window.Title + " started");

            // Quick and rough way to build combobox list of all keys/scale without proper data binding/MVVM....
            MusicKeyScales = new List<MusicKeyScale>();
            foreach (MusicKey key in Enum.GetValues(typeof(MusicKey)))
            {
                foreach (MusicScale scale in Enum.GetValues(typeof(MusicScale)))
                {
                    // Keep a reference list of MusicKeyScale objects for easy lookup/conversion from selected combobox items...
                    MusicKeyScale musicKeyScale = new MusicKeyScale
                    {
                        MusicKey = key,
                        MusicScale = scale
                    };
                    MusicKeyScales.Add(musicKeyScale);

                    if (scale == MusicScale.Minor)
                    {
                        ComboBoxOriginalKey.Items.Add(key.ToString().Replace("Flat", "b").Replace("Sharp", "#") + " Min");
                        ComboBoxUserKey.Items.Add(key.ToString().Replace("Flat", "b").Replace("Sharp", "#") + " Min");
                    }
                    else
                    {
                        ComboBoxOriginalKey.Items.Add(key.ToString().Replace("Flat", "b").Replace("Sharp", "#"));
                        ComboBoxUserKey.Items.Add(key.ToString().Replace("Flat", "b").Replace("Sharp", "#"));
                    }
                }
            }

            string pro7SystemFolder;  // Default = %USERPROFILE%\Documents\ProPresenter but it can be customised/moved in Preferences...

            // Check if Pro7 system folder has been moved/customised...
            string customPathSettingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RenewedVision\\ProPresenter\\PathSettings.proPaths";
            if (File.Exists(customPathSettingsFilePath))
            {
                try
                {
                    using (var sr = new StreamReader(customPathSettingsFilePath))
                    {
                        pro7SystemFolder = Regex.Match(sr.ReadToEnd().Replace(@"\\", @"\"), @"(?<=Base=).*(?=;)").Value;
                        AddLog("Pro7 System Folder In Custom Location: " + pro7SystemFolder);
                    }
                }
                catch (IOException e)
                {
                    AddLog("Exception " + e.Message + " while trying to read PathSettings.proPaths");
                    MessageBox.Show("Exception " + e.Message + " while trying to read PathSettings.proPaths");
                    return;
                }
            }
            else
            {
                pro7SystemFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "ProPresenter\\");
                AddLog("Assuming pro7SystemFolder=" + pro7SystemFolder);
            }

            // Create list to store all Libraries found 
            Pro7Libraries = new List<Pro7Library>();

            if (Directory.Exists(pro7SystemFolder + "Libraries"))
            {
                foreach (string libraryFolder in Directory.EnumerateDirectories(pro7SystemFolder + "Libraries"))
                {
                    AddLog("Adding library folder: " + libraryFolder);
                    Pro7Libraries.Add(new Pro7Library { Name = Path.GetFileName(libraryFolder), Path = libraryFolder });
                }
            }
            else
            {
                AddLog(pro7SystemFolder + "Libraries not found");
                MessageBox.Show(pro7SystemFolder + "Libraries not found.\n\n**Make sure Pro7 installed before using this app**");
                Close(); // quit app - no library folders found!
            }

            DataContext = this;
        }

        private void AddLog(string logMessage)
        {
            if (!Directory.Exists(LogPath))
                LogPath = Path.GetDirectoryName(typeof(App).Assembly.Location);

            string logFileName = LogPath + @"\Pro7ChordEditor.log";
            try
            {
                if (!File.Exists(logFileName))
                    File.CreateText(logFileName);

                StreamWriter sw = File.AppendText(logFileName);
                sw.WriteLine(DateTime.Now + " - " + logMessage);
                sw.Close();
            }
            catch (Exception)
            {
                // TODO:
            }
        }

        private void LoadPresentation(string presentationPath)
        {
            StackPanel.Children.Clear();
            using (var input = File.OpenRead(presentationPath))
            {
                AddLog("Parsing presentation: " + presentationPath);
                try
                {
                    presentation = Presentation.Parser.ParseFrom(input);
                    if (presentation.MultiTracksLicensing != null)
                    {
                        TextBlock tb = new TextBlock
                        {
                            FontSize = 24,
                            IsEnabled = false,
                            TextWrapping = TextWrapping.Wrap,
                            Text = "This is a MultiTracks document." + Environment.NewLine + "The license agreement between RV & MT does not allow RV to offer editing in Pro7." + Environment.NewLine + Environment.NewLine + "For now, until I learn more...To keep this app safe from any legal issues (or getting on the bad side of any big company), editing MT documents is disabled in this app as well."
                        };
                        StackPanel.Children.Add(tb);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddLog("Exception: " + ex.Message + " during parsing");
                }

                if (presentation.Music != null)
                {
                    string originalKey = presentation.Music.Original.MusicKey.ToString();
                    if (presentation.Music.Original.MusicScale == MusicScale.Minor)
                        originalKey += " Min";
                    ComboBoxOriginalKey.SelectedIndex = ComboBoxOriginalKey.Items.IndexOf(originalKey);

                    string userKey = presentation.Music.User.MusicKey.ToString();
                    if (presentation.Music.User.MusicScale == MusicScale.Minor)
                        userKey += " Min";
                    ComboBoxUserKey.SelectedIndex = ComboBoxUserKey.Items.IndexOf(userKey);
                }

                foreach (Cue cue in presentation.Cues)
                {
                    AddLog("Processing Cue: " + cue.Name);
                    foreach (Rv.Data.Action action in cue.Actions)
                    {
                        if (action.ActionTypeDataCase == ActionTypeDataOneofCase.Slide)
                        {
                            AddLog("Processing Action: " + action.Type);
                            // Create a new RichTextBox for each slide (and setup basic properties and events to give desired edit behaviour)
                            RichTextBox richTextBox = new RichTextBox
                            {
                                FontSize = 40,
                                Background = Brushes.LightGray,
                                Padding = new Thickness(0, 10, 0, 10)
                            };
                            richTextBox.PreviewKeyDown += RichTextBox_PreviewKeyDown;
                            richTextBox.PreviewTextInput += RichTextBox_PreviewTextInput;

                            // TODO: Consider instead to enumerate and find first slide element with user text
                            // foreach (Rv.Data.Slide.Types.Element slideElement in action.Slide.Presentation.BaseSlide.Elements)
                            if (action.Slide.Presentation.BaseSlide.Elements.Count > 0)
                            {
                                // Load text....
                                Slide.Types.Element slideElement = action.Slide.Presentation.BaseSlide.Elements[0];
                                AddLog("Processing text from first slide element: " + slideElement.Element_.Name);
                                MemoryStream stream = new MemoryStream(Encoding.Default.GetBytes(slideElement.Element_.Text.RtfData.ToStringUtf8()));
                                TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                                range.Load(stream, DataFormats.Rtf);
                                range.ClearAllProperties();

                                // Convert RTF to plain text (with no spacing between lines/paragraphs)
                                string plainText = range.Text;
                                AddLog("Text: " + plainText);
                                richTextBox.Document.Blocks.Clear();
                                richTextBox.AppendText(plainText);
                                foreach (var block in richTextBox.Document.Blocks)
                                {
                                    block.Margin = new Thickness(0);
                                }

                                // I know.. this is a very crude way to make a UI element that is linked to data that is edits...but it's good for a quick demo.
                                richTextBox.Tag = slideElement;

                                // Load Chords into a list...
                                AddLog("Loading Chords into a list");
                                List<CustomAttribute> customAttributes = new List<CustomAttribute>();
                                foreach (CustomAttribute customAttribute in slideElement.Element_.Text.Attributes.CustomAttributes)
                                {
                                    AddLog("Found Custom Attribute: " + customAttribute);
                                    if (customAttribute.AttributeCase == CustomAttribute.AttributeOneofCase.Chord)
                                    {
                                        AddLog("Adding Custom Attribute: " + customAttribute);
                                        customAttributes.Add(customAttribute);
                                    }
                                }

                                // Reverse sort list by chord position
                                AddLog("Reversing list");
                                customAttributes = customAttributes.OrderByDescending(o => o.Range.Start).ToList();

                                // Insert Chords inline in reverse order (this way is easier to use original range.start to decide where to insert)
                                foreach (CustomAttribute customChordAttribute in customAttributes)
                                {
                                    AddLog("Counting text chars to find chord insertion TextPointer for: " + customChordAttribute.Chord + " at index" + customChordAttribute.Range.Start);
                                    // enumerate blocks/paragraphs/runs and count text until we find run that customChordAttribute.Range.Start would lie in...
                                    TextPointer tp = richTextBox.Document.ContentStart;
                                    int counter = 0;
                                    bool found = false;
                                    foreach (Block block in richTextBox.Document.Blocks)
                                    {
                                        if (found)
                                            break;

                                        if (block.GetType() == typeof(Paragraph))
                                        {
                                            Paragraph paragraph = (Paragraph)block;
                                            foreach (Inline inline in paragraph.Inlines)
                                            {
                                                if (found)
                                                    break;

                                                if (inline.GetType() == typeof(Run))
                                                {
                                                    Run runInline = (Run)inline;
                                                    AddLog("Found a Run of text: " + runInline.Text + " (length=" + runInline.Text.Length + ")");
                                                    if (counter + runInline.Text.Length <= customChordAttribute.Range.Start)
                                                    {
                                                        counter += runInline.Text.Length;
                                                        AddLog("Length Counter now: " + counter);
                                                    }
                                                    else
                                                    {
                                                        // This run contains the position of the chord...
                                                        tp = runInline.ContentStart.GetPositionAtOffset(customChordAttribute.Range.Start - counter);
                                                        AddLog("Found text run for chord insertion point: Just before this text=>" + tp.GetTextInRun(LogicalDirection.Forward));
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (!found)
                                        tp = richTextBox.Document.ContentEnd;

                                    // Prepare a new Run to represent a Chord with string "[*]"
                                    Run run = new Run("[" + customChordAttribute.Chord + "]");

                                    // Insert new Bolded Run with new chord
                                    Bold bold = new Bold(run, tp);
                                    TextRange tr = new TextRange(run.ContentStart, run.ContentEnd);
                                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                                }

                                // Add RichTextBox after rtf is loaded and converted to plain text...
                                StackPanel.Children.Add(richTextBox);
                            }
                        }
                    }
                }
            }
        }

        private bool IsCursorInsideChord(object sender)
        {
            RichTextBox rtb = (RichTextBox)sender;
            TextPointer tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
            int indexOfPreviousOpenBracket = tp.GetTextInRun(LogicalDirection.Backward).LastIndexOf("[");
            int indexOfPreviousCloseBracket = tp.GetTextInRun(LogicalDirection.Backward).LastIndexOf("]");
            int indexOfNextOpenBracket = tp.GetTextInRun(LogicalDirection.Forward).LastIndexOf("[");
            int indexOfNextClosedBracket = tp.GetTextInRun(LogicalDirection.Forward).LastIndexOf("]");
            bool firstBracketToLeftIsOpen = indexOfPreviousOpenBracket >= 0 && indexOfPreviousOpenBracket > indexOfPreviousCloseBracket;
            bool firstBracketToRightIsClosed = indexOfNextClosedBracket >= 0 && (indexOfNextClosedBracket < indexOfNextOpenBracket || indexOfNextOpenBracket == -1);
            return firstBracketToLeftIsOpen && firstBracketToRightIsClosed;
        }

        private void RichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            RichTextBox rtb = (RichTextBox)sender;
            TextPointer tp;
            Run run;
            TextRange tr;

            // TODO: Improve this - it's a quite rudimentry and incomplete implementation of restricting input/edits to Chords only (Easy to break/bypass)
            switch (e.Key)
            {
                case Key.A:
                case Key.B:
                case Key.C:
                case Key.D:
                case Key.E:
                case Key.F:
                case Key.G:
                    if (!IsCursorInsideChord(rtb))
                    {
                        // Get current cursor position
                        tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                        // Prepare a new Run to represent a Chord with string "[*]"
                        run = new Run("[" + e.Key.ToString().ToUpper() + "]");
                        // Insert new Bolded Run with new chord
                        _ = new Bold(run, tp);
                        tr = new TextRange(run.ContentStart, run.ContentEnd);
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                        rtb.CaretPosition = run.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward).GetPositionAtOffset(1);
                        // Block any further processing of this keystroke
                        e.Handled = true;
                    }
                    break;
                case Key.OemCloseBrackets:
                case Key.OemOpenBrackets:
                    if (!IsCursorInsideChord(rtb))
                    {
                        // Get current cursor position
                        tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                        // Prepare a new Run to represent a Chord with string "[]"
                        run = new Run("[]");
                        // Insert new Bolded Run (Empty Chord)
                        _ = new Bold(run, tp);
                        tr = new TextRange(run.ContentStart, run.ContentEnd);
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                        rtb.CaretPosition = run.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
                    }
                    // Block any further processing of this keystroke
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                case Key.Home:
                case Key.End:
                    // Allow these movement keys to workinside RichTextControl
                    break;
                default:
                    // Basically block all keystrokes when cursor is outside of a pair of []
                    // Except, allowing DEL and BACKSPACE outside to remove the entire Chord/Run
                    if (!IsCursorInsideChord(sender))
                    {
                        // User is typing OUTSIDE a chord []...

                        // Allow Delete if cursor is just to the left of a Chord |[*] - and remove entire chord (which is in it's own run)
                        if (e.Key == Key.Delete)
                        {
                            tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                            if (tp.GetTextInRun(LogicalDirection.Forward).StartsWith("["))
                            {
                                tp.Paragraph.Inlines.Remove((Run)tp.Parent);
                            }
                        }

                        // Allow BackSpace if cursor is just to the right of a Chord [*]| - and remove entire chord (which is in it's own run)
                        if (e.Key == Key.Back)
                        {
                            tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Backward);
                            if (tp.GetTextInRun(LogicalDirection.Backward).EndsWith("]"))
                            {
                                tp.Paragraph.Inlines.Remove((Run)tp.Parent);
                            }
                        }
                        // Block all other keystrokes.
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void RichTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Force first char in Chord to be uppercase
            RichTextBox rtb = (RichTextBox)sender;
            TextPointer tp = rtb.CaretPosition.GetInsertionPosition(LogicalDirection.Backward);
            if (tp.GetTextInRun(LogicalDirection.Backward).EndsWith("["))
            {
                TextRange range = new TextRange(rtb.CaretPosition, rtb.CaretPosition)
                {
                    Text = e.TextComposition.Text.ToUpper()
                };
                rtb.CaretPosition = range.End.GetInsertionPosition(LogicalDirection.Forward);
                e.Handled = true;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPro7Presentation == null)
            {
                return;
            }

            IntRange rangeCurrentChord;
            IntRange rangeLastChord;
            List<CustomAttribute> customChordAttributes = new List<CustomAttribute>();
            AddLog("***SAVE CLICKED***");

            foreach (RichTextBox richTextBox in StackPanel.Children) // TODO: this is fragile and would break if I ever decide to add any non-RichTextBox children
            {
                int charIndex = 0;  // Current position (count) of characters as we enumerate through all the blocks/runs of text...(To track the position of chords that are found)
                string lastChordFound = "";
                string currentChordFound = "";
                rangeCurrentChord = new IntRange();
                rangeLastChord = new IntRange();
                customChordAttributes.Clear();
                int blockCount = 0;

                // Process block list into custom attributes
                AddLog("Processing richTextBox " + StackPanel.Children.IndexOf(richTextBox));
                foreach (Block block in richTextBox.Document.Blocks)
                {
                    AddLog("Processing block " + ++blockCount);
                    if (block.GetType() == typeof(Paragraph))
                    {
                        int inlineCount = 0;
                        AddLog("Block " + blockCount + " is paragraph, processing inlines.");
                        Paragraph paragraph = (Paragraph)block;
                        foreach (Inline inline in paragraph.Inlines)
                        {
                            AddLog("Processing inline " + ++inlineCount);
                            if (inline.GetType() == typeof(Run))
                            {
                                Run run = (Run)inline;
                                AddLog("Inline " + inlineCount + " is a Run with text: " + run.Text);

                                if (run.Text.StartsWith("["))
                                {
                                    // We have a run that is a chord...

                                    // Capture any current chord into lastchord..
                                    lastChordFound = currentChordFound;
                                    rangeLastChord.Start = rangeCurrentChord.Start;

                                    // Capture current chord
                                    currentChordFound = run.Text.Substring(1, run.Text.Length - 2);
                                    rangeCurrentChord.Start = charIndex;

                                    // End last chord
                                    rangeLastChord.End = charIndex;

                                    // Here is completed chord...
                                    if (lastChordFound != "")
                                    {
                                        CustomAttribute customChordAttribute = new CustomAttribute
                                        {
                                            Range = new IntRange
                                            {
                                                Start = rangeLastChord.Start,
                                                End = rangeLastChord.End,
                                            },
                                            Chord = lastChordFound,
                                        };
                                        customChordAttributes.Add(customChordAttribute);
                                        AddLog("Added chord: " + customChordAttribute);
                                    }
                                }
                                else
                                {
                                    // Increment text counter....
                                    charIndex += run.Text.Length;
                                }

                                AddLog("charIndex=" + charIndex);
                                AddLog("currentChordFound: " + currentChordFound + " (" + rangeCurrentChord.Start + "-" + rangeCurrentChord.End + ")");
                                AddLog("lastChordFound: " + lastChordFound + " (" + rangeLastChord.Start + "-" + rangeLastChord.End + ")");
                            }
                        }
                    }
                }

                // Complete any current chord found when we reach end of text...
                if (currentChordFound != "")
                {
                    rangeCurrentChord.End = charIndex;
                    CustomAttribute customChordAttribute = new CustomAttribute
                    {
                        Range = new IntRange
                        {
                            Start = rangeCurrentChord.Start,
                            End = rangeCurrentChord.End,
                        },
                        Chord = currentChordFound,
                    };
                    customChordAttributes.Add(customChordAttribute);
                    AddLog("Added chord: " + customChordAttribute);
                }

                // Get slideElement (using our crude method of linking)
                Slide.Types.Element slideElement = (Slide.Types.Element)richTextBox.Tag;

                // Clear all existing chord attributes (in reverse order so removal does not affect the loop!)
                AddLog("Clearing existing chords");
                for (int index = slideElement.Element_.Text.Attributes.CustomAttributes.Count - 1; index >= 0; index--)
                {
                    CustomAttribute customAttribute = slideElement.Element_.Text.Attributes.CustomAttributes[index];
                    AddLog("Found existing customAttribute: " + customAttribute);
                    if (customAttribute.Chord.Length > 0)
                    {
                        AddLog("^^ Removed ^^");
                        slideElement.Element_.Text.Attributes.CustomAttributes.Remove(customAttribute); // TODO: HERE IS BUG!!!
                    }
                }

                foreach (var customAttribute1 in customChordAttributes)
                {
                    AddLog("Adding: " + customAttribute1);
                }

                // Add our modified chord attributes
                if (customChordAttributes.Any())
                    slideElement.Element_.Text.Attributes.CustomAttributes.Add(customChordAttributes);

                AddLog("About to save custom attributes:");
                foreach (var customAttribute2 in slideElement.Element_.Text.Attributes.CustomAttributes)
                {
                    AddLog("Saving: " + customAttribute2);
                }
            }

            // Update Original and User MusicKeyScale (matching selected combo-box item to master list of all MUSICKEYSCALES
            if (presentation.Music == null)
            {
                Music music = new Music();
                presentation.Music = music;
            }

            try
            {
                if (ComboBoxOriginalKey.SelectedIndex >= 0)
                    presentation.Music.Original = MusicKeyScales[ComboBoxOriginalKey.SelectedIndex];
                else
                    presentation.Music.Original = MusicKeyScales[0];
                if (ComboBoxUserKey.SelectedIndex >= 0)
                    presentation.Music.User = MusicKeyScales[ComboBoxUserKey.SelectedIndex];
                else
                    presentation.Music.User = MusicKeyScales[0];
            }
            catch (Exception ex)
            {
                AddLog("Exception: " + ex.Message);
            }

            AddLog("Original Key: " + presentation.Music.Original);
            AddLog("User Key: " + presentation.Music.User);

            // Save File
            AddLog("About to write file: " + Path.GetDirectoryName(SelectedPro7Presentation.Path) + "\\" + presentation.Name + " (Chords).pro");
            string newFilePath = Path.GetDirectoryName(SelectedPro7Presentation.Path) + "\\" + presentation.Name + " (Chords).pro";

            if (File.Exists(newFilePath))
            {
                AddLog("Removed existing: " + Path.GetDirectoryName(SelectedPro7Presentation.Path) + "\\" + presentation.Name + " (Chords).pro");
                File.Delete(newFilePath);
            }

            using (var output = File.OpenWrite(newFilePath))
            {
                AddLog("Writing...");
                presentation.WriteTo(output);
                output.Close();
                AddLog("Closed");
            }
            MessageBox.Show(presentation.Name + " (Chords).pro saved.");
        }

        private void ListLibraries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pro7Presentations = new List<Pro7Presentation>();
            Pro7Library selectedPro7Library = (Pro7Library)((ListView)sender).SelectedItem;
            foreach (string presentationPath in Directory.EnumerateFiles(selectedPro7Library.Path, "*.pro"))
            {
                string name = Path.GetFileName(presentationPath);
                if (name.EndsWith(" (Chords).pro"))
                {
                    AddLog("Skipping Chords version: " + name);
                    continue;
                }
                Pro7Presentations.Add(new Pro7Presentation { Name = name, Path = presentationPath });
            }

            ListPresentations.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
        }

        private void ListPresentations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPro7Presentation = (Pro7Presentation)((ListView)sender).SelectedItem;
            if (SelectedPro7Presentation != null)
            {
                string presentationPath = SelectedPro7Presentation.Path;
                string presentationName = SelectedPro7Presentation.Name;
                string chordsVerPath = presentationPath.Substring(0, presentationPath.Length - 4) + " (Chords).pro";
                // Check if there already is a Chords version of the presentation, if so, load that instead
                if (File.Exists(chordsVerPath))
                {
                    presentationPath = chordsVerPath;
                    SelectedPro7Presentation = new Pro7Presentation
                    {
                        Name = presentationName,
                        Path = presentationPath,
                    };
                }
                LoadPresentation(presentationPath);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Warn user - don't run at same time as Pro7
            Process[] processes = Process.GetProcessesByName("ProPresenter");
            if (processes.Length > 0)
                MessageBox.Show(this, "You should not run this at same time as Pro7." + Environment.NewLine + "To avoid file corruptions, quit Pro7");

            MessageBox.Show(this, "Make sure you have a good backup of your Pro7 library documents!");
        }

        /* private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RichTextBox rtb = (RichTextBox)sender;
            if (tb1 != null)
            {
                tb1.Text = "";
                foreach (Inline inlineItem in ((Paragraph)rtb.Document.Blocks.FirstBlock).Inlines)
                {
                    String name = inlineItem.GetType().Name;
                    String runText = "";
                    if (name == "Run")
                    {
                        runText = ((Run)inlineItem).Text;
                    }
                    tb1.Text += name + " - " + runText + "\n";

                }
            }
        } */
    }

    public class Pro7Library
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class Pro7Presentation
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
