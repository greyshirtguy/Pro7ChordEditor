// The two small pictures that stand before names in the lists at the left of the
// window: an orange one before a library, a grey one before a song (spec/06-library.md,
// rules 7 and 8).
//
// WHOSE THEY ARE. Both are ProPresenter's own, not this app's, and are here at the
// maintainer's asking, so that what is a ProPresenter library and what is a
// presentation look here as they do there:
//
//   - the outline is the one ProPresenter draws before a presentation in its own
//     lists ("PresentationItem", from a file the maintainer supplied, 2026-10-11);
//   - the orange square with that outline on it is ProPresenter's mark. Its three
//     colours were read off a picture of the mark; nothing of the picture itself is
//     kept.
//
// They are Renewed Vision's, and nothing in this repository's licence makes them
// anyone else's. This app is not ProPresenter and has nothing to do with Renewed
// Vision (AGENTS.md). If either is ever to be taken out, this is the one file.
//
// HOW THEY ARE DRAWN. From the outline's path, in code, and not from picture files: a
// list has one for every row, at whatever size the row's words are, on a screen of any
// sharpness, and a path is sharp at all of them and costs nothing to keep.

import SwiftUI

/// The outline that stands for a presentation: a bar, and beside it a screen seen from
/// its side. It fills what it is given, kept square, in whatever it is filled with.
struct PresentationGlyph: Shape {
    /// The outline's path as its file has it, drawn in a square of 200.
    static let drawing = """
        M52.7658902,159.785959 L11.3005931,171.295039 C5.62059887,172.866106 0,168.595351 0,162.693146 \
        L0,37.199155 C0,31.2967302 5.62059887,27.025646 11.3005931,28.5977009 L52.7658902,40.1059032 \
        C55.8061407,40.9512711 57.9119851,43.7096957 57.9119851,46.8685173 L57.9119851,153.024113 \
        C57.9119851,156.182715 55.8061407,158.940591 52.7658902,159.785959 Z \
        M79.8014154,33.3284677 C73.0981966,34.8114846 73.7210242,44.5549539 80.5429238,45.1778912 \
        L133.116026,58.5103316 C139.092887,60.3789241 143.156143,65.9105947 143.156143,72.1689523 \
        L143.156143,126.967019 C143.156143,133.358879 138.89966,138.994299 132.744943,140.72895 \
        L79.4900565,153.749812 C72.6829782,154.373408 72.0452195,164.072084 78.7039741,165.584963 \
        L188.002363,193.747792 C194.142808,195.156373 200,190.484892 200,184.18196 L200,15.8288026 \
        C200,9.55551379 194.202093,4.88402157 188.061648,6.23356914 L79.786594,33.3284677 \
        L79.8014154,33.3284677 Z
        """
    static let square = 200.0

    /// The path, made once.
    static let outline = path(of: drawing)

    func path(in rect: CGRect) -> Path {
        let scale = min(rect.width, rect.height) / Self.square
        let fitted = CGAffineTransform(scaleX: scale, y: scale)
            .concatenating(CGAffineTransform(translationX: rect.midX - Self.square * scale / 2, y: rect.midY - Self.square * scale / 2))
        return Self.outline.applying(fitted)
    }

    /// Reads a path written as a picture file writes one, of the four things this one is
    /// written with: M (move to), L (line to), C (curve to, with its two handles) and Z
    /// (close), each followed by the places it needs, all counted from the picture's
    /// corner. Anything else in it is passed over.
    static func path(of drawing: String) -> Path {
        var path = Path()
        var command: Character = " "
        var numbers: [Double] = []
        var number = ""

        func place(_ at: Int) -> CGPoint {
            CGPoint(x: numbers[at], y: numbers[at + 1])
        }
        func endNumber() {
            if let value = Double(number) {
                numbers.append(value)
            }
            number = ""
            // A command is done as soon as it has the places it needs; more places
            // after it are the same command again.
            switch command {
            case "M" where numbers.count == 2:
                path.move(to: place(0))
                numbers = []
                command = "L"
            case "L" where numbers.count == 2:
                path.addLine(to: place(0))
                numbers = []
            case "C" where numbers.count == 6:
                path.addCurve(to: place(4), control1: place(0), control2: place(2))
                numbers = []
            default:
                break
            }
        }

        for character in drawing {
            switch character {
            case "M", "L", "C":
                endNumber()
                command = character
                numbers = []
            case "Z", "z":
                endNumber()
                path.closeSubpath()
                command = " "
                numbers = []
            case "0"..."9", ".":
                number.append(character)
            case "-":
                // (A minus starts a number, and so ends the one before it.)
                endNumber()
                number = "-"
            default:
                endNumber()
            }
        }
        endNumber()
        return path
    }
}

/// What stands before a song's name: the outline, in grey.
struct PresentationIcon: View {
    var body: some View {
        PresentationGlyph()
            .fill(.secondary)
            .aspectRatio(1, contentMode: .fit)
            .accessibilityHidden(true)
    }
}

/// What stands before a library's name: the outline in white on an orange square with
/// rounded corners, lighter at its top left than at its bottom right.
struct LibraryIcon: View {
    /// The mark's three colours, from its top left corner, its middle and its bottom
    /// right.
    static let colours = [Color(red: 1, green: 0.79, blue: 0), Color(red: 1, green: 0.56, blue: 0), Color(red: 1, green: 0.25, blue: 0)]

    var body: some View {
        GeometryReader { given in
            let side = min(given.size.width, given.size.height)
            ZStack {
                RoundedRectangle(cornerRadius: side * 0.23, style: .continuous)
                    .fill(LinearGradient(colors: Self.colours, startPoint: .topLeading, endPoint: .bottomTrailing))
                // (On the mark the outline is two thirds as wide as the square.)
                PresentationGlyph()
                    .fill(.white)
                    .frame(width: side * 0.66, height: side * 0.66)
            }
            .frame(width: side, height: side)
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
        .aspectRatio(1, contentMode: .fit)
        .accessibilityHidden(true)
    }
}
