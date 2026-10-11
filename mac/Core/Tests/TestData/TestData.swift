// Where the tests find testdata/, the folder at the top of the repository that both
// apps' tests are held to (testdata/README.md).
//
// It is found by walking up from this source file, not copied into the package as a
// resource: a package cannot take resources from outside itself, and a copy would be a
// second thing to keep true. So the tests must be run from the repository they were
// built in, which is how `swift test` and Xcode both run them.
//
// Nothing here writes. A test that changes a file works on a copy in a temporary
// folder (`copy(of:)`), never on testdata/ itself and never on anyone's library
// (AGENTS.md, "The person's files").

import Foundation

public enum TestData {
    /// The repository's `testdata` folder.
    public static let root: URL = {
        var folder = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
        while folder.path != "/" {
            let candidate = folder.appendingPathComponent("testdata")
            if FileManager.default.fileExists(atPath: candidate.appendingPathComponent("vectors").path) {
                return candidate
            }
            folder.deleteLastPathComponent()
        }
        fatalError("testdata/vectors was not found above \(#filePath): run the tests from a clone of the repository")
    }()

    /// A file of `testdata/vectors`, by its name: "keys.json".
    public static func vector(_ name: String) throws -> Data {
        try Data(contentsOf: root.appendingPathComponent("vectors").appendingPathComponent(name))
    }

    /// The names of every file of `testdata/vectors`, in order.
    public static func vectorNames() throws -> [String] {
        try FileManager.default.contentsOfDirectory(atPath: root.appendingPathComponent("vectors").path)
            .filter { $0.hasSuffix(".json") }
            .sorted()
    }

    /// A file of `testdata/songs`, by its name: "amazing-grace.cho".
    public static func song(_ name: String) throws -> Data {
        try Data(contentsOf: root.appendingPathComponent("songs").appendingPathComponent(name))
    }

    /// The folders of `testdata/files` that hold presentations: those that Simple
    /// Presenter was asked to read, so that reading one can be held to what it read
    /// (`expected/as-read-by-simple-presenter.json`).
    ///
    /// `shaped-as-real-ones` is not among them. Its one song was made afterwards, for
    /// chords with no words, by a tool that runs anywhere, and Simple Presenter runs
    /// only on the machine it is built on. The tests of chords with no words name it.
    public static let presentationFolders = ["made-by-simple-presenter", "as-the-old-windows-app-left-them", "made-by-propresenter"]

    /// Every presentation of `testdata/files`, as "folder/Name.pro", in order.
    public static func presentationPaths() throws -> [String] {
        try presentationFolders.flatMap { folder in
            try FileManager.default.contentsOfDirectory(atPath: root.appendingPathComponent("files").appendingPathComponent(folder).path)
                .filter { $0.hasSuffix(".pro") }
                .sorted()
                .map { folder + "/" + $0 }
        }
    }

    /// Where a presentation of `testdata/files` is, by its name alone: "Plain Song.pro"
    /// as "made-by-simple-presenter/Plain Song.pro". Nothing if there is none.
    public static func presentationPath(named name: String) throws -> String? {
        try presentationPaths().first { $0.split(separator: "/").last.map(String.init) == name }
    }

    /// A file of `testdata/files`, by its path from there: "expected/Amazing Grace.json".
    public static func file(_ path: String) throws -> Data {
        try Data(contentsOf: root.appendingPathComponent("files").appendingPathComponent(path))
    }
}
