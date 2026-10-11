// The words of a text box: the plain text of the RTF the file keeps them in. Held to
// testdata/vectors/rtf-text.json.
//
// A chord's place is a number counted along this plain text, so both apps must get the
// same plain text from the same RTF, unit for unit. spec/01-file-format.md ("The words,
// and counting along them") has the rules as a table; this is a port of the reader they
// were taken from, reference/simple-presenter/src/rtf.cpp, with everything about fonts,
// colours and sizes left out. That reader builds a model of the formatted text. This one
// only wants the characters, so it keeps no more of an RTF's state than decides whether
// a character is words: which destination a group is, and how many characters to pass
// over after a `\u`.
//
// Why a reader of our own, when AppKit has one (NSAttributedString reads RTF). The rules
// are to have no AppKit in them; the Windows app has nothing native to trust and must
// port this same reader, so two ports of one reader agree by construction where AppKit's
// and a port might not; and AppKit's is free to change with macOS. mac/NOTES.md has what
// was found when the two were set side by side on real songs.
//
// Reading is nearly all of it. The apps never write RTF, with one exception: the
// characters that chords with no words hang on (spec/01-file-format.md, "Chords with no
// words"). That is at the foot of this file, and it is done by changing bytes in place:
// the reader says which bytes of the RTF each character of the plain text was written
// with, the bytes of one line's characters are taken out and others put there, and
// every other byte of the RTF is left exactly as it was. Nothing here reads an RTF into
// a model and writes it out again in its own words, as the reference does; that would
// rewrite the RTF of someone's library for the sake of one line.
//
// What is a guess. The dialect in every file to hand is the one Apple's text system
// writes. The other usual spellings (`\par`, `\line`, bytes written straight in) are
// taken in because ProPresenter on Windows may write them; mac/NOTES.md says what one
// song written there was found to have.

public enum RTFWords {
    /// The plain text of an RTF: its lines joined by single line breaks.
    public static func words<Bytes: Sequence>(ofRTF rtf: Bytes) -> Words where Bytes.Element == UInt8 {
        var reader = Reader(rtf: Array(rtf))
        reader.read()
        return Words(units: reader.text)
    }
}

/// Windows code page 1252 differs from Latin-1 only in 0x80 to 0x9F.
private let cp1252High: [UInt16] = [
    0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
    0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
    0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
    0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178,
]

private func fromCp1252(_ byte: UInt8) -> UInt16 {
    (0x80...0x9F).contains(byte) ? cp1252High[Int(byte) - 0x80] : UInt16(byte)
}

private func isLetter(_ byte: UInt8) -> Bool {
    (0x41...0x5A).contains(byte) || (0x61...0x7A).contains(byte)
}

/// U+2028, which `\line` is read as, and which is a line break in the plain text.
private let lineSeparator: UInt16 = 0x2028

/// Where one unit of the plain text is written in the RTF.
private struct Written {
    /// Its bytes: from the first, to one past the last. A control word's are its
    /// backslash, its letters and number, and the space that ends it if it has one.
    var from: Int
    var to: Int
    /// A control word with no space after it ends exactly where this begins: a plain
    /// space put in here would be taken for that word's end, and would not be text.
    var openBefore: Bool
    /// This is itself such a control word: the same goes for a space put in after it.
    var openAfter: Bool
    /// How many groups are open where it is written.
    var depth: Int
}

private struct Reader {
    /// What the text of a group is: words, or one of the tables and lists that are not.
    /// (The reference tells the font table and the two colour tables apart, to read them.
    /// Nothing in any of them is words, so here they are all `notWords`.)
    enum Destination { case body, notWords }

    struct State {
        var destination = Destination.body
        /// How many characters after a `\u` stand in for it, for readers that do not
        /// know `\u`, and are to be passed over (`\ucN`). One, unless told otherwise.
        var unicodeSkip = 1
        /// A `\*` has been met in this group: the next control word names a destination
        /// that a reader may skip if it does not know it.
        var starred = false
    }

