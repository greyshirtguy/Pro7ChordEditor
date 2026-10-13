// What the window shows, and what a click in it does, with no drawing in it: the places
// found, the libraries of the one chosen, the songs of the library chosen, the song that
// is open. spec/06-library.md, "The window".
//
// The views (LibraryWindow.swift) only show what is here and pass on what the person
// does. Everything that is a rule is in the layers below (Song: Places, Listing, Marks,
// SongText), which this calls; so that the rules are proved by `swift test` with no
// window, and are the same in the Windows app.
//
// A song is opened with its real safeguards: nothing is saved while ProPresenter runs,
// nor before a copy of the file is kept. Three things here change a song's file: what
// is typed in the ChordPro editor (spec/04-chordpro-editor.md; the rules of that are in
// Song/ChordProEditor.swift, and this only says when to save), the two keys in the
// song's header (spec/05-song-and-saving.md, rules 5 to 7), and putting an earlier
// version back (spec/07-backups.md).
//
// It is handed what it would otherwise ask the Mac for (where the app keeps its own
// things, how to find places, whether ProPresenter is running), so that tests can hand
// it made-up ones. `onThisMac` hands it the real ones.

import ChordRules
import Foundation
import Observation
import Song

@MainActor
@Observable
public final class LibraryModel {
    /// A song as it is shown: its lines, or why it could not be read.
    public struct ShownSong: Equatable, Sendable {
        public var name: String
        /// The key its chords are written in, and the key ProPresenter shows it in; ""
        /// if it states none.
        public var key = ""
        public var shownKey = ""
        public var lines: [SongLine] = []
        public var slides = 0
        public var chords = 0
        /// If it could not be read, the sentence that says so. Then there are no lines.
        public var problem: String?
    }

    /// The copies of the song that is open, and the one picked to be looked at.
    public struct EarlierVersions: Equatable, Sendable {
        public var copies: [KeptCopy]
        public var pickedID: KeptCopy.ID?
        /// The copy picked, as it was: its lines, to look at and not to change.
        public var picked: ShownSong?
        /// One line that sets it against the song as it is now: how many chords each
        /// has, and their keys.
        public var comparison = ""
        /// If putting the picked one back wants asking first (rule 23): the question.
        public var question: String?
    }

    /// What was just put back, for the few seconds in which the line under the song
    /// offers to undo it.
    public struct PutBack: Equatable, Sendable {
        /// "Put back the version of 4 Oct."
        public var sentence: String
        /// The copy that was made just before: putting THAT back is the undoing.
        var before: KeptCopy
    }

    /// The song as ChordPro text, as the editor shows it.
    public struct ChordProShown: Equatable, Sendable {
        /// The text, as it last passed the editor's rule.
        public var text: String
        /// Counted up each time the text is set from here and not typed: the view then
        /// puts it in place of what it has, and forgets its own undo.
        public var version: Int
        /// Counted up each time a song is opened or put back: the view then starts at
        /// the top of the text. (Otherwise the caret stays where it was.)
        public var sitting: Int
        /// Why the song can be read here and not changed, as a sentence; nil if it can.
        public var barred: String?
    }

    private let appData: AppData
    private let copies: Copies
    private var openSong: OpenSong?
    private var editor: ChordProEditor?
    private var textVersion = 0
    private var sitting = 0
    /// The wait for the typing to stop, before what was typed is saved.
    private var pause: Task<Void, Never>?
    private let finder: ([URL]) -> PlaceFinder
    private let askWhetherProPresenterIsRunning: () -> Bool
    private var started = false
    private var keptMarks = KeptMarks()
    private var marking: Task<Void, Never>?
    private var chosenFolders: [URL] = []

