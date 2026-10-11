// What a song must be given before it may save anything.
//
// Two things stand between a change and somebody's Sunday library, and neither is the
// song's own to know:
//
//   - whether ProPresenter is running. It has the library open, and what it does with a
//     song that changes under it is not known (spec/06-library.md, rules 16 to 19).
//     Asking the system is milestone M3.
//   - keeping a copy of the file as it was, before the first change to it since it was
//     opened (spec/07-backups.md). Where the copies go is milestone M4.
//
// So the song is handed both, and asks each at the right moment (OpenSong's Save).
// There is no way to make a song that saves without them: one opened with no safeguards
// can be read and nothing else. That is on purpose. It means no test, no half-built
// window and no mistake can write to a file that was not first copied, or while
// ProPresenter has it.
//
// The Mac's is Safeguards.swift.

namespace Pro7ChordEditor.Song;

/// <summary>Why a copy of a file is being kept (spec/07-backups.md, rule 8).</summary>
public enum CopyReason
{
    /// <summary>Before the first change to a file since it was opened.</summary>
    BeforeAChange,

    /// <summary>Before a copy kept earlier is put back in the file's place.</summary>
    BeforePuttingBack,

    /// <summary>Before a mending of what the first Windows editor left.</summary>
    BeforeMending,
}

/// <param name="ProPresenterIsRunning">Whether ProPresenter is running now. Asked before every save.</param>
/// <param name="KeepCopy">
/// Keeps a copy of the file at this path, as it is on disk now, and is told why. Asked
/// before the first save of a song since it was opened, and before a copy is put back.
/// If it throws, the save is refused, and what it threw says why: the app never changes
/// a file it could not first copy.
/// </param>
public sealed record Safeguards(Func<bool> ProPresenterIsRunning, Action<string, CopyReason> KeepCopy);