    let rtf: [UInt8]
    var pos = 0
    var state = State()
    var stack: [State] = []
    var pendingSkip = 0
    var text: [UInt16] = []
    /// Where each unit of `text` is written: as many of these as of those.
    var written: [Written] = []
    /// Where the last control word that had no space after it ended.
    var openEnd = -1

    init(rtf: [UInt8]) {
        self.rtf = rtf
    }

    mutating func read() {
        while pos < rtf.count {
            let c = rtf[pos]
            pos += 1
            switch c {
            case UInt8(ascii: "{"):
                stack.append(state)
                state.starred = false
                pendingSkip = 0
            case UInt8(ascii: "}"):
                if let outer = stack.popLast() { state = outer }
                pendingSkip = 0
            case UInt8(ascii: "\\"):
                control()
            case 0x0D, 0x0A:
                // A bare line end in the file is nothing.
                break
            default:
                if c < 0x80 {
                    character(UInt16(c), from: pos - 1)
                } else {
                    pos -= 1
                    rawHighBytes()
                }
            }
        }
        // (The reference ends by making a last paragraph of what follows the last break,
        // even when that is nothing, so that text ending with a break ends with an empty
        // line. With each break written as it is met, that is already so.)
        text = text.map { $0 == lineSeparator ? Words.lineBreak : $0 }
    }

    /// Strict RTF escapes everything above ASCII, but files written by other tools have
    /// UTF-8 written straight in. A run of high bytes is UTF-8 if it is valid as that,
    /// else it is read by the code page.
    private mutating func rawHighBytes() {
        let start = pos
        while pos < rtf.count && rtf[pos] >= 0x80 { pos += 1 }
        let bytes = rtf[start..<pos]
        if let decoded = String(validating: bytes, as: UTF8.self) {
            // (Each character's units are written with that character's bytes: one
            // from beyond the first 65,536 is two units and one stretch of bytes.)
            var at = start
            for scalar in decoded.unicodeScalars {
                let next = at + UTF8.width(scalar)
                for unit in UTF16.encode(scalar)! { character(unit, from: at, to: next) }
                at = next
            }
        } else {
            for (n, byte) in bytes.enumerated() { character(fromCp1252(byte), from: start + n, to: start + n + 1) }
        }
    }

    private mutating func control() {
        guard pos < rtf.count else { return }
        let c = rtf[pos]
        // Where its backslash is, and whether a control word with no space after it
        // ends just there (asked now: this may be such a word itself, and would then
        // be the last one).
        let from = pos - 1
        let open = from == openEnd

        if !isLetter(c) {
            pos += 1
            switch c {
            case UInt8(ascii: "'"):
                // Two hexadecimal digits: a character of the code page. What is not two
                // such digits is read as nought. (The reference leaves this to Qt's reader
                // of numbers, which is believed to do the same but for a sign or a space
                // beside one digit. Not tried, and nothing is known to write such a thing.)
                var byte: UInt8 = 0
                if pos + 2 <= rtf.count, let value = UInt8(String(decoding: rtf[pos..<pos + 2], as: UTF8.self), radix: 16),
                   rtf[pos] != UInt8(ascii: "+"), rtf[pos] != UInt8(ascii: "-") {
                    byte = value
                }
                pos += 2
                character(fromCp1252(byte), from: from, openBefore: open)
            case UInt8(ascii: "*"):
                state.starred = true
            case 0x0A, 0x0D:
                // Apple's text system writes a paragraph break as a backslash at the end
                // of the line. This is how ProPresenter writes the end of every line.
                if state.destination == .body { endParagraph(from: from, openBefore: open) }
            case UInt8(ascii: "~"):
                character(0x00A0, from: from, openBefore: open)
            case UInt8(ascii: "_"):
                character(0x2011, from: from, openBefore: open)
            case UInt8(ascii: "-"):
                break
            default:
                // `\\`, `\{`, `\}`: the character itself.
                character(UInt16(c), from: from, openBefore: open)
            }
            return
        }

        let wordStart = pos
        while pos < rtf.count && isLetter(rtf[pos]) { pos += 1 }
        let word = String(decoding: rtf[wordStart..<pos], as: UTF8.self)

        let paramStart = pos
        if pos < rtf.count && rtf[pos] == UInt8(ascii: "-") { pos += 1 }
        while pos < rtf.count && (0x30...0x39).contains(rtf[pos]) { pos += 1 }
        let hasParam = pos > paramStart
        // (A number too large to hold is nought, as it is to the reference.)
        let param = hasParam ? Int(Int32(String(decoding: rtf[paramStart..<pos], as: UTF8.self)) ?? 0) : 0
        // A space ends a control word and is not text.
        if pos < rtf.count && rtf[pos] == UInt8(ascii: " ") {
            pos += 1
        } else {
            openEnd = pos
        }

        controlWord(word, param, from: from, openBefore: open)
    }