    public private(set) var places: [Place] = []
    public private(set) var placeID: Place.ID?
    public private(set) var libraries: [Library] = []
    public private(set) var libraryID: Library.ID?
    public private(set) var songs: [ListedSong] = []
    public private(set) var marks: [ListedSong.ID: Marks] = [:]
    public private(set) var songID: ListedSong.ID?
    public private(set) var shown: ShownSong?
    public private(set) var proPresenterIsRunning = false
    /// The names of the songs of the library that have copies kept.
    public private(set) var songsWithCopies: Set<String> = []
    /// The panel of earlier versions, while it is open.
    public private(set) var earlier: EarlierVersions?
    public private(set) var justPutBack: PutBack?
    /// What went wrong with the last thing asked, as a sentence; nothing if nothing did.
    public private(set) var refusal: String?
    /// Whether that was that the file has been changed by something else, so that the
    /// song is offered read again.
    public private(set) var offersOpeningAgain = false
    /// The song as ChordPro text; nil if no song is open, or it could not be read.
    public private(set) var chordPro: ChordProShown?
    /// Which of the two editors the song is shown in (spec/06, rule 12).
    public private(set) var editorShown: Editor = .sheet
    /// The song as the chord sheet has it; nil if no song is open, or it could not be
    /// read. Its view reads it and tells it what was done; what it asks of the song
    /// comes back here (`sheetChanged`, `refuse`).
    public private(set) var sheet: ChordSheet?
    /// Goes up whenever the sheet was changed from outside its own view, which is then
    /// to lay it out and draw it again.
    public private(set) var sheetVersion = 0
    /// How many times its usual size the sheet is drawn: one of `sheetZooms`. The words
    /// are smaller or larger by it, and so the slides' cards, and so how many of them
    /// there are to a row (spec/03, "Zooming").
    public private(set) var sheetZoom = LibraryModel.usualSheetZoom
    public static let sheetZooms: [Double] = [0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5]
    public static let usualSheetZoom = 0.8
    /// The colour of a chord's name on the sheet, and of a chord in the ChordPro text,
    /// each as "#rrggbb"; nothing where the usual colour is kept (spec/06, "Settings").
    /// Only how chords look in this app: nothing in any song's file says a colour.
    public private(set) var sheetChordColour: String?
    public private(set) var textChordColour: String?
    /// The colour of the words of the highlighted line on the sheet, the line the
    /// person is at (SheetHints.swift), kept the same way: nothing where it is the
    /// orange it comes as.
    public private(set) var sheetLineColour: String?
    /// How many times its usual size the ChordPro text is: one of the same steps, kept
    /// apart from the sheet's, since a size that suits a grid of slides is not the one
    /// that suits a page of text.
    public private(set) var textZoom = LibraryModel.usualTextZoom
    public static let usualTextZoom = 1.0
    /// How much of the height of the two lists at the window's left the libraries have,
    /// the songs having the rest: what the bar between them is dragged to.
    public private(set) var listsSplit = LibraryModel.usualListsSplit
    public static let usualListsSplit = 0.36
    /// How wide those two lists are, the song having the rest of the window: what the
    /// edge between them and the song is dragged to, between a least and a most. As
    /// it comes it is half way between the two (the maintainer's, 2026-10-13: "about
    /// half way between min and max"), whatever the window's width.
    public private(set) var listsWidth = LibraryModel.usualListsWidth
    public nonisolated static let leastListsWidth = 210.0
    public nonisolated static let mostListsWidth = 480.0
    public nonisolated static let usualListsWidth = (leastListsWidth + mostListsWidth) / 2
    /// Goes up each time the guide is to be shown (spec/13-guide.md): because it was
    /// asked for, by the ? in the strip, or by the app itself, which shows it once, at
    /// its first start on this Mac. The window watches this and opens the guide.
    public private(set) var guideAsked = 0
    /// Whether the guide has opened by itself already. It is remembered.
    private var guideShown = false
    /// What the sheet just said ("4 chords copied ..."), for a few seconds.
    public private(set) var said: String?
    private var saying: Task<Void, Never>?
    /// Whether the song refused the last thing the sheet asked of it.
    private var sheetRefused = false
    /// Whether what the quiet line says went wrong is that ProPresenter was running:
    /// that goes from the line when ProPresenter does.
    private var refusedForProPresenter = false
    /// The song's file as it was when the song was opened here: what Revert puts back.
    private var asOpened: Data?
    /// What Revert asks before it does anything, while it is asking.
    public private(set) var revertQuestion: RevertQuestion?

    /// A question put to the person before a song is reverted: its two sentences, and
    /// what the button that says yes says.
    public struct RevertQuestion: Equatable, Sendable {
        public var title: String
        public var message: String
        public var yes: String
    }
    /// Whether the song's file is no longer that: something has been changed here
    /// since the song was opened, and Revert has something to do.
    public private(set) var changedSinceOpened = false
    /// Whether the song has been changed otherwise than by typing in the text, since
    /// the text was made from it: the text is then made again before it is next shown.
    private var textIsStale = false

    public enum Editor: String, Sendable, CaseIterable {
        case sheet, chordPro
    }

    /// What is going on in the sheet, as its view says it; what a paste there would
    /// do; and whether Option or Command is held down: what the line of hints under
    /// the sheet goes by (SheetHints.swift). None of it is anything of the song's.
    public private(set) var sheetSituation = SheetHints.Situation.nothing
    public private(set) var sheetPaste = SheetHints.Paste()
    public private(set) var modifierHeld = false
    /// The same three as they were last said, kept where nothing watches them
    /// (`sheetIs` has why).
    @ObservationIgnored private var told = (situation: SheetHints.Situation.nothing, paste: SheetHints.Paste(), held: false)
    /// How long what the sheet said stays on the quiet line.
    public var sayingLasts: Duration = .seconds(6)

    /// How long after the typing stops a change is saved (spec/04, rule 13).
    public var pauseBeforeSaving: Duration = .milliseconds(700)

    /// What is typed in the search box, and what the list is narrowed to by mark.
    public var typed = ""
    public var filter: MarkFilter = .all

    public init(appData: AppData, appVersion: String = "", finder: @escaping ([URL]) -> PlaceFinder, proPresenterIsRunning: @escaping () -> Bool) {
        self.appData = appData
        self.copies = appData.copies(appVersion: appVersion)
        self.finder = finder
        self.askWhetherProPresenterIsRunning = proPresenterIsRunning
    }

    public static func onThisMac() -> LibraryModel {
        LibraryModel(appData: .onThisMac(), appVersion: AppTitle.versionOfThisApp,
                     finder: { PlaceFinder.onThisMac(chosen: $0) }, proPresenterIsRunning: ProPresenterApp.isRunning)
    }

    // ---- What is shown

    public var place: Place? { places.first { $0.id == placeID } }
    public var library: Library? { libraries.first { $0.id == libraryID } }

    /// The songs of the library, narrowed by what is typed and by mark. The song that
    /// is open stays in the list whatever it is narrowed to: its first chord must not
    /// take it out of "without chords" from under the person typing it.
    public var listedSongs: [ListedSong] {
        let narrowed = Set(Listing.narrowed(songs, to: typed).filter { filter.lets(marks[$0.id]) }.map(\.id))
        return songs.filter { narrowed.contains($0.id) || $0.id == songID }
    }

