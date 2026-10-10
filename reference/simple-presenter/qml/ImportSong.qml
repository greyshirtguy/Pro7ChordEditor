import QtQuick
import SimplePresenterApp

// The panel a ChordPro file is imported from: what the file was found to hold, how many
// lines of words go on a slide, and Import. It is put up once a file has been chosen
// (see importSong in Main.qml), over the operator window; Cancel, Esc or a click outside
// leaves the library as it was.
//
// Two lines to a slide is what a new song starts at, being what most lyric slides are;
// the number can be changed here before the song comes in. After it is in, the slides
// are ordinary slides and are changed in the editor like any others.
Rectangle {
    id: panel

    required property var win
    // The file chosen, "" while the panel is shut
    property string file: ""
    // What it holds (Chords.describeFile)
    property var found: ({})
    property int lines: 2
    readonly property string library: win.libraryPath
    readonly property string libraryName: library.substring(library.lastIndexOf("/") + 1)
    readonly property int slideCount: (found.sections ?? []).reduce((sum, part) => sum + Math.ceil(part.lines / Math.max(1, lines)), 0)

    signal closed

    function show(path) {
        found = Chords.describeFile(path)
        if (found.error !== "") {
            win.report(found.error)
            return
        }
        lines = 2
        file = path
        forceActiveFocus()
    }

    function close() {
        file = ""
        closed()
    }

    function importIt() {
        const path = file
        file = ""
        win.importSong(path, lines)
        closed()
    }

    visible: file !== ""
    color: "#99000000"
    Keys.onPressed: (event) => {
        if (event.key === Qt.Key_Escape)
            close()
        else if (event.key === Qt.Key_Return || event.key === Qt.Key_Enter)
            importIt()
        event.accepted = true
    }

    MouseArea {
        anchors.fill: parent
        acceptedButtons: Qt.AllButtons
        hoverEnabled: true
        onWheel: (wheel) => wheel.accepted = true
        onClicked: panel.close()
    }

    Rectangle {
        objectName: "importSongPanel"
        anchors.centerIn: parent
        width: 440
        height: content.height + 36
        radius: 10
        color: "#2b2d31"
        border.width: 1
        border.color: "#45484e"

        MouseArea {
            anchors.fill: parent
        }

        Column {
            id: content

            x: 20
            y: 18
            width: parent.width - 40
            spacing: 12

            Text {
                width: parent.width
                elide: Text.ElideRight
                color: "#f0f0f0"
                font.pixelSize: 17
                font.bold: true
                text: (panel.found.title ?? "") !== "" ? panel.found.title : panel.file.substring(panel.file.lastIndexOf("/") + 1)
            }

            Text {
                width: parent.width
                wrapMode: Text.Wrap
                color: "#b4b7bd"
                font.pixelSize: 13
                text: {
                    const parts = panel.found.sections ?? []
                    const named = parts.filter(part => part.name !== "").map(part => part.name)
                    return [(panel.found.artist ?? "") !== "" ? panel.found.artist : "",
                            parts.length + (parts.length === 1 ? " part" : " parts") + (named.length > 0 ? ": " + named.join(", ") : ""),
                            (panel.found.chords ?? 0) + " chords" + ((panel.found.key ?? "") !== "" ? ", in " + panel.found.key
                                                                                                  : ", and the file names no key"),
                            "Into the library " + panel.libraryName].filter(line => line !== "").join("\n")
                }
            }

            Row {
                spacing: 10

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    color: "#e6e6e6"
                    font.pixelSize: 14
                    text: "Lines on a slide"
                }

                NumberField {
                    objectName: "importLinesField"
                    anchors.verticalCenter: parent.verticalCenter
                    width: 64
                    value: panel.lines
                    from: 1
                    to: 12
                    onEdited: (value) => panel.lines = value
                    onFinished: panel.forceActiveFocus()
                }

                Text {
                    anchors.verticalCenter: parent.verticalCenter
                    color: "#9a9da3"
                    font.pixelSize: 13
                    text: "which makes " + panel.slideCount + (panel.slideCount === 1 ? " slide" : " slides")
                }
            }

            Row {
                anchors.right: parent.right
                spacing: 10

                AppButton {
                    text: "Cancel"
                    onClicked: panel.close()
                }

                AppButton {
                    objectName: "importSongButton"
                    text: "Import"
                    onClicked: panel.importIt()
                }
            }
        }
    }
}
