import QtQuick
import "chordlayout.js" as ChordLayout

// Words with their chords over them, as a stage screen shows a song to the band: what a
// text box of a stage layout draws when it is linked to the words of the live slide (or
// of the next) and has its chords switched on (SlideElement.qml).
//
// It is not the drawing every other text gets (StrokedText). Chords have to stand over
// the syllables they belong to, so the words are drawn a line at a time, each with a
// row for chords above it, in plain QML text. Two things follow, both on purpose:
//   - A line is never broken. Where ProPresenter would wrap a line that is too long for
//     the box, here the whole text is made smaller until its longest line fits, since a
//     chord over a word that has moved to another line is worse than small text.
//   - The words have the element's font, size, colour and alignment, and no more: no
//     outline, no gradient. (The shadow is the element's, and still drawn.)
// How far a chord stands over its words. A row of chords stacked on a row of words, each
// as tall as its font says, leaves a gap between the foot of the chords and the top of
// the capitals that is the chord font's room for tails (which chords hardly have) and
// the word font's room for accents: about a third of the words' height. Half of it is
// taken out (`closer`), so that a chord reads as belonging to the line under it and
// not as floating between two.
//
// If any line of the text has a chord, every line gets a row for chords, so that the
// lines stay evenly apart; a slide with no chords at all is drawn without the rows and
// looks as it would with chords switched off.
//
// The key and the notation are settled before the lines get here (Show.chordLines).
Item {
    id: block

    // [{ text, chords: [{ at, name }] }], a line each
    property var lines: []
    // The element's style, as proconvert gives it: family, size, bold, italic, color,
    // capitals, alignment
    property var style: ({})
    property color chordColor: "white"
    // Output pixels per slide unit
    property real unit: 1
    // How the text suits its size to the box, as the file numbers it (see StrokedText):
    // 3 and 4 let it grow to fill the box; anything else only ever makes it smaller.
    property int fit: 0
    property int verticalAlignment: Qt.AlignVCenter
    property real insetLeft: 0
    property real insetTop: 0
    property real insetRight: 0
    property real insetBottom: 0
    // A chord's size as a part of the words'
    readonly property real chordSize: 0.72
    readonly property bool anyChords: lines.some(line => line.chords.length > 0)
    readonly property real nominal: Math.max(1, (style.size ?? 60) * unit)
    readonly property real roomWidth: Math.max(1, width - (insetLeft + insetRight) * unit)
    readonly property real roomHeight: Math.max(1, height - (insetTop + insetBottom) * unit)
    // The lines measured at the size the element gives its words, and from that how
    // much smaller or larger they are drawn.
    readonly property var natural: {
        let widest = 1
        for (const line of lines)
            widest = Math.max(widest, ChordLayout.place(shownText(line.text), line.chords, wordsNominal, chordsNominal, gapAt(nominal)).width)
        const rows = lines.length * (wordsNominal.height + (anyChords ? chordsNominal.height - closer(wordsNominal, chordsNominal) : 0))
        return { width: widest, height: Math.max(1, rows) }
    }
    readonly property real factor: {
        const fits = Math.min(roomWidth / natural.width, roomHeight / natural.height)
        return fit === 3 || fit === 4 ? fits : Math.min(1, fits)
    }
    readonly property real size: Math.max(1, nominal * factor)

    function shownText(text) {
        return style.capitals ? text.toUpperCase() : text
    }

    function gapAt(pixels) {
        return pixels * 0.25
    }

    // How much nearer its words a row of chords is drawn than plain stacking would put
    // it: half of the empty space between the two (see the top of this file).
    function closer(words, chords) {
        const capitals = words.tightBoundingRect("H").height
        return Math.max(0, 0.5 * (chords.descent + words.ascent - capitals))
    }

    FontMetrics {
        id: wordsNominal

        font.family: block.style.family ?? ""
        font.pixelSize: block.nominal
        font.bold: block.style.bold ?? false
        font.italic: block.style.italic ?? false
    }

    FontMetrics {
        id: chordsNominal

        font.family: block.style.family ?? ""
        font.pixelSize: Math.max(1, block.nominal * block.chordSize)
        font.bold: true
    }

    FontMetrics {
        id: wordsShown

        font.family: block.style.family ?? ""
        font.pixelSize: block.size
        font.bold: block.style.bold ?? false
        font.italic: block.style.italic ?? false
    }

    FontMetrics {
        id: chordsShown

        font.family: block.style.family ?? ""
        font.pixelSize: Math.max(1, block.size * block.chordSize)
        font.bold: true
    }

    Column {
        id: column

        x: block.insetLeft * block.unit
        y: block.insetTop * block.unit
           + (block.verticalAlignment === Qt.AlignTop ? 0
              : block.verticalAlignment === Qt.AlignBottom ? block.roomHeight - height : (block.roomHeight - height) / 2)
        width: block.roomWidth

        Repeater {
            model: block.lines

            Item {
                id: row

                required property var modelData
                readonly property string words: block.shownText(modelData.text)
                readonly property var placed: ChordLayout.place(words, modelData.chords, wordsShown, chordsShown, block.gapAt(block.size))
                // The line, chords and all, goes left, middle or right as the element's
                // text does.
                readonly property real start: (block.style.alignment ?? 1) === 1 ? (column.width - placed.width) / 2
                                              : block.style.alignment === 2 ? column.width - placed.width : 0

                readonly property real wordsTop: block.anyChords ? chordsShown.height - block.closer(wordsShown, chordsShown) : 0

                width: column.width
                height: wordsShown.height + wordsTop

                Repeater {
                    model: row.modelData.chords

                    Text {
                        required property var modelData
                        required property int index

                        x: row.start + row.placed.xs[index]
                        color: block.chordColor
                        font: chordsShown.font
                        text: modelData.name
                    }
                }

                Text {
                    x: row.start
                    y: row.wordsTop
                    color: block.style.color ?? "white"
                    font: wordsShown.font
                    text: row.words
                }
            }
        }
    }
}