    /// Whether the song that is open can be changed just now: it was read, ProPresenter
    /// is not running, and (for typing) its text can say it exactly.
    public var canChange: Bool { openSong != nil && !proPresenterIsRunning }
    public var canType: Bool { canChange && chordPro != nil && chordPro?.barred == nil }

    /// The keys a song can be said to be in, as the pickers list them.
    public var keys: [String] { Chords.majorKeys() + Chords.minorKeys() }

    /// The quiet line under the song: what is there to be seen. Nothing here is a
    /// promise of what the app will do.
    public var notice: String {
        if let refusal { return refusal }
        if let justPutBack { return justPutBack.sentence }
        if let shown {
            if shown.problem != nil { return "Nothing was done to this file." }
            // What was just said (a copy, a paste, Command+S) is said in either editor.
            if let said { return said }
            let slides = shown.slides == 1 ? "1 slide" : "\(shown.slides) slides"
            let chords = shown.chords == 1 ? "1 chord" : "\(shown.chords) chords"
            // (What the keys do on the sheet is said on a line of its own, over this
            // one: `sheetHints`.)
            if editorShown == .sheet { return "\(slides), \(chords)." }
            // (There is no Save button to look for, so it says there is no need of one;
            // but only where something can be typed.)
            return canType ? "\(slides), \(chords). A change is saved a moment after it is typed." : "\(slides), \(chords)."
        }
        if library != nil {
            let withChords = songs.filter { marks[$0.id]?.hasChords == true }.count
            return songs.count == 1 ? "1 song, \(withChords) with chords." : "\(songs.count) songs, \(withChords) with chords."
        }
        return ""
    }

    /// What the line of hints under the sheet says just now.
    public var sheetHints: [SheetHints.Hint] {
        guard canChange else { return SheetHints.cannotChange }
        return SheetHints.hints(sheetSituation, held: modifierHeld, paste: sheetPaste)
    }

    /// The sheet's view says what is going on in it, and what a paste there would do.
    /// (Only what has changed is put down: the pointer says this at every move.)
    ///
    /// What was last said is kept a second time, where nothing watches it, and that
    /// is what the new is held against. The sheet's view says this while it is being
    /// brought up to date by the window, among other times; and a thing that is READ
    /// while a view is brought up to date is a thing the window then brings that view
    /// up to date for, every time it changes. So the sheet's view was brought up to
    /// date, and drawn again whole, each time the hints changed: by its own saying.
    public func sheetIs(_ situation: SheetHints.Situation, paste: SheetHints.Paste = SheetHints.Paste()) {
        if told.situation != situation {
            told.situation = situation
            sheetSituation = situation
        }
        if told.paste != paste {
            told.paste = paste
            sheetPaste = paste
        }
    }

    /// Option or Command went down, or came up.
    public func holds(_ held: Bool) {
        if told.held != held {
            told.held = held
            modifierHeld = held
        }
    }

    // ---- What the person does

    /// When the window first shows: what the app kept is read, the places are found, and
    /// the place and library last open are opened again. Asked a second time it does
    /// nothing.
    public func start() async {
        guard !started else { return }
        started = true
        let settings = appData.settings
        chosenFolders = settings.chosenFolders.map { URL(fileURLWithPath: $0, isDirectory: true) }
        sheetZoom = Self.nearestZoom(to: settings.sheetZoom ?? Self.usualSheetZoom)
        listsSplit = Self.within(settings.listsSplit ?? Self.usualListsSplit)
        listsWidth = Self.withinWidths(settings.listsWidth ?? Self.usualListsWidth)
        sheetChordColour = Self.colour(settings.sheetChordColour)
        textChordColour = Self.colour(settings.textChordColour)
        sheetLineColour = Self.colour(settings.sheetLineColour)
        textZoom = Self.nearestZoom(to: settings.textZoom ?? Self.usualTextZoom)
        keptMarks = appData.marks
        // Copies that are kept no longer go now, when the app starts (spec/07, rule 12).
        let thrownAway = copies.thin()
        if thrownAway > 0 {
            appData.log("\(thrownAway) old copies were thrown away.")
        }
        guideShown = settings.guideShown ?? false
        findPlaces(preferring: settings.lastPlace, library: settings.lastLibrary)
        proPresenterIsRunning = askWhetherProPresenterIsRunning()
        appData.log("Started. \(places.count) places found, \(libraries.count) libraries in the one open.")
        // The guide opens by itself once: at the first start of the app on this Mac,
        // which is the first at which the app's own settings do not say that it has.
        // That it has is written down now, and not when the guide is closed, so that
        // whatever becomes of this start it is not opened unasked a second time.
        // (Only that is written: what else was kept stays as it was read, the place
        // last open too, which may not be there to be found today.)
        if !guideShown {
            guideShown = true
            var kept = appData.settings
            kept.guideShown = true
            appData.settings = kept
            guideAsked += 1
            appData.log("The guide was shown, as it is once.")
        }
        await marksAreWorkedOut()
    }

    /// The guide is asked for: by the ? at the right of the strip. (Help in the menu
    /// bar opens it without asking here, so that it does so with no window open too.)
    public func showGuide() {
        guideAsked += 1
    }

