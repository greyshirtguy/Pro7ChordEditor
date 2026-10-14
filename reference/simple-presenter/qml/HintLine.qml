import QtQuick
import SimplePresenterApp

// The line of hints under the chord sheet, drawn: what src/sheethints.h says just now,
// its keys drawn as keys. ChordSheet.qml says what is going on and hands over what is to
// be said (`hints`); this draws as many of them as there is room for, the first first.
// It is one line, but for the two sentences on copying and pasting where there is not
// room for them side by side: then it is two, and the sheet over it that much shorter
// while they are said.
//
// What it says changes as often as the pointer crosses a chord, and so:
//   - how many hints go in, and how tall the line is, is worked out (SheetHints.fit)
//     and not asked of what is in it; it asks for no width, and what does not fit is
//     cut off at its right edge, pushing nothing about;
//   - hints the same as those it says already change nothing at all, and nothing is
//     made again (the sheet hands over a new list for every place the spot goes to);
//   - the line's height changes only where it is to be two lines or one again.
//
// IT IS BLUE, TO BE SEEN, AND LIGHTS UP WHEN WHAT IT SAYS CHANGES (the Mac and Windows
// chord editors' rule 53h; the maintainer's, 2026-10-14: "Let's make the tips MORE
// visible! - I'm thinking a light blue theme down there! (and maybe make it light up
// with a residual fading glow (1-2 seconds) when the tip text changes). Make the key
// shapes a bit brighter or different color to stand out a little!"). The colours are
// their page's set for a dark system, this app being dark. What lights up is a layer
// of the lit colour over the ground, its opacity going up in about a tenth of a second,
// staying a quarter, and fading over a little more than a second; a change while it is
// lit lights it again from however lit it is. Nothing is laid out by it, and nothing is
// drawn once it has faded. (Where a person has asked their system for less motion it
// is meant not to light up; Qt tells an app nothing of that on Linux, so here it
// always does. It is a fade of colour, and nothing moves.)
Rectangle {
    id: line

    // What is to be said, as SheetHints.hints gives it: [{ lead, keys, says, needed }]
    property var hints: []
    // What is said: the hints as last handed over that differed from those before,
    // and how they fit
    property var shown: []
    property string shownAs
    property var fitted: ({ count: 0, stacked: false, size: 1, height: SheetHints.sizes.height })
    readonly property var sizes: SheetHints.sizes
    readonly property real size: fitted.size
    // What it says, as it would be read out: for a test, and for whoever asks
    readonly property string says: shown.slice(0, fitted.count).map(hint => [hint.lead].concat(Array.from(hint.keys), [hint.says]).filter(part => part !== "").join(" ")).join(" · ")
    // How many times it has lit up, for a test to count
    property int litUp: 0

    function take(lightUp) {
        const as = JSON.stringify(hints)
        if (as === shownAs)
            return
        shownAs = as
        shown = hints
        refit()
        if (lightUp) {
            ++litUp
            glow.restart()
        }
    }

    function refit() {
        fitted = SheetHints.fit(shown, width)
    }

    onHintsChanged: take(true)
    onWidthChanged: refit()
    Component.onCompleted: take(false)

    height: fitted.height
    clip: true
    color: "#1d477c"

    // Lit, when what it says has just changed
    Rectangle {
        id: lit

        objectName: "hintGlow"
        anchors.fill: parent
        color: "#3a82e0"
        opacity: 0
    }

    SequentialAnimation {
        id: glow

        NumberAnimation {
            target: lit
            property: "opacity"
            to: 1
            duration: 120
            easing.type: Easing.OutQuad
        }
        PauseAnimation {
            duration: 250
        }
        NumberAnimation {
            target: lit
            property: "opacity"
            to: 0
            duration: 1300
            easing.type: Easing.OutQuad
        }
    }

    // The line along its top
    Rectangle {
        width: parent.width
        height: 1
        color: "#3565a3"
    }

    // The hints: side by side, or the two on copying one under the other
    Grid {
        x: line.sizes.sides
        anchors.verticalCenter: parent.verticalCenter
        columns: line.fitted.stacked ? 1 : Math.max(1, line.fitted.count)
        columnSpacing: line.sizes.between * line.size
        rowSpacing: line.sizes.lines * line.size

        Repeater {
            model: line.shown.slice(0, line.fitted.count)

            // One hint: its words before its keys, its keys, and what it says
            Row {
                id: hint

                required property var modelData

                spacing: line.sizes.within * line.size

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    visible: hint.modelData.lead !== ""
                    color: "#e3efff"
                    font.pixelSize: Math.round(line.sizes.wordSize * line.size)
                    text: hint.modelData.lead
                }

                Repeater {
                    model: hint.modelData.keys

                    // A key, drawn as one: its sign in a small box with rounded corners,
                    // brighter than the line it is on, so that the keys are what is seen
                    // first
                    Rectangle {
                        required property string modelData

                        anchors.verticalCenter: parent.verticalCenter
                        width: Math.max(line.sizes.keyLeast * line.size, Math.ceil(sign.implicitWidth) + 2 * line.sizes.keyPad * line.size)
                        height: line.sizes.keyHeight * line.size
                        radius: 4.5 * line.size
                        color: "#4f90e6"
                        border.width: 1
                        border.color: "#b4d4ff"

                        Text {
                            id: sign

                            anchors.centerIn: parent
                            color: "#ffffff"
                            font.pixelSize: Math.round(line.sizes.keySize * line.size)
                            font.weight: Font.DemiBold
                            text: parent.modelData
                        }
                    }
                }

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    color: "#e3efff"
                    font.pixelSize: Math.round(line.sizes.wordSize * line.size)
                    text: hint.modelData.says
                }
            }
        }
    }
}
