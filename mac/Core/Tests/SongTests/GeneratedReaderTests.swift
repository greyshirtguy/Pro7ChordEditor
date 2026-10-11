// The reader of ProPresenter's files is generated code that is KEPT in the repository
// (mac/Core/generate-proto.sh says why), and so can fall behind the descriptions it was
// made from, which are in the proto/ submodule. This is what notices.
//
// It can only notice where the submodule is checked out: on the machine of whoever
// moves the submodule on, which is where it matters. On GitHub's machine the package
// is built without the submodule, there is nothing to hold the code against, and the
// test says nothing.
//
// (The Windows app generates its reader at every build, from the submodule, and has no
// such test to need: spec/10-platforms.md has both ways. What the two must share is
// the descriptions, and this is the Mac's half of seeing that they do.)

import Foundation
import ProFormat
import Testing
import TestData

@Suite("The generated reader")
struct GeneratedReaderTests {
    @Test("Where the proto/ submodule is checked out, the reader kept in the repository was made from the descriptions that are in it")
    func madeFromWhatIsThere() throws {
        let version = TestData.root.deletingLastPathComponent().appendingPathComponent("proto/autogen-proto/version.txt")
        guard let there = try? String(contentsOf: version, encoding: .utf8) else { return }
        #expect(there.trimmingCharacters(in: .whitespacesAndNewlines) == MadeFrom.proPresenterVersion,
                "proto/ has moved on to the descriptions of another ProPresenter: run mac/Core/generate-proto.sh, and every test after it")
        #expect(MadeFrom.fileCount > 0 && !MadeFrom.protoCommit.isEmpty)
    }
}