    /// The system says an application has started or quit, or a moment has gone by
    /// since one did. Whether ProPresenter is running is asked again, so that the band
    /// comes and goes by itself and not only when the app is next brought to the
    /// front: ProPresenter can be quit from its Dock icon with this app in front all
    /// the while, and when it is quit from its own window this app is in front before
    /// ProPresenter has finished going.
    public func proPresenterMayHaveChanged() {
        let running = askWhetherProPresenterIsRunning()
        guard running != proPresenterIsRunning else { return }
        proPresenterIsRunning = running
        if !running, refusedForProPresenter {
            // What was refused because of it can be done now: the line need not go on
            // saying otherwise.
            refusal = nil
            refusedForProPresenter = false
        }
        sheetVersion += 1
    }

    /// When the app comes to the front: the lists are read again, since ProPresenter may
    /// have added songs meanwhile, and whether it is running is asked again.
    public func refresh() async {
        guard started else { return }
        settle()
        let song = songID
        findPlaces(preferring: placeID, library: libraryID)
        proPresenterIsRunning = askWhetherProPresenterIsRunning()
        // The song that is open stays open, with what was typed in it and its undo, if
        // its file is still the one that was read. If something else has changed the
        // file meanwhile, it is read again: nothing could be saved over it anyway.
        // (Not while the panel of earlier versions is open over it: that is put away
        // by the person, not by the app coming to the front.)
        if let song, songs.contains(where: { $0.id == song }), earlier == nil, openSong?.fileIsAsLastRead != true {
            open(song: song)
        }
        await marksAreWorkedOut()
    }

    public func choose(place id: Place.ID?) {
        guard id != placeID, settle() else { return }
        placeID = id
        listLibraries(preferring: nil)
        remember()
    }

    public func choose(library id: Library.ID?) {
        guard id != libraryID, settle() else { return }
        libraryID = id
        listSongs()
        remember()
    }

    /// A click on a song opens it. One that cannot be read says so, and nothing is done
    /// to it. What was being typed in the song that is left is saved first.
    public func open(song id: ListedSong.ID?) {
        guard settle() else { return }
        songID = id
        earlier = nil
        justPutBack = nil
        refusal = nil
        offersOpeningAgain = false
        close()
        guard let song = songs.first(where: { $0.id == id }), let kept = keptSong(song) else {
            shown = nil
            return
        }
        do {
            // With its safeguards: ProPresenter is asked after before every save, and a
            // copy is kept before the first.
            let copies = copies
            let opened = try OpenSong(opening: song.url, safeguards: Safeguards(
                proPresenterIsRunning: askWhetherProPresenterIsRunning,
                keepCopy: { file, why in try copies.keep(file, of: kept, why: why) }))
            openSong = opened
            asOpened = opened.bytes
            changedSinceOpened = false
            shown = Self.show(opened, named: song.name)
            edit(opened)
            appData.log("Opened \(song.url.lastPathComponent).")
        } catch {
            shown = ShownSong(name: song.name, problem: (error as? Refusal)?.sentence ?? "\(song.url.lastPathComponent) could not be read.")
            appData.log("Could not read \(song.url.lastPathComponent).")
        }
    }

    /// "Open Again": the song read afresh from its file, after something else changed it.
    public func openAgain() {
        open(song: songID)
    }

    // ---- The ChordPro editor (spec/04-chordpro-editor.md)

    /// The text view asks before it lets a change through: may the text become this?
    /// If it may, it is taken, and saved a moment after the typing stops. If it may
    /// not, the text stays as it was, and nothing is said: the caret not moving says it.
    public func textMayBecome(_ text: String) -> Bool {
        guard let editor, canType else { return false }
        switch editor.take(text) {
        case .taken:
            refusal = nil
            offersOpeningAgain = false
            chordPro?.text = text
            pause?.cancel()
            let wait = pauseBeforeSaving
            pause = Task { [weak self] in
                try? await Task.sleep(for: wait)
                guard !Task.isCancelled else { return }
                self?.settle()
            }
            return true
        case .notTheWords:
            return false
        }
    }

    /// Saves what has been typed, without waiting for the typing to pause: before the
    /// song is left for another, before anything else is done to it, and when the app
    /// is left or quit. Answers whether all is saved; false if a save was refused, and
    /// then whatever was to be done next is not done, so that the sentence is seen
    /// and not swept away with the song.
    @discardableResult
    public func settle() -> Bool {
        pause?.cancel()
        pause = nil
        // A chord being typed on the sheet is kept first (spec/03, rule 39).
        if let sheet, sheet.typing {
            sheetRefused = false
            sheet.settle()
            sheetVersion += 1
            if sheetRefused { return false }
        }
        guard let editor, openSong != nil else { return true }
        do {
            if try editor.save() {
                noteAChange()
            }
            return true
        } catch {
            // What was saved before the refusal stays saved. The text is made again
            // from the song, so that what is seen is what the sentence says: nothing
            // (more) was changed.
            refuse(error)
            editor.reload(keepingWhatIsTyped: false)
            showText(of: editor)
            noteAChange()
            return false
        }
    }

    /// Waits until what was typed has been saved, if a save is waiting.
    public func whatWasTypedIsSaved() async {
        await pause?.value
    }

    // ---- The two keys (spec/05-song-and-saving.md, rules 5 to 7)

    /// Says what key the song's chords are written in. It changes no chord.
    public func setKey(_ key: String) {
        change { try $0.setKey(key) }
    }

    /// Says what key ProPresenter is to show the song's chords in, and that alone.
    public func setShownKey(_ key: String) {
        change { try $0.setShownKey(key) }
    }

