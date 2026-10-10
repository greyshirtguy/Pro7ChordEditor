// Finding where ProPresenter keeps songs as files. spec/06-library.md, "Where
// ProPresenter keeps its songs".
//
// A place is a folder with a `Libraries` folder in it, in which each folder is a library
// and each `.pro` file directly in one is a song. There can be several on one Mac:
//
//   - a WORKSPACE that ProPresenter keeps as a folder. ProPresenter lists these in its
//     own settings (under `userWorkspaces`), each with a name, the folder, and whether
//     it is the one in use. KNOWN of ProPresenter 21 on the maintainer's Mac, where the
//     list is JSON kept as data. How other versions keep it is A GUESS: a string of the
//     same JSON, or a list ready made, are both taken.
//   - the folder ProPresenter's settings name as its own (`applicationShowDirectory`),
//     which is how a ProPresenter from before workspaces says its folder was moved.
//     A GUESS that it means that: on the maintainer's Mac it names a workspace.
//   - `ProPresenter` in the person's Documents folder, where it is unless moved.
//   - any folder the person has chosen by hand: a copy of a library kept elsewhere, or
//     the songs of someone with no ProPresenter on this Mac at all.
//
// ProPresenter's settings are only ever read. Nothing of this app's is put in
// ProPresenter's folders, and nothing here makes a folder or a file.
//
// The finder is handed what it needs (a way to read one of ProPresenter's settings,
// where Documents is, what was chosen), so that tests can hand it made-up ones.
// `onThisMac` hands it the real ones.

import Foundation

/// A place where songs are kept as files: a folder with `Libraries` in it, or a folder
/// chosen by hand.
public struct Place: Hashable, Sendable, Identifiable {
    public enum Kind: String, Sendable {
        /// A workspace listed in ProPresenter's settings.
        case workspace
        /// The folder ProPresenter's settings name as its own.
        case namedInSettings
        /// `ProPresenter` in Documents.
        case documents
        /// A folder the person chose.
        case chosen
    }

    public var kind: Kind
    /// What to call it: a workspace's own name, or the folder's.
    public var name: String
    public var folder: URL
    /// Whether ProPresenter's settings say this workspace is the one in use.
    public var isInUse: Bool

    /// A place is known by where it is.
    public var id: String { folder.resolvingSymlinksInPath().standardizedFileURL.path }

    public init(kind: Kind, name: String, folder: URL, isInUse: Bool = false) {
        self.kind = kind
        self.name = name
        self.folder = folder
        self.isInUse = isInUse
    }
}

public struct PlaceFinder {
    /// The identifier ProPresenter keeps its settings under, which is its bundle's.
    public static let proPresenter = "com.renewedvision.propresenter"

    /// Reads one of ProPresenter's own settings by its key. Nothing is ever written.
    public var proPresentersSetting: (String) -> Any?
    /// The person's Documents folder, as the system says it is.
    public var documents: URL?
    /// Folders the person has chosen by hand, in the order they chose them.
    public var chosen: [URL]

    public init(proPresentersSetting: @escaping (String) -> Any?, documents: URL?, chosen: [URL] = []) {
        self.proPresentersSetting = proPresentersSetting
        self.documents = documents
        self.chosen = chosen
    }

    /// The finder for this Mac.
    public static func onThisMac(chosen: [URL] = []) -> PlaceFinder {
        PlaceFinder(
            proPresentersSetting: { key in
                CFPreferencesCopyAppValue(key as CFString, proPresenter as CFString)
            },
            // (Asked of the system, not built: Documents may be kept in iCloud.)
            documents: FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first,
            chosen: chosen)
    }

    /// Every place there is, each once: the workspaces (the one in use first), the folder
    /// ProPresenter's settings name, Documents, then what was chosen. A folder that is
    /// not there is left out.
    public func places() -> [Place] {
        var found: [Place] = []
        func add(_ place: Place) {
            var isFolder: ObjCBool = false
            guard FileManager.default.fileExists(atPath: place.folder.path, isDirectory: &isFolder), isFolder.boolValue,
                  !found.contains(where: { $0.id == place.id }) else { return }
            found.append(place)
        }
        let workspaces = self.workspaces()
        for workspace in workspaces.filter(\.isInUse) + workspaces.filter({ !$0.isInUse }) {
            add(workspace)
        }
        if let named = proPresentersSetting("applicationShowDirectory") as? String, !named.isEmpty {
            let folder = URL(fileURLWithPath: (named as NSString).expandingTildeInPath, isDirectory: true)
            if Self.isFolder(folder.appendingPathComponent("Libraries")) {
                add(Place(kind: .namedInSettings, name: folder.lastPathComponent, folder: folder))
            }
        }
        if let documents {
            add(Place(kind: .documents, name: "Documents", folder: documents.appendingPathComponent("ProPresenter", isDirectory: true)))
        }
        for folder in chosen {
            add(Place(kind: .chosen, name: folder.lastPathComponent, folder: folder))
        }
        return found
    }

    /// The workspaces ProPresenter's settings list, as they are listed.
    func workspaces() -> [Place] {
        let listed: [[String: Any]]
        switch proPresentersSetting("userWorkspaces") {
        case let data as Data:
            listed = (try? JSONSerialization.jsonObject(with: data)) as? [[String: Any]] ?? []
        case let text as String:
            listed = (try? JSONSerialization.jsonObject(with: Data(text.utf8))) as? [[String: Any]] ?? []
        case let ready as [[String: Any]]:
            listed = ready
        default:
            listed = []
        }
        return listed.compactMap { entry in
            guard let where_ = entry["url"] as? String, !where_.isEmpty else { return nil }
            // (A file URL as ProPresenter writes it; a plain path is taken too.)
            let folder = where_.hasPrefix("file:") ? URL(string: where_) : URL(fileURLWithPath: (where_ as NSString).expandingTildeInPath, isDirectory: true)
            guard let folder, folder.isFileURL else { return nil }
            let name = (entry["name"] as? String).flatMap { $0.isEmpty ? nil : $0 } ?? folder.lastPathComponent
            return Place(kind: .workspace, name: name, folder: folder, isInUse: entry["isActive"] as? Bool ?? false)
        }
    }

    static func isFolder(_ url: URL) -> Bool {
        var isFolder: ObjCBool = false
        return FileManager.default.fileExists(atPath: url.path, isDirectory: &isFolder) && isFolder.boolValue
    }
}
