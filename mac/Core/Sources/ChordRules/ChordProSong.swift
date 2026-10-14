// A song read from a ChordPro file: what it is called, and its parts with their lines
// and chords. Held to testdata/vectors/parse-song.json, which has what this gives for
// each file of testdata/songs. What is done with it (making a presentation) is
// spec/09-chordpro-files.md and milestone M9.
//
// What is read. The directives that say what the song is are kept ({title}, {artist},
// {key}, {copyright}, {ccli}, and their short forms). Those that mark its parts make
// sections ({start_of_verse}, {start_of_chorus}, {start_of_bridge} and their ends and
// short forms, with a label if one is given; {comment: Verse 1}, and a line that is only
// "Verse 1:" or "[Verse 1]" too, since most files found in the wild mark their parts
// that way). An empty line ends a section that has no name, so that a file with no
// directives at all still comes in as its verses. Tablature and anything else in a
// {start_of_...} this does not know is left out; so is a line starting "#".
//
// The reference finds a directive and a heading with two regular expressions. They are
// done by hand here, so that what they match does not hang on which regular expression
// library a platform has (Qt's, Foundation's and .NET's differ over what a space and a
// letter are). `directive` and `headingOf` say what each matches. One thing is narrower
// than the reference and is so on purpose (spec/02-chord-rules.md, "To the letter"): a
// heading's words are matched in the plain letters A to Z, of either case. Qt's
// expression would also take a long s for an "s" and the Kelvin sign for a "k".
//
// (`Song` in the reference. Here that name is the layer above this one.)

extension Chords {
    /// One part of a song: "Verse 1", "Chorus", or "" where the file names none.
    public struct Section: Hashable, Sendable {
        public var name: String
        public var lines: [Line]

        public init(name: String = "", lines: [Line] = []) {
            self.name = name
            self.lines = lines
        }
    }

    /// A song as a ChordPro file has it.
    public struct ParsedSong: Hashable, Sendable {
        public var title = ""
        public var artist = ""
        public var key = ""
        public var copyright = ""
        public var ccli = ""
        public var sections: [Section] = []

        public init() {}
    }

    /// Reads ChordPro text.
    public static func parseSong(_ text: String) -> ParsedSong {
        ChordRules.parseSong(units(text))
    }

    /// A section's lines dealt into slides of so many lines each (the last may have
    /// fewer). Less than one line to a slide is taken as one.
    public static func slidesOf(_ section: Section, linesPerSlide: Int) -> [[Line]] {
        let each = max(1, linesPerSlide)
        return stride(from: 0, to: section.lines.count, by: each).map {
            Array(section.lines[$0..<min($0 + each, section.lines.count)])
        }
    }
}

/// The white space a regular expression's `\s` means in the reference: the six of ASCII.
private func isPlainSpace(_ unit: UInt16) -> Bool {
    unit == 0x20 || (0x09...0x0D).contains(unit)
}

/// A directive: "{name}" or "{name: value}", and nothing else on the line. The name is
/// what comes before a colon, a closing brace or a space. Spaces around the name and the
/// value do not count. The reference's expression:
///
///     ^\{\s*([^:}\s]+)\s*(?::\s*(.*?))?\s*\}$
private func directive(_ line: Units) -> (name: Units, value: Units)? {
    guard line.first == u("{"), line.last == u("}"), line.count >= 2 else { return nil }
    let end = line.count - 1
    var i = 1
    while i < end && isPlainSpace(line[i]) { i += 1 }
    let nameStart = i
    while i < end && line[i] != u(":") && line[i] != u("}") && !isPlainSpace(line[i]) { i += 1 }
    let name = Array(line[nameStart..<i])
    guard !name.isEmpty else { return nil }
    while i < end && isPlainSpace(line[i]) { i += 1 }
    if i == end { return (name, []) }
    guard line[i] == u(":") else { return nil }
    i += 1
    while i < end && isPlainSpace(line[i]) { i += 1 }
    var valueEnd = end
    while valueEnd > i && isPlainSpace(line[valueEnd - 1]) { valueEnd -= 1 }
    return (name, Array(line[i..<valueEnd]))
}

/// The words a line that only names a part can start with.
private let headingWords = [
    "verse", "chorus", "pre-chorus", "pre chorus", "prechorus", "bridge", "intro", "outro", "ending", "tag", "interlude",
    "instrumental", "refrain", "turnaround", "vamp", "coda", "hook", "post-chorus", "post chorus", "postchorus",
].map(units)

