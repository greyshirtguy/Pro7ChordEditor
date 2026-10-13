// What the app's own folder (mac/App) must have that a build would not miss: the icon
// the app names as its own.
//
// The app is not this package's to build, and nothing here can look inside a built
// one. But what it is built FROM is in the repository, and a build goes through with
// the icon gone or named wrongly: the app then wears the blank icon macOS gives a
// program that has none, and nothing says so. So it is held here, where every push is
// tested, as well as where an app is made to give out (mac/make-dmg.sh, which looks in
// the built app itself).

import Foundation
import Testing
import TestData

@Suite("The app's own folder")
struct TheAppsFolderTests {
    static let folder = TestData.root.deletingLastPathComponent().appendingPathComponent("mac/App", isDirectory: true)

    /// The pictures an .icns file holds, by the four letters each goes under; nothing
    /// if the bytes are not such a file, whole. (The file is "icns", its own length
    /// in four bytes, and then each picture: four letters, the length of the letters,
    /// the length and the picture together, and the picture.)
    static func pictures(inIcns bytes: [UInt8]) -> [String]? {
        func number(at place: Int) -> Int { bytes[place..<(place + 4)].reduce(0) { $0 << 8 | Int($1) } }
        guard bytes.count >= 8, Array(bytes[0..<4]) == Array("icns".utf8), number(at: 4) == bytes.count else { return nil }
        var names: [String] = []
        var at = 8
        while at < bytes.count {
            guard at + 8 <= bytes.count, number(at: at + 4) >= 8, at + number(at: at + 4) <= bytes.count else { return nil }
            names.append(String(decoding: bytes[at..<(at + 4)], as: UTF8.self))
            at += number(at: at + 4)
        }
        return names
    }

    @Test("The app names its icon, and the icon is beside what names it: an .icns file, whole, with its largest picture and its smallest")
    func theIcon() throws {
        let plist = try PropertyListSerialization.propertyList(from: Data(contentsOf: Self.folder.appendingPathComponent("Info.plist")), format: nil)
        let named = try #require((plist as? [String: Any])?["CFBundleIconFile"] as? String, "Info.plist names no icon")
        #expect(named == "ChordEditor")
        let bytes = [UInt8](try Data(contentsOf: Self.folder.appendingPathComponent(named + ".icns")))
        let pictures = try #require(Self.pictures(inIcns: bytes), "\(named).icns is not a whole .icns file")
        // (ic10 is the picture of 1,024 dots a side, the largest macOS asks for; ic11
        // the one of 32, which is what a fine screen is shown where the icon is
        // smallest.)
        #expect(pictures.contains("ic10") && pictures.contains("ic11"))
    }

    @Test("What is no .icns file is not taken for one: cut short, or with a length that is not its own")
    func whatIsNoIcon() {
        let one: [UInt8] = Array("icns".utf8) + [0, 0, 0, 20] + Array("ic11".utf8) + [0, 0, 0, 12] + [1, 2, 3, 4]
        #expect(Self.pictures(inIcns: one) == ["ic11"])
        #expect(Self.pictures(inIcns: Array(one.dropLast())) == nil)
        #expect(Self.pictures(inIcns: one + [0]) == nil)
        #expect(Self.pictures(inIcns: Array("icon".utf8) + one.dropFirst(4)) == nil)
        // A picture that says it is longer than what is left of the file.
        #expect(Self.pictures(inIcns: Array("icns".utf8) + [0, 0, 0, 20] + Array("ic11".utf8) + [0, 0, 0, 13] + [1, 2, 3, 4]) == nil)
        #expect(Self.pictures(inIcns: []) == nil)
    }
}