    private func change(_ ask: (OpenSong) throws -> Void) {
        guard settle(), let song = openSong else { return }
        refusal = nil
        offersOpeningAgain = false
        do {
            try ask(song)
            noteAChange()
        } catch {
            refuse(error)
        }
        sheet?.reload()
        sheetVersion += 1
    }

    // ---- The chord sheet (spec/03-chord-sheet.md)

    /// Shows the song in the other editor. What was being typed in the one that is
    /// left is kept first, and the one that is come to is made again from the song,
    /// since the other may have changed it.
    public func show(editor wanted: Editor) {
        guard wanted != editorShown, settle() else { return }
        switch wanted {
        case .chordPro:
            if let editor, textIsStale {
                editor.reload(keepingWhatIsTyped: false)
                showText(of: editor)
            }
        case .sheet:
            sheet?.reload()
        }
        editorShown = wanted
        sheetVersion += 1
    }

    /// The song's own history, which every change of either editor is a step of: the
    /// last change taken back, and put again (spec/03, rule 53).
    public var canUndo: Bool { canChange && openSong?.canUndo == true }
    public var canRedo: Bool { canChange && openSong?.canRedo == true }

    public func undo() {
        guard openSong?.canUndo == true else { return }
        change { try $0.undo() }
        textIsStale = true
    }

    public func redo() {
        guard openSong?.canRedo == true else { return }
        change { try $0.redo() }
        textIsStale = true
    }

    /// Makes the sheet's words a step larger (1) or smaller (-1), or their usual size
    /// (0). It is remembered. Answers with the size it now is.
    @discardableResult
    public func zoomSheet(_ direction: Int) -> Double {
        let now = Self.sheetZooms.firstIndex(of: Self.nearestZoom(to: sheetZoom)) ?? 0
        let wanted = direction == 0 ? Self.usualSheetZoom : Self.sheetZooms[min(max(now + direction, 0), Self.sheetZooms.count - 1)]
        if wanted != sheetZoom {
            sheetZoom = wanted
            sheetVersion += 1
            if started {
                remember()
            }
        }
        return sheetZoom
    }

    /// Chooses the colour of a chord's name on the sheet: "#rrggbb", or nothing for
    /// the usual one. What is no colour so written is taken as nothing. It is
    /// remembered.
    public func setSheetChordColour(_ colour: String?) {
        sheetChordColour = Self.colour(colour)
        sheetVersion += 1
        if started {
            remember()
        }
    }

    /// The same for a chord in the ChordPro text.
    public func setTextChordColour(_ colour: String?) {
        textChordColour = Self.colour(colour)
        if started {
            remember()
        }
    }

    /// And for the words of the highlighted line on the sheet.
    public func setSheetLineColour(_ colour: String?) {
        sheetLineColour = Self.colour(colour)
        sheetVersion += 1
        if started {
            remember()
        }
    }

    /// A colour as it is kept: "#" and six of 0 to 9 and a to f, in small letters.
    /// Nothing for anything else.
    static func colour(_ written: String?) -> String? {
        guard let written else { return nil }
        let small = written.lowercased()
        guard small.count == 7, small.first == "#", small.dropFirst().allSatisfy({ $0.isASCII && $0.isHexDigit }) else { return nil }
        return small
    }

    /// The bar between the libraries and the songs was dragged, to where the
    /// libraries have this much of the height; or, with nothing, put back where it
    /// usually is. It is remembered.
    public func setListsSplit(_ share: Double?) {
        listsSplit = Self.within(share ?? Self.usualListsSplit)
        if started {
            remember()
        }
    }

    private static func within(_ share: Double) -> Double {
        min(max(share, 0.1), 0.9)
    }

    /// The edge between the lists and the song was dragged, to where the lists are
    /// this wide; or, with nothing, put back where it usually is. It is remembered.
    public func setListsWidth(_ width: Double?) {
        listsWidth = Self.withinWidths(width ?? Self.usualListsWidth)
        if started {
            remember()
        }
    }

    private static func withinWidths(_ width: Double) -> Double {
        min(max(width, leastListsWidth), mostListsWidth)
    }

    /// Makes the ChordPro text a step larger (1) or smaller (-1), or its usual size
    /// (0). It is remembered. Answers with the size it now is.
    @discardableResult
    public func zoomText(_ direction: Int) -> Double {
        let now = Self.sheetZooms.firstIndex(of: Self.nearestZoom(to: textZoom)) ?? 0
        let wanted = direction == 0 ? Self.usualTextZoom : Self.sheetZooms[min(max(now + direction, 0), Self.sheetZooms.count - 1)]
        if wanted != textZoom {
            textZoom = wanted
            if started {
                remember()
            }
        }
        return textZoom
    }

    public var canZoomTextIn: Bool { textZoom < (Self.sheetZooms.last ?? 1) }
    public var canZoomTextOut: Bool { textZoom > (Self.sheetZooms.first ?? 1) }

    public var canZoomSheetIn: Bool { sheetZoom < (Self.sheetZooms.last ?? 1) }
    public var canZoomSheetOut: Bool { sheetZoom > (Self.sheetZooms.first ?? 1) }

    private static func nearestZoom(to wanted: Double) -> Double {
        sheetZooms.min { abs($0 - wanted) < abs($1 - wanted) } ?? usualSheetZoom
    }

    /// A click on one of the key's seven chords in the strip: on the spot, or where a
    /// chord was being typed, in place of what was typed.
    public func putOfKey(_ number: Int) {
        sheet?.putOfKey(number)
        sheetVersion += 1
    }

