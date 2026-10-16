// What a song must be given before it may save anything.
//
// Two things stand between a change and somebody's Sunday library, and neither is the
// song's own to know:
//
//   - whether ProPresenter is running. It has the library open, and what it does with a
//     song that changes under it is not known (spec/06-library.md, rules 16 to 19).
//     Asking the system is milestone M3. And whether the person has allowed changes
//     while it runs, having been warned (rule 20): a setting, off as it comes.
//   - keeping a copy of the file as it was, before the first change to it since it was
//     opened (spec/07-backups.md). Where the copies go is Copies.swift.
//
// So the song is handed both, and asks each at the right moment (OpenSong.save). There
// is no way to make a song that saves without them: one opened with no safeguards can
// be read and nothing else. That is on purpose. It means no test, no half-built window
// and no mistake can write to a file that was not first copied, or while ProPresenter
// has it unless that was allowed.

import Foundation

public struct Safeguards {
    /// Whether ProPresenter is running now. Asked before every save.
    public var proPresenterIsRunning: () -> Bool
    /// Whether the person has allowed changes to be saved while it runs (spec/06, rule
    /// 20). Asked before every save too, and first. No by default: a song given no
    /// answer saves nothing while ProPresenter runs.
    public var savesWhileProPresenterRuns: () -> Bool
    /// Keeps a copy of the file at this place, as it is on disk now, and is told why.
    /// Asked before the first save of a song since it was opened, and before a copy is
    /// put back. If it throws, the save is refused: the app never changes a file it
    /// could not first copy. (Copies.keep is the real one.)
    public var keepCopy: (URL, CopyReason) throws -> Void

    public init(proPresenterIsRunning: @escaping () -> Bool, savesWhileProPresenterRuns: @escaping () -> Bool = { false },
                keepCopy: @escaping (URL, CopyReason) throws -> Void) {
        self.proPresenterIsRunning = proPresenterIsRunning
        self.savesWhileProPresenterRuns = savesWhileProPresenterRuns
        self.keepCopy = keepCopy
    }
}
