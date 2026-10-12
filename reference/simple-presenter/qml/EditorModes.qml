import QtQuick
import QtQuick.Shapes

// The switch between the editor's three ways of working on a presentation, in the
// middle of its toolbar: the slides, as ProPresenter's editor has them; the chord
// sheet; and the song as ChordPro text (Editor.qml, `mode`).
//
// It is three large buttons side by side and not three small ones at the toolbar's
// end, which it was at first. That a song's chords can be worked on here at all, and in
// two ways, is the first thing to know about them, and as small buttons at the edge it
// was too easy to miss: so it was found in the chord editor that was made of this one
// for the Mac, where the same switch began small and was moved to the middle and made
// like this, and this one follows it. The one that is on is filled with the app's
// orange, which slides from one button to the other when another is clicked.
//
// Each has a small picture, drawn here: ProPresenter has no such switch, and so no
// pictures for it.
//
//   Slides     a slide with two lines of words on it
//   Chords     a chord's bubble in outline with a plus in it, its tail coming down to
//              a line of words: the sheet's own spot, which is what is looked at and
//              pointed with there
//   ChordPro   a chord as it is typed there: a C in square brackets
Rectangle {
    id: modes

    // "slides", "chords" or "chordpro": the one that is on
    property string mode: "slides"
    // What the button under the pointer does, in words, or ""
    readonly property string hint: slides.hovered ? slides.hint : chords.hovered ? chords.hint : chordPro.hovered ? chordPro.hint : ""
    readonly property color accentColor: "#ff8a1f"
    // The orange is sliding to a button that was clicked. (Only then: where the editor
    // changes its way of working by itself, as when it is opened or closed, nothing is
    // to slide. An animation runs whether or not it can be seen, and draws while it
    // does.)
    property bool sliding: false
    // Without its pictures: for a toolbar too narrow for the switch with them. And
    // how wide it is with them, whichever way it is drawn, to tell that by.
    property bool compact: false
    readonly property real widest: slides.full + chords.full + chordPro.full + 2 * row.spacing + 6

    signal picked(string mode)

    width: row.width + 6
    height: 36
    radius: 11
    color: "#34363c"

    // The orange, under the button that is on
    Rectangle {
        readonly property Item under: modes.mode === "chords" ? chords : modes.mode === "chordpro" ? chordPro : slides

        x: row.x + under.x
        y: 3
        width: under.width
        height: 30
        radius: 8
        color: modes.accentColor

        Behavior on x {
            enabled: modes.sliding

            NumberAnimation {
                duration: 180
                easing.type: Easing.OutCubic
                onRunningChanged: {
                    if (!running)
                        modes.sliding = false
                }
            }
        }

        Behavior on width {
            enabled: modes.sliding

            NumberAnimation {
                duration: 180
                easing.type: Easing.OutCubic
            }
        }
    }

    // One of the three: its picture, which is whatever is put in it, and its name
    component Choice: Item {
        id: choice

        property string name
        property string label
        property string hint
        readonly property bool on: modes.mode === name
        readonly property bool hovered: choiceMouse.containsMouse
        // Dark on the orange, light on the rest
        readonly property color ink: on ? "#1b1c1f" : choiceMouse.containsMouse ? "#f0f0f0" : "#c8cacf"
        default property alias picture: pictured.data
        // How wide it is with its picture
        readonly property real full: 14 + pictured.width + 7 + word.implicitWidth + 14

        width: modes.compact ? 14 + word.implicitWidth + 14 : full
        height: 30

        Item {
            id: pictured

            x: 14
            anchors.verticalCenter: parent.verticalCenter
            width: childrenRect.width
            height: 18
            visible: !modes.compact
        }

        Text {
            id: word

            x: modes.compact ? 14 : pictured.x + pictured.width + 7
            anchors.verticalCenter: parent.verticalCenter
            anchors.verticalCenterOffset: -0.5
            color: choice.ink
            font.pixelSize: 13
            font.weight: Font.DemiBold
            text: choice.label
        }

        MouseArea {
            id: choiceMouse

            anchors.fill: parent
            hoverEnabled: true
            onClicked: {
                if (choice.on)
                    return
                modes.sliding = true
                modes.picked(choice.name)
            }
        }
    }

    // A picture drawn as lines, and for some a part filled in, in a box 22 by 18
    component Glyph: Shape {
        id: glyph

        property color ink
        property string lines
        property string thin
        property string solid
        property string faint

        width: 22
        height: 18
        preferredRendererType: Shape.CurveRenderer

        ShapePath {
            strokeColor: glyph.ink
            strokeWidth: 1.5
            fillColor: "transparent"
            capStyle: ShapePath.RoundCap
            joinStyle: ShapePath.RoundJoin

            PathSvg {
                path: glyph.lines
            }
        }

        ShapePath {
            strokeColor: glyph.thin !== "" ? glyph.ink : "transparent"
            strokeWidth: 1.3
            fillColor: "transparent"
            capStyle: ShapePath.RoundCap

            PathSvg {
                path: glyph.thin
            }
        }

        ShapePath {
            strokeColor: "transparent"
            strokeWidth: 0
            fillColor: glyph.solid !== "" ? glyph.ink : "transparent"

            PathSvg {
                path: glyph.solid
            }
        }

        ShapePath {
            strokeColor: "transparent"
            strokeWidth: 0
            fillColor: glyph.faint !== "" ? Qt.rgba(glyph.ink.r, glyph.ink.g, glyph.ink.b, 0.5) : "transparent"

            PathSvg {
                path: glyph.faint
            }
        }
    }

    Row {
        id: row

        x: 3
        y: 3
        spacing: 2

        Choice {
            id: slides

            objectName: "slidesMode"
            name: "slides"
            label: "Slides"
            hint: "The slides: move, size and style what is on each"

            // A slide, and two lines of words in the middle of it
            Glyph {
                ink: slides.ink
                lines: "M 4 2.5 H 18 A 2 2 0 0 1 20 4.5 V 13.5 A 2 2 0 0 1 18 15.5 H 4 A 2 2 0 0 1 2 13.5 V 4.5 A 2 2 0 0 1 4 2.5 Z"
                thin: "M 7 7.4 H 15 M 9 10.6 H 13"
            }
        }

        Choice {
            id: chords

            objectName: "chordsMode"
            name: "chords"
            label: "Chords"
            hint: "The song's chords: point at a word and press 1 to 7, or type a chord; drag chords about"

            // The spot: a bubble in outline with a plus in it, its tail down to a
            // line of words, and a shorter line, fainter, under that
            Glyph {
                ink: chords.ink
                lines: "M 4.8 0.9 H 10.2 A 2.6 2.6 0 0 1 12.8 3.5 V 5.9 A 2.6 2.6 0 0 1 10.2 8.5 H 4.8 A 2.6 2.6 0 0 1 2.2 5.9 V 3.5 A 2.6 2.6 0 0 1 4.8 0.9 Z M 4.8 8.5 V 11.4"
                thin: "M 5.5 4.7 H 9.5 M 7.5 2.7 V 6.7"
                solid: "M 2.75 11.9 H 19.25 A 1.15 1.15 0 0 1 19.25 14.2 H 2.75 A 1.15 1.15 0 0 1 2.75 11.9 Z"
                faint: "M 2.75 15.6 H 12.85 A 1.15 1.15 0 0 1 12.85 17.9 H 2.75 A 1.15 1.15 0 0 1 2.75 15.6 Z"
            }
        }

        Choice {
            id: chordPro

            objectName: "chordProMode"
            name: "chordpro"
            label: "ChordPro"
            hint: "The song as ChordPro text: type chords in square brackets among the words"

            // A chord as it is typed there. (In the app's own letters, and in two
            // weights of them that the toolbar has already: this is made as the app
            // starts, and letters of another kind, the typewriter's that the ChordPro
            // text itself is in, would have their fonts found and read then, which is
            // some hundredths of a second of every start for a picture.)
            Row {
                height: 18
                spacing: 1

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    anchors.verticalCenterOffset: -1
                    color: chordPro.ink
                    font.pixelSize: 14
                    font.weight: Font.Medium
                    text: "["
                }

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    color: chordPro.ink
                    font.pixelSize: 14
                    font.bold: true
                    text: "C"
                }

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    anchors.verticalCenterOffset: -1
                    color: chordPro.ink
                    font.pixelSize: 14
                    font.weight: Font.Medium
                    text: "]"
                }
            }
        }
    }
}