    private mutating func controlWord(_ word: String, _ param: Int, from: Int, openBefore: Bool) {
        // The first control word after `{\*` names an optional destination. None of
        // those is words.
        if state.starred {
            state.starred = false
            state.destination = .notWords
            return
        }
        guard state.destination == .body else { return }

        switch word {
        case "fonttbl", "colortbl", "info", "stylesheet", "pict", "header", "footer", "listtable", "listoverridetable":
            state.destination = .notWords
        case "par":
            endParagraph(from: from, openBefore: openBefore)
        case "line":
            character(lineSeparator, from: from, openBefore: openBefore)
        case "tab":
            character(0x0009, from: from, openBefore: openBefore)
        case "u":
            // The UTF-16 unit of that number; RTF writes those over 32767 as negative.
            // Two of these for one character (an emoji) are two units, and two places.
            let skip = state.unicodeSkip
            character(UInt16(truncatingIfNeeded: param < 0 ? param + 65536 : param), from: from, openBefore: openBefore)
            pendingSkip = skip
        case "uc":
            state.unicodeSkip = param
        default:
            // Any other control word is format.
            break
        }
    }

    /// A character that was written with the bytes from `from` to where the reading has
    /// got to, or to `to`. `openBefore` is given by what is itself a control word.
    private mutating func character(_ unit: UInt16, from: Int, to: Int? = nil, openBefore: Bool? = nil) {
        if pendingSkip > 0 {
            pendingSkip -= 1
            return
        }
        if state.destination == .body {
            text.append(unit)
            note(from: from, to: to ?? pos, openBefore: openBefore ?? (from == openEnd))
        }
    }

    private mutating func endParagraph(from: Int, openBefore: Bool) {
        text.append(Words.lineBreak)
        note(from: from, to: pos, openBefore: openBefore)
    }

    private mutating func note(from: Int, to: Int, openBefore: Bool) {
        written.append(Written(from: from, to: to, openBefore: openBefore, openAfter: to == openEnd, depth: stack.count))
    }
}

// ---- Writing: the characters that chords with no words hang on, and nothing else
//
// spec/01-file-format.md, "Chords with no words" and "Chords onto a blank slide". NEW:
// the reference does neither of these this way (see the head of this file), and
// testdata/vectors/stand-ins-in-rtf.json, which holds both, was made by
// tools/make-fixtures.py, the reference for them.
//
// Both answer with nothing where the thing cannot be done cleanly, and both read back
// what they made before they answer with it: an RTF that comes from here has exactly
// the plain text that was meant. Whoever asks refuses the change on nothing.

extension RTFWords {
    /// The bytes these characters are written with: a plain space as itself, and the
    /// two that stand in for words (U+200B, and the U+2001 between two of them) as
    /// Apple's text system writes any character above ASCII, `\uc0\uN ` with a space to
    /// end it. Nothing else is ever written here, and anything else is nothing.
    static func bytes(of standIns: Words) -> [UInt8]? {
        var bytes: [UInt8] = []
        for unit in standIns.units {
            switch unit {
            case 0x0020: bytes.append(0x20)
            case zeroWidthSpace, emQuad: bytes += Array("\\uc0\\u\(unit) ".utf8)
            default: return nil
            }
        }
        return bytes
    }

