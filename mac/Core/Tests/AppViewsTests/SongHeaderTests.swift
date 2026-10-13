// The header of the song that is open: where its parts go in the width there is
// (SongHeader.swift). The arithmetic is tried here as arithmetic; that the window then
// puts the parts there is seen in the pictures (PictureTests), where the header is
// drawn in a pane of each width.
//
// Why it is arithmetic at all, and not left to SwiftUI to choose an arrangement that
// fits: the head of SongHeader.swift, and mac/NOTES.md, "What made the window slow".

import AppKit
@testable import AppViews
import Testing

@Suite("The song's header: where its parts go")
struct SongHeaderTests {
    typealias Sizes = HeaderArrangement.Sizes

    /// A song with everything: a name, the switch, the way to its earlier versions,
    /// and its keys.
    static let all = Sizes(name: CGSize(width: 200, height: 24), editors: CGSize(width: 230, height: 36),
                           earlier: CGSize(width: 130, height: 24), keys: CGSize(width: 260, height: 22))

    @Test("As few rows as hold everything with the name whole: one where the name, the switch, the earlier versions and the keys fit side by side; two where the name fits beside the switch and the keys beside the earlier versions; else three")
    func howManyRows() {
        let all = Self.all
        // One row: 200, 12, 230, 12, and at the right 130, 12 and 260.
        #expect(all.oneRow == 856 && all.right == 402)
        #expect(HeaderArrangement.rows(in: 856, all) == .one)
        #expect(HeaderArrangement.rows(in: 2000, all) == .one)
        // Two: the name, 12, at least 8, 12, the switch; and under them the keys, 24,
        // the earlier versions.
        #expect(all.twoRows == 462)
        #expect(HeaderArrangement.rows(in: 855, all) == .two)
        #expect(HeaderArrangement.rows(in: 462, all) == .two)
        #expect(HeaderArrangement.rows(in: 461, all) == .three)
        #expect(HeaderArrangement.rows(in: 0, all) == .three)

        // A long name: the rows go by the name whole, so it is three rows sooner,
        // and only there is the name cut short.
        var long = all
        long.name.width = 700
        #expect(long.oneRow == 1356 && long.twoRows == 962)
        #expect(HeaderArrangement.rows(in: 1000, long) == .two)
        #expect(HeaderArrangement.rows(in: 900, long) == .three)

        // A song that was never changed here has no earlier versions.
        var fresh = all
        fresh.earlier = nil
        #expect(fresh.right == 260 && fresh.oneRow == 714 && fresh.twoRows == 462)
        // A song that could not be read has no switch and no keys: there is nothing
        // to put on a second row, so it is one row whatever the width.
        let unread = Sizes(name: CGSize(width: 300, height: 24), editors: nil, earlier: CGSize(width: 130, height: 24), keys: nil)
        #expect(unread.oneRow == 442)
        #expect(HeaderArrangement.rows(in: 500, unread) == .one && HeaderArrangement.rows(in: 300, unread) == .one)
    }

    @Test("One row: the name at the left, the keys at the right with the earlier versions before them, and the switch in the middle of the pane where both sides have room for that; else hard by the wider side. Each part in the middle of the row's height")
    func oneRow() {
        let all = Self.all
        // Wide: both sides fit in half of what the switch leaves, and the switch's
        // middle is the pane's.
        var laid = HeaderArrangement.places(in: 1200, all)
        #expect(laid.height == 36)
        #expect(laid.places[.name] == CGRect(x: 0, y: 6, width: 200, height: 24))
        #expect(laid.places[.editors] == CGRect(x: 485, y: 0, width: 230, height: 36))
        #expect(laid.places[.editors]?.midX == 600)
        #expect(laid.places[.keys] == CGRect(x: 940, y: 7, width: 260, height: 22))
        #expect(laid.places[.earlier] == CGRect(x: 798, y: 6, width: 130, height: 24))
        // Narrower: the right side needs more than half, and the switch stands just
        // before it.
        laid = HeaderArrangement.places(in: 1000, all)
        #expect(laid.places[.editors]?.minX == 356)
        #expect(laid.places[.earlier]?.minX == 598 && laid.places[.keys]?.minX == 740)
        // At the least width that is one row, nothing is over anything.
        laid = HeaderArrangement.places(in: 856, all)
        #expect(laid.places[.name]?.maxX == 200 && laid.places[.editors]?.minX == 212 && laid.places[.editors]?.maxX == 442)
        #expect(laid.places[.earlier]?.minX == 454 && laid.places[.keys]?.maxX == 856)
        // A long name: the switch is just after it.
        var long = all
        long.name.width = 600
        laid = HeaderArrangement.places(in: 1300, long)
        #expect(laid.places[.name]?.width == 600 && laid.places[.editors]?.minX == 612)
    }

    @Test("Two rows: the name and, at the right, the switch; under them the keys, and at the right the earlier versions. Three: the name and the earlier versions; the switch in the middle; the keys. The name is cut short only to leave the part beside it its room")
    func moreRows() {
        let all = Self.all
        var laid = HeaderArrangement.places(in: 600, all)
        #expect(laid.places[.name] == CGRect(x: 0, y: 6, width: 200, height: 24))
        #expect(laid.places[.editors] == CGRect(x: 370, y: 0, width: 230, height: 36))
        #expect(laid.places[.keys] == CGRect(x: 0, y: 45, width: 260, height: 22))
        #expect(laid.places[.earlier] == CGRect(x: 470, y: 44, width: 130, height: 24))
        #expect(laid.height == 68)

        laid = HeaderArrangement.places(in: 400, all)
        #expect(laid.places[.name] == CGRect(x: 0, y: 0, width: 200, height: 24))
        #expect(laid.places[.earlier] == CGRect(x: 270, y: 0, width: 130, height: 24))
        #expect(laid.places[.editors] == CGRect(x: 85, y: 32, width: 230, height: 36))
        #expect(laid.places[.keys] == CGRect(x: 0, y: 76, width: 260, height: 22))
        #expect(laid.height == 98)

        // A long name in three rows: cut short to leave the earlier versions their
        // room and a little between.
        var long = all
        long.name.width = 700
        laid = HeaderArrangement.places(in: 600, long)
        #expect(laid.places[.name]?.width == 450 && laid.places[.earlier]?.minX == 470)
        // With no earlier versions it has the whole row, and no more.
        long.earlier = nil
        laid = HeaderArrangement.places(in: 600, long)
        #expect(laid.places[.name]?.width == 600 && laid.places[.earlier] == nil)
        #expect(laid.places[.editors]?.midX == 300)

        // A song that could not be read, in a pane too narrow for its name: one
        // row, the name cut short.
        let unread = Sizes(name: CGSize(width: 300, height: 24), editors: nil, earlier: CGSize(width: 130, height: 24), keys: nil)
        laid = HeaderArrangement.places(in: 400, unread)
        #expect(laid.places[.name]?.width == 258 && laid.places[.earlier]?.minX == 270)
        #expect(laid.places[.editors] == nil && laid.places[.keys] == nil && laid.height == 24)
        // Narrower than anything fits in: no width is ever less than nothing.
        laid = HeaderArrangement.places(in: 50, all)
        #expect(laid.places.values.allSatisfy { $0.width >= 0 })
    }
}
