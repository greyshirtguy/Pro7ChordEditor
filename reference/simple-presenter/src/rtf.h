#pragma once

#include "richtext.h"

#include <QByteArray>
#include <QColor>
#include <QHash>
#include <QString>

struct RtfDefaults
{
    // Used for text with no \cf, or \cf0.
    QColor textColor = Qt::black;
    // RTF font tables name fonts by PostScript name; the document often knows the family.
    QHash<QString, QString> familyForPostScriptName;
};

// Parses the RTF that ProPresenter stores in slide text elements.
//
// ProPresenter is a Mac program at heart, and keeps each element's text as the RTF that
// Apple's text system writes. That is a dialect: fonts are named by PostScript name
// ("HelveticaNeue-Bold", not a family and a weight), colours live in an extra table
// that can also carry transparency, and an outline is a stroke width given as a
// percentage of the font size. A general RTF library would read little of that and be
// far larger than this needs, so the parser is written for this dialect alone: font
// and colour tables (including the expanded colour table), runs with font, size, bold,
// italic, underline, strikethrough, spacing, fill colour and stroke, and paragraphs
// with alignment and line height. Whatever else is in the RTF is passed over.
//
// A few things about the text are not in the RTF at all, because RTF cannot say them
// (capitals, for one): ProPresenter keeps those beside it, and proconvert.h puts the
// two together.
RichText parseRtf(const QByteArray &rtf, const RtfDefaults &defaults = {});