    /// One of the key's chords is picked up from the strip, to be carried to the sheet
    /// and let go on a word: a chord that was being typed is kept first (spec/03, rule
    /// 46e).
    public func liftOfKey() {
        guard let sheet, sheet.typing else { return }
        sheet.liftOfKey()
        sheetVersion += 1
    }

    /// The carrying is over, wherever it ended: nothing is left looking as if a chord
    /// were still over the sheet.
    public func carriedOfKeyEnded() {
        guard let sheet else { return }
        sheet.carryOff()
        sheetVersion += 1
    }

    /// Copies the chords of the spot's line, or of its whole group, and says so on the
    /// quiet line for a few seconds.
    public func copyChords(wholeGroup: Bool) {
        guard let sentence = sheet?.copy(whole: wholeGroup) else { return }
        refusal = nil
        say(sentence)
    }

    /// Puts what was copied on the lines from the spot's on, and says what came of
    /// it: a paste that changed nothing would otherwise look like a key that did not
    /// work, and there are plain reasons it may change nothing.
    public func pasteChords() {
        guard let sheet else { return }
        guard sheet.copied != nil else {
            say("Nothing has been copied yet. \(SheetHints.command)C copies the chords of the line the spot is on.")
            return
        }
        guard let pasted = sheet.paste() else { return }
        sheetVersion += 1
        // (What the song refused has its own sentence on the line already.)
        if pasted.refused == 0, let sentence = Self.said(of: pasted) {
            say(sentence)
        }
    }

    /// What is said of a paste.
    static func said(of pasted: ChordSheet.Pasted) -> String? {
        let otherKind = "chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none"
        if pasted.lines == 0 {
            return pasted.passedOver > 0 ? "Nothing was put here: \(otherKind)." : nil
        }
        if pasted.chords == 0 {
            return "None of the copied chords had a word to go on here."
        }
        var sentence = "\(pasted.chords) \(pasted.chords == 1 ? "chord" : "chords") put on \(pasted.lines == 1 ? "the line" : "\(pasted.lines) lines")."
        if pasted.leftOut > 0 {
            sentence += " \(pasted.leftOut) had no word to go on, and \(pasted.leftOut == 1 ? "was" : "were") left out."
        }
        if pasted.passedOver > 0 {
            sentence += " \(pasted.passedOver == 1 ? "1 line was" : "\(pasted.passedOver) lines were") passed over: \(otherKind)."
        }
        return sentence
    }

    /// Puts a sentence on the quiet line for a few seconds.
    private func say(_ sentence: String) {
        said = sentence
        saying?.cancel()
        let lasts = sayingLasts
        saying = Task { [weak self] in
            try? await Task.sleep(for: lasts)
            guard !Task.isCancelled else { return }
            self?.said = nil
        }
    }

    /// Waits until what the sheet said has gone from the quiet line again.
    public func whatWasSaidIsGone() async {
        await saying?.value
    }

    /// The sheet's song for a song just opened, or put back.
    private func makeSheet(of song: OpenSong) -> ChordSheet {
        let made = ChordSheet(song: song)
        sheetIs(.nothing)
        made.pasteKeys = SheetHints.command + "V"
        made.refused = { [weak self] error in
            self?.sheetRefused = true
            self?.refuse(error)
        }
        made.changed = { [weak self] in self?.sheetChanged() }
        return made
    }

    /// The song took a change the sheet asked for.
    private func sheetChanged() {
        refusal = nil
        offersOpeningAgain = false
        said = nil
        textIsStale = true
        noteAChange()
    }

    // ---- Earlier versions (spec/07-backups.md, rules 14 to 23)

    /// Whether the song that is open has copies kept.
    public var hasEarlierVersions: Bool {
        songs.first { $0.id == songID }.map { songsWithCopies.contains($0.name) } ?? false
    }

    /// Opens the panel, with the newest copy picked.
    public func showEarlierVersions() {
        guard settle() else { return }
        guard let song = songs.first(where: { $0.id == songID }), let kept = keptSong(song) else { return }
        let all = copies.copies(of: kept)
        guard !all.isEmpty else { return }
        earlier = EarlierVersions(copies: all)
        pick(copy: all.first?.id)
    }

    /// When a copy was made, as the list says it: "Today 09:14", "Sunday 4 Oct".
    public func when(_ copy: KeptCopy) -> String {
        copies.when(copy.made)
    }

    /// Esc, or a click outside, puts the panel away.
    public func closeEarlierVersions() {
        earlier = nil
    }

    /// Picking one shows it as it was, and one line that sets it against the song now.
    public func pick(copy id: KeptCopy.ID?) {
        guard var panel = earlier, let copy = panel.copies.first(where: { $0.id == id }) else { return }
        panel.pickedID = copy.id
        panel.question = nil
        if let then = try? OpenSong(opening: copy.url) {
            panel.picked = Self.show(then, named: copy.songName)
        } else {
            panel.picked = ShownSong(name: copy.songName, problem: "This copy could not be read as a ProPresenter presentation.")
        }
        let now = shown.map { Self.count($0.chords, key: $0.key) } ?? ""
        panel.comparison = "\(Self.count(copy.chords, key: copy.key)).  Now: \(now)."
        earlier = panel
    }

