// What the window shows at milestone M0: that there is nothing to do yet, and what the
// app's reader of ProPresenter's files was made from.
//
// It is here to be thrown away at M3, when the library takes its place. Until then it
// does one useful thing: it names something from ProFormat and imports Song, so that
// the app is built against all three layers of the package, and a mistake in how they
// are joined to the Xcode project shows now.
//
// The words say plainly what is so. They promise nothing (AGENTS.md).

import ProFormat
import Song
import SwiftUI

struct NothingYetView: View {
    /// "21.4" of "21.4,352583705": the version as a person would say it.
    private var proPresenterVersion: String {
        String(MadeFrom.proPresenterVersion.prefix { $0 != "," })
    }

    var body: some View {
        VStack(spacing: 12) {
            Text("Pro7 Chord Editor")
                .font(.title)
            Text("Nothing can be opened yet.")
            Text("This build only shows that the parts of the app are joined. Its reader of ProPresenter's files was made from ProPresenter \(proPresenterVersion).")
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(40)
        .frame(minWidth: 480, minHeight: 220)
    }
}
