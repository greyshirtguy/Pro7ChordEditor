// The guide: one page that says how chords are put on a song. spec/13-guide.md.
//
// Its words are not written here. They are in `Guide/guide.json`, beside this file,
// which is a copy, the same to the byte, of `spec/guide.json`: the one place the words
// are, so that the Mac app and the Windows app say the same. A test holds the copy to
// the original, as one holds the generated reader of ProPresenter's files to the
// descriptions it was made from. (A copy, because a package can carry only what is
// inside its own folder.)
//
// What is the Mac's own in the words is the names of its keys, Command where Windows has
// Ctrl. The file has both platforms' names, each under a short name of its own, and
// the words have that short name in curly brackets wherever a key is named: `said(on:)`
// puts the platform's in.
//
// The pictures are beside the words, in the same folder: each a picture of this app's
// own sheet, drawn by the tests (GuidePictureTests) from a test song and kept in the
// repository. `GuideWindow.swift` shows all of it.
//
// No window in this file, and nothing of AppKit: what the guide says can be held to the
// spec by a test that draws nothing.

import Foundation

public struct Guide: Decodable, Equatable, Sendable {
    /// What stands at the top, before any way of putting a chord on: the few things
    /// that matter more than any of them.
    public struct First: Decodable, Equatable, Sendable {
        public var heading: String
        public var points: [String]
    }

    /// One way of doing a thing: a few words that name it, and what it is. Some have a
    /// picture under them.
    public struct Item: Decodable, Equatable, Sendable {
        public var lead: String
        public var text: String
        public var picture: String?
    }

    public struct Section: Decodable, Equatable, Sendable {
        public var heading: String
        /// The one sentence of the section that is written large; only the first
        /// section has one.
        public var lead: String?
        public var picture: String?
        public var paragraphs: [String]?
        public var items: [Item]?
        /// A line of ChordPro text, to be shown as the ChordPro editor shows one.
        public var sample: String?
    }

    public var title: String
    public var first: First
    public var sections: [Section]
    public var foot: String
    /// What each picture shows, in words, by the picture's name: for whoever cannot
    /// see it, and for whoever draws it.
    public var pictures: [String: String]
    /// Each platform's names for the keys, by the short names the words use.
    public var names: [String: [String: String]]

    /// The platform this app is for, as the file names it.
    public static let mac = "mac"

    /// The names of the pictures the words ask for, in the order they come.
    public var picturesNamed: [String] {
        var named: [String] = []
        for section in sections {
            if let picture = section.picture { named.append(picture) }
            for item in section.items ?? [] {
                if let picture = item.picture { named.append(picture) }
            }
        }
        return named
    }

    /// Every sentence there is to read, in the order it is read.
    public var everySentence: [String] {
        var all: [String] = [title, first.heading]
        all += first.points
        for section in sections {
            all.append(section.heading)
            if let lead = section.lead { all.append(lead) }
            all += section.paragraphs ?? []
            for item in section.items ?? [] {
                all += [item.lead, item.text]
            }
            if let sample = section.sample { all.append(sample) }
        }
        all.append(foot)
        return all
    }

    /// The guide with a platform's own names for the keys put in, wherever a short
    /// name stands in curly brackets. A short name the platform has no name for is
    /// left standing, brackets and all, to be seen and put right; the tests see that
    /// there is none.
    public func said(on platform: String) -> Guide {
        let own = names[platform] ?? [:]
        func put(_ text: String) -> String { Self.put(own, into: text) }
        var said = self
        said.title = put(title)
        said.first.heading = put(first.heading)
        said.first.points = first.points.map(put)
        said.sections = sections.map { section in
            var section = section
            section.heading = put(section.heading)
            section.lead = section.lead.map(put)
            section.paragraphs = section.paragraphs?.map(put)
            section.items = section.items?.map { Item(lead: put($0.lead), text: put($0.text), picture: $0.picture) }
            return section
        }
        said.foot = put(foot)
        said.pictures = pictures.mapValues(put)
        return said
    }

    /// `text` with each "{name}" that `names` knows made its word.
    static func put(_ names: [String: String], into text: String) -> String {
        var result = ""
        var rest = Substring(text)
        while let open = rest.firstIndex(of: "{"), let close = rest[open...].firstIndex(of: "}") {
            let name = String(rest[rest.index(after: open)..<close])
            result += rest[..<open]
            result += names[name] ?? String(rest[open...close])
            rest = rest[rest.index(after: close)...]
        }
        return result + rest
    }

    /// The guide as its file has it. Nothing if the bytes are not the guide.
    public static func read(_ bytes: Data) -> Guide? {
        try? JSONDecoder().decode(Guide.self, from: bytes)
    }
}

/// Where the guide's words and pictures are, in the app as it is built and under the
/// tests.
///
/// They are the package's own resources, which the package manager puts in a bundle
/// named for the package and the library (`Core_AppViews.bundle`), in the app's
/// Resources when Xcode builds the app, and beside the tests when `swift test` builds
/// those. It is looked for here by hand, and not with the `Bundle.module` that the
/// package manager writes: that one stops the whole app if the bundle is not where it
/// expects, and the guide is opened by the app itself at its first start. A guide
/// that cannot be found says so in its window, and nothing else is the worse for it.
public enum GuideFiles {
    /// Only so that the bundle this code is in can be asked for.
    private final class Here {}

    /// The folder that has `guide.json` and the pictures; nothing if it is not found.
    /// Looked for where the app's own resources are, and beside the code this is in,
    /// which under the tests is another place.
    public static let folder: URL? = {
        let code = Bundle(for: Here.self)
        return folder(in: [Bundle.main.resourceURL, code.resourceURL, code.bundleURL.deletingLastPathComponent(), Bundle.main.bundleURL].compactMap { $0 })
    }()

    public static let wordsName = "guide.json"
    static let bundleName = "Core_AppViews.bundle"

    /// The guide's folder, in the first of these places that has the package's bundle
    /// with the guide's words in it.
    static func folder(in places: [URL]) -> URL? {
        for place in places {
            let bundle = place.appendingPathComponent(bundleName, isDirectory: true)
            // (As Xcode lays the bundle out in a Mac app, with its resources in
            // Contents/Resources; and as `swift build` does, with them directly in it.)
            for inside in ["Contents/Resources/Guide", "Guide"] {
                let guide = bundle.appendingPathComponent(inside, isDirectory: true)
                if FileManager.default.fileExists(atPath: guide.appendingPathComponent(wordsName).path) {
                    return guide
                }
            }
        }
        return nil
    }

    /// The guide, in the Mac's own words; nothing if it cannot be found or read.
    public static let guide: Guide? = {
        guard let folder, let bytes = try? Data(contentsOf: folder.appendingPathComponent(wordsName)) else { return nil }
        return Guide.read(bytes)?.said(on: Guide.mac)
    }()

    /// Where the picture of that name is, if it is there.
    public static func picture(_ name: String) -> URL? {
        guard let file = folder?.appendingPathComponent(name + ".png"), FileManager.default.fileExists(atPath: file.path) else { return nil }
        return file
    }
}
