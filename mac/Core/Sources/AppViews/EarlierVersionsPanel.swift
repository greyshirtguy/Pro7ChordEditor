// Earlier versions of the song that is open: the copies kept of it, the one picked shown
// as it was, and Put This One Back. spec/07-backups.md, rules 15 to 23.
//
//     Earlier versions of "Amazing Grace"
//     +------------------------------------+-------------------------------------+
//     | Today 09:14     before a change    |   the song as it was then, to look  |
//     | Sunday 4 Oct    before a change    |   at and not to change              |
//     | 2 Aug  (the first) before a change |                                     |
//     +------------------------------------+-------------------------------------+
//     | 31 chords, in G.  Now: 34 chords, in G.            [ Put This One Back ] |
//
// A copy is shown as its lines with chords in brackets, as a song is in the window,
// until the chord sheet is built (M6), when it is to be drawn as the sheet draws it.
//
// Putting one back is the first thing in the app that changes a song's file. It is a
// save like any other (LibraryModel.putBackPicked, OpenSong.putBack): refused while
// ProPresenter runs, and never before the file as it is now has been copied, so that
// nothing can be lost by putting the wrong one back.

import Song
import SwiftUI

struct EarlierVersionsPanel: View {
    @Bindable var model: LibraryModel

    private func why(_ copy: KeptCopy) -> String {
        (copy.isTheFirst ? "(the first) " : "") + (copy.why?.rawValue ?? "")
    }

    var body: some View {
        let panel = model.earlier
        VStack(alignment: .leading, spacing: 0) {
            Text("Earlier versions of \u{201C}\(model.shown?.name ?? "")\u{201D}")
                .font(.headline)
                .padding(.horizontal, 16)
                .padding(.vertical, 12)
            Divider()
            HSplitView {
                List(selection: Binding(get: { panel?.pickedID }, set: { model.pick(copy: $0) })) {
                    ForEach(panel?.copies ?? []) { copy in
                        HStack {
                            Text(model.when(copy))
                            Spacer(minLength: 8)
                            Text(why(copy))
                                .foregroundStyle(.secondary)
                        }
                        .tag(Optional(copy.id))
                    }
                }
                .listStyle(.inset)
                .frame(minWidth: 260, idealWidth: 300, maxWidth: 380)

                Group {
                    if let problem = panel?.picked?.problem {
                        Text(problem)
                            .padding(16)
                            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
                    } else {
                        GeometryReader { pane in
                            ScrollView([.vertical, .horizontal]) {
                                SongLines(lines: panel?.picked?.lines ?? [])
                                    .padding(16)
                                    .frame(minWidth: pane.size.width, minHeight: pane.size.height, alignment: .topLeading)
                            }
                        }
                    }
                }
                .frame(minWidth: 340, maxWidth: .infinity, maxHeight: .infinity)
                .background(Color(nsColor: .textBackgroundColor))
            }
            Divider()
            if let question = panel?.question {
                // A copy of another song than the one now here: asked first, with both names.
                HStack(spacing: 10) {
                    Text(question)
                        .fixedSize(horizontal: false, vertical: true)
                    Spacer(minLength: 8)
                    Button("Leave It") { model.pick(copy: panel?.pickedID) }
                    Button("Put It Back") { model.putBackPicked(sure: true) }
                }
                .padding(12)
            } else {
                HStack(spacing: 10) {
                    Text(model.refusal ?? panel?.comparison ?? "")
                        .foregroundStyle(model.refusal == nil ? .secondary : .primary)
                        .fixedSize(horizontal: false, vertical: true)
                    Spacer(minLength: 8)
                    Button("Done") { model.closeEarlierVersions() }
                        .keyboardShortcut(.cancelAction)
                    Button("Put This One Back") { model.putBackPicked() }
                        .disabled(panel?.picked == nil || panel?.picked?.problem != nil)
                        .help("Make the song's file this version again. The song as it is now is kept first, so this can be undone.")
                }
                .padding(12)
            }
        }
        .frame(minWidth: 760, idealWidth: 860, minHeight: 440, idealHeight: 520)
    }
}
