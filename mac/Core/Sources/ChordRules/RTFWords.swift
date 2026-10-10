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
// This only READS. The apps never write RTF, with the one exception of a line of chords
// alone (spec/01-file-format.md, "Chords with no words"), which is milestone M7 and is
// done by changing bytes in place, not by anything here.
//
// What is a guess. The dialect in every file to hand is the one Apple's text system
// writes. The other usual spellings (`\par`, `\line`, bytes written straight in) are
// taken in because ProPresenter on Windows may write them; mac/NOTES.md says what one
// song written there was found to have.

public enum RTFWords {
    /// The plain text of an RTF: its lines joined by single line breaks.
    public static func words<Bytes: Sequence>(ofRTF rtf: Bytes) -> Words where Bytes.Element == UInt8 {
        var reader = Reader(rtf: Array(rtf))
        return Words(units: reader.read())
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

    init(rtf: [UInt8]) {
        self.rtf = rtf
    }

    mutating func read() -> [UInt16] {
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
                    character(UInt16(c))
                } else {
                    pos -= 1
                    rawHighBytes()
                }
            }
        }
        // (The reference ends by making a last paragraph of what follows the last break,
        // even when that is nothing, so that text ending with a break ends with an empty
        // line. With each break written as it is met, that is already so.)
        return text.map { $0 == lineSeparator ? Words.lineBreak : $0 }
    }

    /// Strict RTF escapes everything above ASCII, but files written by other tools have
    /// UTF-8 written straight in. A run of high bytes is UTF-8 if it is valid as that,
    /// else it is read by the code page.
    private mutating func rawHighBytes() {
        let start = pos
        while pos < rtf.count && rtf[pos] >= 0x80 { pos += 1 }
        let bytes = rtf[start..<pos]
        if let decoded = String(validating: bytes, as: UTF8.self) {
            for unit in decoded.utf16 { character(unit) }
        } else {
            for byte in bytes { character(fromCp1252(byte)) }
        }
    }

    private mutating func control() {
        guard pos < rtf.count else { return }
        let c = rtf[pos]

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
                character(fromCp1252(byte))
            case UInt8(ascii: "*"):
                state.starred = true
            case 0x0A, 0x0D:
                // Apple's text system writes a paragraph break as a backslash at the end
                // of the line. This is how ProPresenter writes the end of every line.
                if state.destination == .body { endParagraph() }
            case UInt8(ascii: "~"):
                character(0x00A0)
            case UInt8(ascii: "_"):
                character(0x2011)
            case UInt8(ascii: "-"):
                break
            default:
                // `\\`, `\{`, `\}`: the character itself.
                character(UInt16(c))
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
        if pos < rtf.count && rtf[pos] == UInt8(ascii: " ") { pos += 1 }

        controlWord(word, param)
    }

    private mutating func controlWord(_ word: String, _ param: Int) {
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
            endParagraph()
        case "line":
            character(lineSeparator)
        case "tab":
            character(0x0009)
        case "u":
            // The UTF-16 unit of that number; RTF writes those over 32767 as negative.
            // Two of these for one character (an emoji) are two units, and two places.
            let skip = state.unicodeSkip
            character(UInt16(truncatingIfNeeded: param < 0 ? param + 65536 : param))
            pendingSkip = skip
        case "uc":
            state.unicodeSkip = param
        default:
            // Any other control word is format.
            break
        }
    }

    private mutating func character(_ unit: UInt16) {
        if pendingSkip > 0 {
            pendingSkip -= 1
            return
        }
        if state.destination == .body {
            text.append(unit)
        }
    }

    private mutating func endParagraph() {
        text.append(Words.lineBreak)
    }
}
