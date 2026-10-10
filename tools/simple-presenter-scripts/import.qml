import QtQuick

// Makes presentations of the ChordPro files in testdata/songs, with Simple Presenter's
// own import (Catalog::importSong), and stops.
QtObject {
    function run() {
        const library = catalog.libraries[0].path
        const songs = [["amazing-grace.cho", 2], ["plain-song.cho", 2], ["it-is-well.chordpro", 4], ["minor-key.cho", 2], ["holy-holy-holy.txt", 2]]
        for (const [file, lines] of songs) {
            const made = catalog.importSong("file://@SONGS@/" + file, library, lines)
            testInput.say("IMPORTED " + file + " " + JSON.stringify(made))
        }
        testInput.quit()
    }
}