    /// "Put This One Back". A copy that belongs to another song than the one now at
    /// that place (another id inside) asks first, saying both names; `sure` is the yes.
    public func putBackPicked(sure: Bool = false) {
        guard var panel = earlier, let copy = panel.copies.first(where: { $0.id == panel.pickedID }), let song = openSong else { return }
        if !sure && !copy.songID.isEmpty && copy.songID != song.id {
            panel.question = "This copy is of \u{201C}\(copy.songName)\u{201D}, and the song here now is \u{201C}\(song.name)\u{201D}: another song under the same name. Put the copy in its place?"
            earlier = panel
            return
        }
        putBack(copy, undoing: false)
    }

    /// Undoes the putting back, which is only putting back the copy made just before it.
    public func takeBack() {
        guard let before = justPutBack?.before else { return }
        putBack(before, undoing: true)
    }

    /// The offer to undo goes after a few seconds; the copy is in the list all the same.
    public func forgetWhatWasPutBack() {
        justPutBack = nil
    }

    private func putBack(_ copy: KeptCopy, undoing: Bool) {
        let when = copies.when(copy.made)
        putBack(bytes: { try Data(contentsOf: copy.url) }, undoing: undoing, said: "Put back the version of \(when).",
                logged: { "Put back an earlier version of \($0), the one of \(when)." }, notDone: { "An earlier version of \($0) was not put back." })
    }

    /// Makes the song's file these bytes again: an earlier version of it, or the song
    /// as it was when it was opened. It is a save like any other, and the file as it
    /// is now is kept as a copy first, so that this can be taken back.
    private func putBack(bytes: () throws -> Data, undoing: Bool, said: String, logged: (String) -> String, notDone: (String) -> String) {
        guard settle(), let song = openSong, let listed = songs.first(where: { $0.id == songID }), let kept = keptSong(listed) else { return }
        refusal = nil
        offersOpeningAgain = false
        do {
            try song.putBack(try bytes())
        } catch {
            refuse(error, or: "The copy could not be read.")
            appData.log(notDone(listed.url.lastPathComponent))
            earlier?.question = nil
            return
        }
        // The song starts afresh from its file, and so does its text. The newest copy
        // now is the file as it was just before: the one to put back, to take this back.
        edit(song)
        noteAChange()
        earlier = nil
        if undoing {
            justPutBack = nil
            appData.log("Putting back was undone for \(listed.url.lastPathComponent).")
        } else if let before = copies.copies(of: kept).first {
            justPutBack = PutBack(sentence: said, before: before)
            appData.log(logged(listed.url.lastPathComponent))
        }
    }

    // ---- Saving, which is not done by hand (spec/06, rule 12a; spec/07, rule 22a)

    /// **Revert** asks first. It takes back everything done to the song since it was
    /// opened, which may be an hour's work, and its name alone could be read as taking
    /// back the last change, which is Undo's. So the question says which: all of them.
    /// (In one sentence, which is the maintainer's: a longer one was not read.)
    public func askToRevert() {
        guard canRevert, let shown else { return }
        revertQuestion = RevertQuestion(
            title: "Revert \u{201C}\(shown.name)\u{201D} to how it was when it was opened?",
            message: "This takes back EVERY change made to this song since it was opened in this editor.",
            yes: "Revert All Changes")
    }

    /// The answer was no.
    public func dontRevert() {
        revertQuestion = nil
    }

    /// The answer was yes: the song is made what it was when it was opened here. It is
    /// a putting back like that of an earlier version: what the song is now is kept as
    /// a copy first, and the quiet line offers to undo it.
    public func revert() {
        revertQuestion = nil
        guard let asOpened, changedSinceOpened else { return }
        putBack(bytes: { asOpened }, undoing: false, said: "The song is as it was when it was opened here.",
                logged: { "Reverted \($0) to as it was opened." }, notDone: { "\($0) was not reverted." })
    }

    public var canRevert: Bool { canChange && changedSinceOpened }

    /// What stands at the right of the quiet line, where a Save button would be:
    /// there is nothing to save by hand, since every change is saved as it is made.
    /// While ProPresenter is running nothing is saved at all.
    public var savingCaption: String {
        canChange ? "Autosave on" : "Autosave paused"
    }

    /// Command+S, pressed out of habit. What has been typed and not yet saved is saved
    /// without waiting for the pause, and the quiet line says that there was nothing
    /// to do by hand.
    public func saveNow() {
        guard openSong != nil else { return }
        guard !proPresenterIsRunning else {
            say("ProPresenter is running. Quit it to change chords here.")
            return
        }
        if settle() {
            say("Saved. Every change here is saved as it is made: there is nothing to save by hand.")
        }
    }

    private static func count(_ chords: Int, key: String) -> String {
        (chords == 1 ? "1 chord" : "\(chords) chords") + (key.isEmpty ? ", no key" : ", in \(key)")
    }

    private static func show(_ song: OpenSong, named name: String) -> ShownSong {
        let boxes = song.textBoxes
        return ShownSong(name: name, key: song.key, shownKey: song.shownKey, lines: SongText.lines(of: boxes),
                         slides: song.slideCount, chords: boxes.reduce(0) { $0 + $1.chords.count })
    }

    /// A song as its copies know it: by its place, its library and its name.
    private func keptSong(_ song: ListedSong) -> KeptSong? {
        guard let place, let library else { return nil }
        return KeptSong(place: place.name, library: library.name, name: song.name)
    }

    private func noteCopies() {
        songsWithCopies = place.flatMap { place in library.map { copies.namesWithCopies(place: place.name, library: $0.name) } } ?? []
    }

