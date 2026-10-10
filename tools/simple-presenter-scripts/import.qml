import QtQuick

// Makes presentations of the ChordPro files in testdata/songs, with Simple Presenter's
// own import (Catalog::importSong), and stops.
//
// One of them, Blank Slides, is then given the blank slides it is named for, in Simple
// Presenter's editor, since a ChordPro file cannot say "a slide with nothing on it".
// The file has a line of one chord alone where each is to be, and here:
//   - the first two (the intro, and the slide in the middle of the verse) have that
//     chord taken off, which leaves the slide its text box with no text in it;
//   - the last (the ending) has its text box taken off as well, which leaves a slide
//     with nothing on it at all.
// A blank text box made this way was written by Simple Presenter, and its RTF still
// names its font. One left by ProPresenter is an RTF with no font in it at all
// (spec/01-file-format.md). The words and the chords read the same from both.
QtObject {
    id: maker

    property var blanks: []
    property Timer clock: Timer {
        interval: 900
        onTriggered: maker.next()
    }

    function next() {
        if (!editing) {
            testInput.say("NOT IN THE EDITOR")
            testInput.quit()
            return
        }
        const editor = editScreen.editor
        const alone = editor.song().filter(block => block.chords.length === 1 && block.text.length === 1)
        const said = alone.map((block, index) => {
            let error = editor.setChordsAlone(block.row, block.element, 0, [])
            if (error === "" && index === alone.length - 1)
                error = editor.remove(block.row, block.element)
            return error
        })
        testInput.say("BLANKED " + alone.length + " " + JSON.stringify(said) + " " + JSON.stringify(editor.song().filter(block => block.blank).map(block => block.row + ":" + block.element)))
        stopEditing()
        testInput.quit()
    }

    function run() {
        const library = catalog.libraries[0].path
        const songs = [["amazing-grace.cho", 2], ["plain-song.cho", 2], ["it-is-well.chordpro", 4], ["minor-key.cho", 2], ["holy-holy-holy.txt", 2],
                       ["blank-slides.cho", 1]]
        for (const [file, lines] of songs) {
            const made = catalog.importSong("file://@SONGS@/" + file, library, lines)
            testInput.say("IMPORTED " + file + " " + JSON.stringify(made))
        }
        openLibrary(library)
        startEditing(documents.find(d => d.name === "Blank Slides"))
        clock.start()
    }
}
