import QtQuick

// Opens each presentation of the library in Simple Presenter's editor and prints what
// its chord editors are handed (PresentationEditor::song), then stops.
QtObject {
    id: dump

    property var names: []
    property int at: 0
    property Timer clock: Timer {
        interval: 700
        onTriggered: dump.next()
    }

    function next() {
        if (editing) {
            const blocks = editScreen.editor.song().map(b => ({ slide: b.row, group: b.group, startsItsGroup: b.groupStart, element: b.element,
                                                               words: b.text, chords: b.chords, blank: b.blank }))
            testInput.say("SONG " + JSON.stringify({ file: names[at] + ".pro", key: editScreen.editor.key, textBoxes: blocks }))
            stopEditing()
            ++at
            clock.start()
            return
        }
        if (at >= names.length) {
            testInput.quit()
            return
        }
        startEditing(documents.find(d => d.name === names[at]))
        clock.start()
    }

    function run() {
        openLibrary(catalog.libraries[0].path)
        names = documents.map(d => d.name)
        clock.start()
    }
}
