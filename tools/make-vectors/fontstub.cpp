// Stands in for Simple Presenter's font look-up (src/fontresolver.cpp), which asks the
// system which fonts are installed. Nothing here depends on the answer: the vectors are
// about the characters of a text, not what they are drawn in. So a font's name is taken
// as its family, and that is all.
#include "fontresolver.h"

ResolvedFont resolvePostScriptName(const QString &postScriptName, const QString &familyHint)
{
    return {familyHint.isEmpty() ? postScriptName : familyHint, false, false};
}

QString postScriptNameFor(const QString &family, bool, bool)
{
    return family;
}

QString restyledPostScriptName(const QString &postScriptName, const QString &, bool, bool)
{
    return postScriptName;
}
