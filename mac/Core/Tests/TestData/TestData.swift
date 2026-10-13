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

    /// The folders of `testdata/files` whose presentations the tests make their
    /// libraries of: eleven files, which many a test counts on being eleven.
    ///
    /// `shaped-as-real-ones` is not among them. Its one song was made afterwards, for
    /// chords with no words, and the tests of that name it and bring it in themselves.
    /// (It was kept out at first because Simple Presenter had not read it. It has
    /// since 2026-10-12, and `everyPresentationPath` has it for whatever is held to
    /// what Simple Presenter read.)
    public static let presentationFolders = ["made-by-simple-presenter", "as-the-old-windows-app-left-them", "made-by-propresenter"]

    /// The folder of the one song made in the shapes real songs have.
    public static let shapedFolder = "shaped-as-real-ones"

    /// The presentations of those folders of `testdata/files`, as "folder/Name.pro",
    /// in order.
    private static func presentations(in folders: [String]) throws -> [String] {
        try folders.flatMap { folder in
            try FileManager.default.contentsOfDirectory(atPath: root.appendingPathComponent("files").appendingPathComponent(folder).path)
                .filter { $0.hasSuffix(".pro") }
                .sorted()
                .map { folder + "/" + $0 }
        }
    }

    /// The presentations the tests make their libraries of, as "folder/Name.pro", in
    /// order: those of `presentationFolders`.
    public static func presentationPaths() throws -> [String] {
        try presentations(in: presentationFolders)
    }

    /// Every presentation of `testdata/files`: those, and after them the one that was
    /// made in the shapes real songs have. All of them are in
    /// `expected/as-read-by-simple-presenter.json`.
    public static func everyPresentationPath() throws -> [String] {
        try presentations(in: presentationFolders + [shapedFolder])
    }

    /// Where a presentation of `testdata/files` is, by its name alone: "Plain Song.pro"
    /// as "made-by-simple-presenter/Plain Song.pro". Nothing if there is none.
    public static func presentationPath(named name: String) throws -> String? {
        try everyPresentationPath().first { $0.split(separator: "/").last.map(String.init) == name }
    }

    /// A file of `testdata/files`, by its path from there: "expected/Amazing Grace.json".
    public static func file(_ path: String) throws -> Data {
        try Data(contentsOf: root.appendingPathComponent("files").appendingPathComponent(path))
    }

    /// A file of `spec`, the folder beside `testdata` that says what the apps are to
    /// do, by its name: "guide.json". Read where a page of it is itself what an app
    /// is held to, as the guide's words are.
    public static func spec(_ name: String) throws -> Data {
        try Data(contentsOf: root.deletingLastPathComponent().appendingPathComponent("spec").appendingPathComponent(name))
    }
}