/// "Verse 1", "CHORUS:", "[Bridge]", "Pre-Chorus 2": a line that only names a part, as
/// that name ("VERSE 1" as "Verse 1"); empty if it is not one. It is one of the words
/// above, then perhaps a number (with perhaps a space before it and perhaps one letter
/// after it: "2b"), and nothing more. The reference's expression, without regard to case:
///
///     ^(verse|chorus|pre[- ]?chorus|bridge|...|post[- ]?chorus)( ?\d+[a-z]?)?$
private func headingOf(_ line: Units) -> Units {
    var text = line.trimmed()
    if text.first == u("[") && text.last == u("]") {
        text = text.mid(1, text.count - 2).trimmed()
    }
    if text.last == u(":") {
        text.removeLast()
    }
    let lowered = text.map(asciiLowered)
    guard let word = headingWords.first(where: { lowered.startsWith($0) }) else { return [] }
    var rest = lowered.mid(word.count)
    if !rest.isEmpty {
        if rest.first == u(" ") { rest.removeFirst() }
        let digits = rest.prefix(while: isASCIIDigit).count
        guard digits > 0 else { return [] }
        rest = rest.mid(digits)
        if let letter = rest.first, isASCIILetter(letter) { rest.removeFirst() }
        guard rest.isEmpty else { return [] }
    }
    // "VERSE 1" as "Verse 1": a capital to start, and after a space or a hyphen.
    var name = lowered
    var start = true
    for i in name.indices {
        if start && isASCIILetter(name[i]) {
            name[i] = asciiRaised(name[i])
        }
        start = name[i] == u(" ") || name[i] == u("-")
    }
    return name
}

func parseSong(_ text: Units) -> Chords.ParsedSong {
    var song = Chords.ParsedSong()
    var section = Chords.Section()
    // Inside a part the file marked with a directive, where an empty line is only a gap.
    var marked = false
    var skipping = false
    var counts: [String: Int] = [:]

    func close() {
        if !section.lines.isEmpty {
            song.sections.append(section)
        }
        section = Chords.Section()
        marked = false
    }
    func open(_ kind: String, _ label: String) {
        close()
        marked = true
        if !label.utf16.isEmpty {
            section.name = label
            return
        }
        // Verses are numbered as they come; a chorus or a bridge is not, the first time.
        counts[kind, default: 0] += 1
        let n = counts[kind]!
        section.name = kind == "Verse" || n > 1 ? "\(kind) \(n)" : kind
    }

    // Line ends of every kind are one line end.
    var normal: Units = []
    var i = 0
    while i < text.count {
        if text[i] == 0x0D {
            normal.append(Words.lineBreak)
            if i + 1 < text.count && text[i + 1] == 0x0A { i += 1 }
        } else {
            normal.append(text[i])
        }
        i += 1
    }

    for raw in normal.split(at: Words.lineBreak) {
        let line = raw.trimmed()
        if line.first == u("#") { continue }
        if let (directiveName, directiveValue) = directive(line) {
            let name = string(directiveName).lowercased()
            // What the name starts with is asked of its units, as the reference asks it
            // (spec/02-chord-rules.md, "To the letter"). Swift's own `hasPrefix` goes
            // by whole characters, and to it a name does not start with "start_of_" if
            // an accent is laid over that underline. Nothing writes such a name.
            let nameUnits = Array(name.utf16)
            func starts(_ with: String) -> Bool { nameUnits.starts(with: with.utf16) }
            let valueUnits = directiveValue.trimmed()
            let value = string(valueUnits)
            switch name {
            case "title", "t":
                song.title = value
            case "artist", "subtitle", "st":
                if song.artist.utf16.isEmpty { song.artist = value }
            case "key":
                song.key = string(tidied(valueUnits))
            case "copyright":
                song.copyright = value
            case "ccli":
                song.ccli = value
            case "start_of_verse", "sov":
                open("Verse", value)
            case "start_of_chorus", "soc":
                open("Chorus", value)
            case "start_of_bridge", "sob":
                open("Bridge", value)
            case _ where starts("start_of_") || name == "sot" || name == "sog":
                // Tablature, a grid, or something newer: not words to show.
                close()
                skipping = true
            case _ where starts("end_of_") || (nameUnits.count == 3 && starts("eo")):
                close()
                skipping = false
            case "comment", "c", "ci", "comment_italic":
                let heading = headingOf(valueUnits)
                if !heading.isEmpty {
                    close()
                    section.name = string(heading)
                    marked = true
                }
            default:
                break
            }
            continue
        }
        if skipping { continue }
        if line.isEmpty {
            // Between the parts of a file that marks none, and inside a marked part only
            // a gap.
            if !marked || section.name.utf16.isEmpty { close() }
            continue
        }
        let heading = headingOf(line)
        if !heading.isEmpty {
            close()
            section.name = string(heading)
            marked = false
            continue
        }
        var kept = fromChordPro(line)
        if !kept.text.units.trimmed().isEmpty || !kept.chords.isEmpty {
            if !isPlaceholders(kept.text.units) {
                // Spaces at the ends are of no use on a slide, and the chords move with
                // the words.
                let lead = kept.text.units.prefix(while: isSpace).count
                let trimmed = kept.text.units.trimmed()
                kept.text = Words(units: trimmed)
                for n in kept.chords.indices {
                    kept.chords[n].at = min(max(0, kept.chords[n].at - lead), max(0, trimmed.count - 1))
                }
            }
            section.lines.append(kept)
        }
    }
    close()
    return song
}
