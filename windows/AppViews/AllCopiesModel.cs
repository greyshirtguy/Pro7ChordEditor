// All Copies, in the app's settings: every song that has copies kept, by place and
// library, those whose song is no longer where it was included. From here a copy can be
// looked at, or saved somewhere as a file. spec/07-backups.md, rules 13 and 18.
//
// Nothing here changes a song. Putting a copy back is done from the song itself
// (LibraryModel.PutBackPicked), where the song is open and its safeguards are asked.
//
// This is what the settings show of the copies, with no drawing in it, and is tested
// so. The Mac's is `AllCopiesModel`, in AllCopies.swift.

using System.ComponentModel;
using System.Globalization;
using System.IO;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed class AllCopiesModel(Copies copies) : INotifyPropertyChanged
{
    /// <summary>A song that has copies, and what is known of whether it is still where it was.</summary>
    /// <param name="SongIsStillThere">
    /// Whether the file the newest copy says it is a copy of is there still. Not known
    /// if the copies' index is lost.
    /// </param>
    public sealed record Row(KeptSong Song, IReadOnlyList<KeptCopy> Copies, bool? SongIsStillThere)
    {
        public string Id => Song.Id;

        /// <summary>Under the song's name: its library and place, and that it is gone if it is.</summary>
        public string Where => Song.Library + ", in " + Song.Place + (SongIsStillThere == false ? ". The song is no longer there." : "");
    }

    public static AllCopiesModel OnThisPC() => new(AppData.OnThisPC().Copies());

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>What stands over the lists.</summary>
    public static string WhatTheseAre =>
        "Before this app changes a song" + (char)0x2019 + "s file, it keeps a copy of the file as it was. These are the copies.";

    /// <summary>What stands in place of the lists while there are none.</summary>
    public static string NoneYet => "No copies are kept yet.";

    /// <summary>The folder the copies are in.</summary>
    public string Folder => copies.Folder;

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    public string? PickedSong { get; private set; }

    public string? PickedCopy { get; private set; }

    /// <summary>The copy picked, as its lines, to look at.</summary>
    public LibraryModel.ShownSong? Looked { get; private set; }

    /// <summary>How much room all the copies take, in words: "1.2 MB".</summary>
    public string Size { get; private set; } = "";

    /// <summary>What went wrong with the last saving of a copy as a file; nothing if nothing did.</summary>
    public string? Problem { get; private set; }

    public Row? PickedRow => Rows.FirstOrDefault(row => row.Id == PickedSong);

    public KeptCopy? Copy => PickedRow?.Copies.FirstOrDefault(copy => copy.Id == PickedCopy);

    /// <summary>The line at the foot: what went wrong, or how much room the copies take.</summary>
    public string Notice => Problem ?? (Rows.Count == 0 ? "" : "The copies take " + Size + ".");

    /// <summary>Reads what is kept. Done when the settings are opened.</summary>
    public void Read()
    {
        Rows = [.. copies.Songs().Select(song =>
        {
            var kept = copies.CopiesOf(song);
            var whereItWas = kept.Count > 0 ? kept[0].CopyOf : "";
            return new Row(song, kept, whereItWas.Length == 0 ? null : File.Exists(whereItWas));
        })];
        Size = InWords(Rows.Sum(row => row.Copies.Sum(copy => copy.Size)));
        Problem = null;
        if (PickedRow is null)
        {
            PickSong(Rows.Count > 0 ? Rows[0].Id : null);
        }
        else
        {
            Changed();
        }
    }

    public void PickSong(string? id)
    {
        PickedSong = id;
        PickCopy(PickedRow?.Copies.FirstOrDefault()?.Id);
    }

    public void PickCopy(string? id)
    {
        PickedCopy = id;
        Looked = Copy is { } copy ? LibraryModel.Looked(copy) : null;
        Changed();
    }

    public string When(KeptCopy copy) => copies.When(copy.Made);

    /// <summary>The name a copy is offered to be saved under: the song's, and when the copy was made.</summary>
    public string NameToSaveAs(KeptCopy copy) => Copies.FolderName((PickedRow?.Song.Name ?? "Copy") + " (" + When(copy) + ")") + ".pro";

    /// <summary>
    /// Saves the copy picked somewhere as a file: its bytes, unchanged. Answers with
    /// what went wrong, or nothing; and says it at the foot.
    /// </summary>
    public string? SavePicked(string path)
    {
        if (Copy is not { } copy)
        {
            return null;
        }
        // Written beside where it is to go and then moved there, whole or not at all.
        var beside = path + ".saving";
        try
        {
            File.WriteAllBytes(beside, File.ReadAllBytes(copy.Path));
            File.Move(beside, path, overwrite: true);
            Problem = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            Problem = "The copy could not be saved there: " + exception.Message;
            try
            {
                File.Delete(beside);
            }
            catch (Exception again) when (again is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
            }
        }
        Changed();
        return Problem;
    }

    /// <summary>
    /// So many bytes, in words, as Windows itself counts them: 1024 to the kilobyte.
    /// The same on every PC.
    /// </summary>
    public static string InWords(long bytes)
    {
        const double kilobyte = 1024;
        return bytes switch
        {
            1 => "1 byte",
            < 1024 => bytes.ToString(CultureInfo.InvariantCulture) + " bytes",
            < 1024 * 1024 => Math.Round(bytes / kilobyte).ToString("0", CultureInfo.InvariantCulture) + " KB",
            < 1024L * 1024 * 1024 => (bytes / kilobyte / kilobyte).ToString("0.0", CultureInfo.InvariantCulture) + " MB",
            _ => (bytes / kilobyte / kilobyte / kilobyte).ToString("0.00", CultureInfo.InvariantCulture) + " GB",
        };
    }

    private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
