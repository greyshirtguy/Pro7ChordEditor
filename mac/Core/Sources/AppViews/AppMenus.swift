// The menu bar, kept to what this app has a use for; and the About window.
// spec/10-platforms.md, "Menus".
//
// SwiftUI makes the menus, and macOS adds to them what it gives every app: in Edit, a
// Delete, Writing Tools, AutoFill, Start Dictation and Emoji & Symbols. None of those
// does anything useful here. (Delete would take off the chord at the spot, and the spot
// follows the pointer: by the time the pointer is in the menu it is no longer on the
// chord. The maintainer, 2026-10-13: "I cannot see a user could ever select a chord and
// then select delete from the menu".) So Edit is kept to the six that work wherever
// something can be typed: Undo, Redo, Cut, Copy, Paste and Select All.
//
// What is not wanted is HIDDEN, not removed: the items are SwiftUI's and the system's
// to keep count of, and one that is hidden is still where they left it. It is done
// each time the menu bar is about to be opened, as well as when the menus are made,
// because either of them may put an item back.
//
// What macOS asks before it adds two of them is answered at the start (`atStart`), so
// that those two are never made at all.

import AppKit
import SwiftUI

@MainActor
public enum AppMenus {
    /// What the items of Edit that stay do, by the name of what each sends.
    static let keptInEdit: Set<String> = ["undo:", "redo:", "cut:", "copy:", "paste:", "selectAll:"]

    /// To be done before the app has made anything: the system is told not to add
    /// Start Dictation and Emoji & Symbols to Edit, and not to offer windows as tabs
    /// (which would put Show Tab Bar and Show All Tabs in View, for an app that has
    /// one window). These are said for this run of the app only, and are written
    /// nowhere.
    public static func atStart() {
        UserDefaults.standard.register(defaults: ["NSDisabledDictationMenuItem": true, "NSDisabledCharacterPaletteMenuItem": true])
        NSWindow.allowsAutomaticWindowTabbing = false
    }

    /// Whether a menu is Edit: the one that has Paste in it. (Not by its name, which
    /// is the system's to translate.)
    static func isEdit(_ menu: NSMenu) -> Bool {
        menu.items.contains { $0.action.map(NSStringFromSelector) == "paste:" }
    }

    /// Hides, in Edit, everything but the six that are kept and the lines between
    /// them; and then any line that is left with nothing over it, nothing under it,
    /// or another line next to it.
    static func tidy(edit menu: NSMenu) {
        for item in menu.items where !item.isSeparatorItem {
            let kept = item.action.map { keptInEdit.contains(NSStringFromSelector($0)) } ?? false
            if item.isHidden == kept {
                item.isHidden = !kept
            }
        }
        var lineWanted = false
        var lastLine: NSMenuItem?
        for item in menu.items {
            if item.isSeparatorItem {
                if item.isHidden == lineWanted {
                    item.isHidden = !lineWanted
                }
                if lineWanted {
                    lastLine = item
                }
                lineWanted = false
            } else if !item.isHidden {
                lineWanted = true
                lastLine = nil
            }
        }
        // (A line that nothing follows.)
        lastLine?.isHidden = true
    }

    /// Tidies the menu bar: Edit, wherever it is in it; and a menu that has nothing
    /// in it to choose is not shown at all. (View is one, in this app: all macOS had
    /// put in it was the two commands for tabs. If something is put in it later, it
    /// is shown again.)
    public static func tidy(_ bar: NSMenu?) {
        for item in bar?.items ?? [] {
            guard let menu = item.submenu else { continue }
            if isEdit(menu) {
                tidy(edit: menu)
            }
            let empty = !menu.items.contains { !$0.isHidden && !$0.isSeparatorItem }
            if item.isHidden != empty {
                item.isHidden = empty
            }
        }
    }

    /// What the menu bar has in it that can be seen, one menu to a line: "Edit: Undo,
    /// Redo, -, Cut". (For whoever checks a build with no hand on a mouse:
    /// `saidOnce`, below.)
    static func described(_ bar: NSMenu?) -> String {
        (bar?.items ?? []).filter { !$0.isHidden }.map { top in
            let inside = (top.submenu?.items ?? []).filter { !$0.isHidden }.map { item in
                item.isSeparatorItem ? "-" : item.title + keys(of: item)
            }
            return "\(top.submenu?.title ?? top.title): \(inside.joined(separator: ", "))"
        }.joined(separator: "\n")
    }

