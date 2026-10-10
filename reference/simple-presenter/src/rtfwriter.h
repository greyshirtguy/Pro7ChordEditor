#pragma once

#include "richtext.h"

#include <QByteArray>

// Writes text as RTF in the dialect Cocoa produces, which is what ProPresenter stores in
// slide text elements and what parseRtf reads: font and colour tables (with the expanded
// colour table, the only place a colour's alpha fits), then the paragraphs and runs.
// Everything RichText models is written; anything else an original document had (lists,
// tab stops, indents) is not known here and so cannot be carried over.
QByteArray writeRtf(const RichText &text);