    /// The RTF with one line of its plain text made these stand-ins, and every byte that
    /// is not that line's characters left as it was.
    ///
    ///   - The line's characters are found by the bytes each was written with.
    ///   - What the line has already of what it is to become, from its start, is kept
    ///     as it is written. The characters after that are taken out: their own bytes,
    ///     and none of the control words that lie between them, which say how what
    ///     follows the line is set (in a song from Multitracks every other stand-in is
    ///     in another font). What is still to come is put where the first of those
    ///     was, or after the line's last character if none was taken out.
    ///   - A plain space put in just after a control word that has no space of its own
    ///     gets one before it, or it would be that word's end and not text.
    ///
    /// Nothing, if the text has no such line; if the line is empty, there being no
    /// character to say where it is; if two of the line's characters were written with
    /// the same bytes; or if what was made does not read back as the same text with
    /// that line changed and nothing else.
    public static func rtf<Bytes: Sequence>(_ rtf: Bytes, withLine line: Int, become standIns: Words) -> [UInt8]? where Bytes.Element == UInt8 {
        let rtf = Array(rtf)
        var reader = Reader(rtf: rtf)
        reader.read()
        let text = reader.text
        // The line: from `start`, to `end`.
        var start = 0
        for _ in 0..<max(0, line) {
            guard let next = text[start...].firstIndex(of: Words.lineBreak) else { return nil }
            start = next + 1
        }
        guard line >= 0 else { return nil }
        let end = text[start...].firstIndex(of: Words.lineBreak) ?? text.count
        guard end > start else { return nil }

        var same = 0
        while start + same < end && same < standIns.length && text[start + same] == standIns[same] { same += 1 }
        let coming = Words(units: Array(standIns.units[same...]))
        guard var put = bytes(of: coming) else { return nil }
        let going = reader.written[(start + same)..<end]
        for (one, next) in zip(going, going.dropFirst()) where next.from < one.to { return nil }

        // Where what is coming goes, and whether a space there would be lost.
        let at: Int
        let open: Bool
        if let first = going.first {
            at = first.from
            open = first.openBefore
        } else {
            at = reader.written[end - 1].to
            open = reader.written[end - 1].openAfter
        }
        if open && put.first == 0x20 { put.insert(0x20, at: 0) }

        var made = Array(rtf[..<at]) + put
        var from = at
        for one in going {
            made += rtf[from..<one.from]
            from = one.to
        }
        made += rtf[from...]

        let meant = Array(text[..<(start + same)]) + coming.units + Array(text[end...])
        guard words(ofRTF: made).units == meant else { return nil }
        return made
    }

    /// Where a line's characters are written: from the first byte of its first to one
    /// past the last byte of its last. Nothing outside it is changed by the function
    /// above, which is what the tests hold it to. Nothing for a line that is not there,
    /// or is empty.
    static func span<Bytes: Sequence>(ofLine line: Int, of rtf: Bytes) -> Range<Int>? where Bytes.Element == UInt8 {
        var reader = Reader(rtf: Array(rtf))
        reader.read()
        var start = 0
        guard line >= 0 else { return nil }
        for _ in 0..<line {
            guard let next = reader.text[start...].firstIndex(of: Words.lineBreak) else { return nil }
            start = next + 1
        }
        let end = reader.text[start...].firstIndex(of: Words.lineBreak) ?? reader.text.count
        guard end > start else { return nil }
        return reader.written[start].from..<reader.written[end - 1].to
    }

    /// A new RTF for a text box that has no words: the opening of another's, up to
    /// where its words begin (its fonts, its colours and how its first words are set),
    /// then these stand-ins, then as many closing brackets as that opening left open.
    ///
    /// Nothing, if the other has no character at all to say where its words begin, or
    /// if what was made does not read back as the stand-ins and nothing else.
    public static func rtf<Bytes: Sequence>(withTheOpeningOf model: Bytes, then standIns: Words) -> [UInt8]? where Bytes.Element == UInt8 {
        let model = Array(model)
        var reader = Reader(rtf: model)
        reader.read()
        guard let first = reader.written.first, var put = bytes(of: standIns) else { return nil }
        if first.openBefore && put.first == 0x20 { put.insert(0x20, at: 0) }
        let made = Array(model[..<first.from]) + put + Array(repeating: UInt8(ascii: "}"), count: first.depth)
        guard words(ofRTF: made).units == standIns.units else { return nil }
        return made
    }
}