    /// The keys an item is chosen by, if it has any, as " [cmd+shift+z]".
    private static func keys(of item: NSMenuItem) -> String {
        guard !item.keyEquivalent.isEmpty else { return "" }
        let flags = item.keyEquivalentModifierMask
        let held = [(NSEvent.ModifierFlags.control, "ctrl"), (.option, "opt"), (.shift, "shift"), (.command, "cmd")].filter { flags.contains($0.0) }.map(\.1)
        return " [" + (held + [item.keyEquivalent]).joined(separator: "+") + "]"
    }

    private static var watching = false
    private static var asked = false
    /// Whether the menus have been written out, where that was asked for.
    private static var saidOnce = false

    /// From now on the menu bar is tidied when the app has started, whenever an item
    /// is added to any menu, and each time the bar is about to be opened.
    public static func watch() {
        guard !watching else { return }
        watching = true
        let center = NotificationCenter.default
        for name in [NSApplication.didFinishLaunchingNotification, NSMenu.didAddItemNotification, NSMenu.didBeginTrackingNotification] {
            center.addObserver(forName: name, object: nil, queue: .main) { _ in
                MainActor.assumeIsolated { soon() }
            }
        }
    }

    /// Once, a moment on, however many times it is asked for meanwhile: items are
    /// added one after another, and the menu is to be whole before it is looked at.
    private static func soon() {
        guard !asked else { return }
        asked = true
        DispatchQueue.main.async {
            asked = false
            tidy(NSApp.mainMenu)
            // Started with PRO7_SAY_MENUS set, the app writes its menus out once, as
            // they are after the tidying, where a program writes what it has to
            // say: so that a build can be checked without opening its menus by
            // hand, which this app's maker's assistant cannot do (mac/NOTES.md).
            // Nobody who opens the app the ordinary way sets it, and nothing is
            // written.
            if !saidOnce, ProcessInfo.processInfo.environment["PRO7_SAY_MENUS"] != nil, let bar = NSApp.mainMenu, bar.items.contains(where: { $0.submenu.map(isEdit) ?? false }) {
                saidOnce = true
                print("MENUS\n" + described(bar))
                fflush(stdout)
                // (And once more a little later: the system goes on adding to the
                // menus for a moment after the app has started.)
                DispatchQueue.main.asyncAfter(deadline: .now() + 3) {
                    tidy(NSApp.mainMenu)
                    print("MENUS, A MOMENT ON\n" + described(NSApp.mainMenu))
                    fflush(stdout)
                }
            }
        }
    }

    // ---- About

    /// The app's icon as the Finder and the Dock show it: the one macOS has for the
    /// app's own file.
    static func icon(ofAppAt path: String) -> NSImage {
        NSWorkspace.shared.icon(forFile: path)
    }

    /// The About window: the system's own, told which icon to show. Left to itself it
    /// showed the blank one macOS gives a program with no icon, though the app has
    /// one (seen by the maintainer in 0.3, 2026-10-13).
    public static func showAbout() {
        NSApp.orderFrontStandardAboutPanel(options: [.applicationIcon: icon(ofAppAt: Bundle.main.bundlePath)])
        NSApp.activate()
    }
}

/// The commands of the menu bar that are this app's own and are not the guide's
/// (GuideCommands): About, with the app's icon; and under File, Revert. There is no
/// Save: nothing is saved by hand, and a Save in the menu says there is saving to do
/// (spec/06-library.md, rule 12a). Command+S is still answered, by the window.
public struct AppCommands: Commands {
    private var model: LibraryModel

    public init(model: LibraryModel) {
        self.model = model
    }

    public var body: some Commands {
        CommandGroup(replacing: .appInfo) {
            Button("About \(AppTitle.name)") { AppMenus.showAbout() }
        }
        // (What this takes the place of had Close in it as well as Save, so Close is
        // said again here: it closes whichever window has the keyboard, the guide's
        // too. Closing the main one quits the app.)
        CommandGroup(replacing: .saveItem) {
            Button("Close") { NSApp.keyWindow?.performClose(nil) }
                .keyboardShortcut("w", modifiers: .command)
            Divider()
            Button("Revert to as Opened\u{2026}") { model.askToRevert() }
                .disabled(!model.canRevert)
        }
    }
}
