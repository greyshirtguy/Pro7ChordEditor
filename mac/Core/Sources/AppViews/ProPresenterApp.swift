// Whether ProPresenter is running. spec/06-library.md, rules 16 to 20.
//
// It is running if an application is whose bundle identifier is ProPresenter's. Every
// version of it looked at has the same one, so any of them counts. Its helper programs,
// which go on running after it has quit, have identifiers of their own and do not.
//
// This is here and not with the song because asking needs AppKit, which the layers
// below do without. The song is handed it as one of its safeguards.

import AppKit
import Song

public enum ProPresenterApp {
    public static func isRunning() -> Bool {
        !NSRunningApplication.runningApplications(withBundleIdentifier: PlaceFinder.proPresenter).isEmpty
    }
}
