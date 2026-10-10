// The lists: a place's libraries, and a library's songs. spec/06-library.md, rules 5, 7
// and 8.
//
// Inside a place, `Libraries` has one folder for each library, named as ProPresenter
// shows it, and each of those has the library's presentations as `.pro` files directly
// in it. A folder chosen by hand need not be laid out so, and is taken for what it has:
// a ProPresenter folder, or a `Libraries` folder, or one library of songs.
//
// Only names are read here, and what the file system says of a file. No file is opened:
// that is Marks.swift, afterwards and in the background.

import Foundation

/// A library: a folder of songs.
public struct Library: Hashable, Sendable, Identifiable {
    public var name: String
    public var folder: URL

    public var id: String { folder.standardizedFileURL.path }

    public init(name: String, folder: URL) {
        self.name = name
        self.folder = folder
    }
}

/// A song's file, as it is listed: its name without the ending, where it is, and its
/// size and time of last change, by which what was worked out of it before is known to
/// be still true.
public struct ListedSong: Hashable, Sendable, Identifiable {
    public var name: String
    public var url: URL
    public var size: Int
    public var modified: Date

    public var id: String { url.standardizedFileURL.path }

    public init(name: String, url: URL, size: Int, modified: Date) {
        self.name = name
        self.url = url
        self.size = size
        self.modified = modified
    }
}

public enum Listing {
    /// Sorted as the system sorts names: "Song 2" before "Song 10", and no regard to capitals.
    static func sorted<T>(_ items: [T], by name: (T) -> String) -> [T] {
        items.sorted { name($0).localizedStandardCompare(name($1)) == .orderedAscending }
    }

    private static func contents(of folder: URL) -> [URL] {
        (try? FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: [.isDirectoryKey, .fileSizeKey, .contentModificationDateKey],
                                                     options: [.skipsHiddenFiles])) ?? []
    }

    private static func folders(in folder: URL) -> [URL] {
        contents(of: folder).filter { (try? $0.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true }
    }

    private static func isSong(_ url: URL) -> Bool {
        url.pathExtension.lowercased() == "pro" && (try? url.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) != true
    }

    /// The libraries of a place, by name.
    ///
    /// A place with `Libraries` in it has the folders of that, whether or not they have
    /// songs in them yet. One without (a folder chosen by hand) is itself a library if it
    /// has songs directly in it, and otherwise has for libraries those of its folders
    /// that have songs.
    public static func libraries(of place: Place) -> [Library] {
        let inside = place.folder.appendingPathComponent("Libraries", isDirectory: true)
        if PlaceFinder.isFolder(inside) {
            return sorted(folders(in: inside).map { Library(name: $0.lastPathComponent, folder: $0) }, by: \.name)
        }
        if contents(of: place.folder).contains(where: isSong) {
            return [Library(name: place.folder.lastPathComponent, folder: place.folder)]
        }
        return sorted(folders(in: place.folder).filter { contents(of: $0).contains(where: isSong) }
            .map { Library(name: $0.lastPathComponent, folder: $0) }, by: \.name)
    }

    /// The songs of a library: its `.pro` files, by name.
    public static func songs(of library: Library) -> [ListedSong] {
        sorted(contents(of: library.folder).filter(isSong).map { url in
            let values = try? url.resourceValues(forKeys: [.fileSizeKey, .contentModificationDateKey])
            return ListedSong(name: url.deletingPathExtension().lastPathComponent, url: url,
                            size: values?.fileSize ?? 0, modified: values?.contentModificationDate ?? .distantPast)
        }, by: \.name)
    }

    /// The songs whose names have what was typed in them, capitals and accents apart.
    /// Nothing typed is every song.
    public static func narrowed(_ songs: [ListedSong], to typed: String) -> [ListedSong] {
        let wanted = typed.trimmingCharacters(in: .whitespaces)
        guard !wanted.isEmpty else { return songs }
        return songs.filter { $0.name.range(of: wanted, options: [.caseInsensitive, .diacriticInsensitive]) != nil }
    }
}
