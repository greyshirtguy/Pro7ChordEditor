#pragma once

#include "chords.h"

#include "presentation.pb.h"

#include <QString>

// A new presentation made from a ChordPro file.
//
// ChordPro is the plain-text way of writing a song with its chords: the words, with
// each chord in square brackets where it is played, and lines in curly brackets that
// say what the song is and where its verses and choruses start. chords::parseSong
// reads one; this makes a presentation of what was read, as ProPresenter would have
// written it had it imported the song itself, as far as that can be told:
//   - a group for each part of the song (Verse 1, Chorus, ...), by name, as the
//     Multitracks imports to hand have them: a name and an id and no colour, the
//     colour being the workspace's for a group of that name;
//   - a slide for every so many lines of a part (two unless asked otherwise), each
//     with one text box named "Lyrics" over the whole slide, its words centred in
//     white, and its chords over them (see proconvert::readChords);
//   - the song's key, as the key its chords are written in and are shown in;
//   - its title and artist where ProPresenter keeps a song's (the CCLI block).
// What is NOT set, on purpose: the block that says a song came from Multitracks and is
// licensed by them. A song typed or imported by hand is not theirs.
//
// A GUESS, and untested: that ProPresenter opens a presentation made here. Every field
// it writes for an empty slide is written (proconvert::addBlankCue and makeTextElement
// are what the editor has always added slides and text boxes with), and the file is
// stamped as ProPresenter 7.16's, but none has been opened in ProPresenter yet.
namespace songimport {

// Reads a ChordPro file from disk. An empty song, with *error set, if it cannot be read
// or has no words in it.
chords::Song readFile(const QString &path, QString *error);

// The presentation for a song. `name` is what it is called; `linesPerSlide` how many
// lines of words go on a slide.
rv::data::Presentation build(const chords::Song &song, const QString &name, int linesPerSlide);

// Reads `file` and writes the presentation into the library folder `library`, under
// the song's title (or the file's name, for a song that has none), with a number after
// it if there is one of that name already. Gives the new file's path. Returns an error
// message, empty on success.
QString importFile(const QString &file, const QString &library, int linesPerSlide, QString *made);

}