    /// No song is open.
    private func close() {
        pause?.cancel()
        pause = nil
        openSong = nil
        asOpened = nil
        changedSinceOpened = false
        revertQuestion = nil
        editor = nil
        chordPro = nil
        sheet = nil
        said = nil
    }

    /// The song as text, made afresh from it: a new sitting at the editor.
    private func edit(_ song: OpenSong) {
        let made = ChordProEditor(song: song)
        editor = made
        sitting += 1
        showText(of: made)
        sheet = makeSheet(of: song)
        sheetVersion += 1
    }

    /// Sets the text the view shows from the editor's, in place of whatever was typed.
    private func showText(of editor: ChordProEditor) {
        textVersion += 1
        textIsStale = false
        chordPro = ChordProShown(text: editor.text, version: textVersion, sitting: sitting, barred: editor.barred)
    }

    /// Something was not done: its sentence, and what follows from why.
    private func refuse(_ error: Error, or otherwise: String? = nil) {
        let refused = error as? Refusal
        refusal = refused?.sentence ?? otherwise ?? "That could not be done: \(error.localizedDescription)"
        offersOpeningAgain = refused?.reason == .changedByAnother
        refusedForProPresenter = refused?.reason == .proPresenterIsRunning
        if refusedForProPresenter {
            proPresenterIsRunning = true
        }
        if let song = songs.first(where: { $0.id == songID }) {
            appData.log("A change to \(song.url.lastPathComponent) was refused.")
        }
    }

    /// The song's file has been changed from here: what is shown of it, its marks in
    /// the list, and whether it has copies, are brought up to date.
    private func noteAChange() {
        guard let song = openSong, let index = songs.firstIndex(where: { $0.id == songID }) else { return }
        shown = Self.show(song, named: songs[index].name)
        changedSinceOpened = song.bytes != asOpened
        // (A URL keeps what it was last told of its file; a new one asks again.)
        let values = try? URL(fileURLWithPath: songs[index].url.path).resourceValues(forKeys: [.fileSizeKey, .contentModificationDateKey])
        songs[index].size = values?.fileSize ?? songs[index].size
        songs[index].modified = values?.contentModificationDate ?? songs[index].modified
        let found = Marks.read(songs[index])
        marks[songs[index].id] = found
        keptMarks.keep(found, for: songs[index])
        appData.marks = keptMarks
        noteCopies()
        appData.log("Saved \(songs[index].url.lastPathComponent).")
    }

    /// A folder chosen by hand becomes a place, is remembered, and is opened.
    public func add(folder: URL) {
        guard settle() else { return }
        if !chosenFolders.contains(where: { $0.standardizedFileURL == folder.standardizedFileURL }) {
            chosenFolders.append(folder)
        }
        findPlaces(preferring: Place(kind: .chosen, name: "", folder: folder).id, library: nil)
        remember()
        appData.log("A folder was chosen: \(folder.lastPathComponent).")
        Task { await marksAreWorkedOut() }
    }

    /// Waits until the marks of the songs listed have been worked out.
    public func marksAreWorkedOut() async {
        await marking?.value
    }

    // ---- How

    private func findPlaces(preferring wanted: Place.ID?, library: Library.ID?) {
        places = finder(chosenFolders).places()
        placeID = places.first { $0.id == wanted }?.id ?? places.first?.id
        listLibraries(preferring: library)
    }

    private func listLibraries(preferring wanted: Library.ID?) {
        libraries = place.map(Listing.libraries(of:)) ?? []
        libraryID = libraries.first { $0.id == wanted }?.id ?? libraries.first?.id
        listSongs()
    }

    private func listSongs() {
        songs = library.map(Listing.songs(of:)) ?? []
        if !songs.contains(where: { $0.id == songID }) {
            songID = nil
            shown = nil
            close()
            earlier = nil
        }
        noteCopies()
        // What was worked out before, for the files that are as they were then; the
        // rest in the background, a few at a time, so that the list is there at once.
        marks = [:]
        var unknown: [ListedSong] = []
        for song in songs {
            if let known = keptMarks.marks(for: song) {
                marks[song.id] = known
            } else {
                unknown.append(song)
            }
        }
        marking?.cancel()
        guard !unknown.isEmpty else { return }
        marking = Task { [weak self] in
            for start in stride(from: 0, to: unknown.count, by: 20) {
                let few = Array(unknown[start..<min(start + 20, unknown.count)])
                let found = await Task.detached(priority: .utility) { few.map { ($0, Marks.read($0)) } }.value
                guard let self, !Task.isCancelled else { return }
                for (song, marks) in found {
                    self.marks[song.id] = marks
                    self.keptMarks.keep(marks, for: song)
                }
            }
            guard let self, !Task.isCancelled else { return }
            self.appData.marks = self.keptMarks
        }
    }

    private func remember() {
        var settings = AppData.Settings()
        settings.chosenFolders = chosenFolders.map(\.path)
        settings.lastPlace = placeID
        settings.lastLibrary = libraryID
        settings.sheetZoom = sheetZoom == Self.usualSheetZoom ? nil : sheetZoom
        settings.listsSplit = listsSplit == Self.usualListsSplit ? nil : listsSplit
        settings.listsWidth = listsWidth == Self.usualListsWidth ? nil : listsWidth
        settings.sheetChordColour = sheetChordColour
        settings.textChordColour = textChordColour
        settings.sheetLineColour = sheetLineColour
        settings.textZoom = textZoom == Self.usualTextZoom ? nil : textZoom
        settings.guideShown = guideShown ? true : nil
        appData.settings = settings
    }
}
