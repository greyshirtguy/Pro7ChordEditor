// ProFormat: the reader and writer of ProPresenter's .pro files.
//
// This file is the only one in the module written by hand, and it has no code in it.
// Everything else is in Generated/, made by ../../generate-proto.sh from the
// descriptions of the format in the proto/ submodule (proto/autogen-proto, the set
// both apps agree to use: spec/01-file-format.md). Generated/MadeFrom.swift says which
// version of ProPresenter those were taken from. Do not edit anything in Generated/;
// run the script again. Do not write a reader by hand (spec/10-platforms.md).
//
// A presentation is `RVData_Presentation`. To read one and write it back:
//
//     let presentation = try RVData_Presentation(serializedBytes: data)
//     let data: Data = try presentation.serializedData()
//
// What makes writing a file back safe is that swift-protobuf keeps what it does not
// understand: a field that is not in the descriptions is held in the message's
// `unknownFields` and written out again. That is on unless it is asked for otherwise
// (`BinaryDecodingOptions.discardUnknownFields`), and it must never be asked for
// otherwise: it is how a file from a ProPresenter newer than these descriptions comes
// through a change of chords with everything else as it came (AGENTS.md, the first
// thing that must stay true).
//
// "As it came" is said of the message, not of the bytes. A file read and written
// back with nothing changed says exactly what it said, to ProPresenter or to anything
// else that reads protocol buffers, but its bytes may be in another order, and a
// number that was written out as nought may be left out, which reads as nought. The
// maintainer's word on 2026-10-10 was that this is what the rule means: ProPresenter's
// reading of the file is what is not to be disturbed. SongTests proves it of every
// test file.
//
// The rules of chords do not belong here, nor does anything about what this app does
// with a file: that is Song, which is built on this.
