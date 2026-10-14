#include "sheethints.h"

#include <QtMath>

#include <algorithm>

namespace sheethints {

namespace {

// What is said of dragging a chord, while the spot is on one: the maintainer's words
// (2026-10-14), with Windows' key that copies, which is this app's too.
const QString dragging = QStringLiteral("You can drag this chord to move it anywhere. (Hold Ctrl to drag a copy)");

// The copying of a line's chords onto another, which is done with keys alone and so is
// seen nowhere on the sheet. The words are the maintainer's (2026-10-13): "Press
// Command-C to copy all chords on the highlighted line. Press Command-V to paste
// previously copied chords to the highlighted line", and after it, where a line it would
// go onto has chords, " (Replaces existing chords)". One thing is added to them by the
// Mac's session: a whole verse or chorus that was copied (Ctrl+Shift+C) goes onto as
// many lines as it has, from the highlighted one on, so for that, and only then, it says
// "and the lines after it".
QList<Hint> copying(const Paste &paste)
{
    const QString onto = paste.lines > 1 ? QStringLiteral("to the highlighted line and the lines after it")
                                         : QStringLiteral("to the highlighted line");
    return {{QStringLiteral("Press"), {Ctrl + "+C"}, QStringLiteral("to copy all chords on the highlighted line."), true},
            {QStringLiteral("Press"), {Ctrl + "+V"},
             "to paste previously copied chords " + onto + (paste.replaces ? QStringLiteral(" (Replaces existing chords)") : QString()), true}};
}

Hint words(const QString &says)
{
    return {{}, {}, says, false};
}

Hint keyed(const QStringList &keys, const QString &says)
{
    return {{}, keys, says, false};
}

}

QString Hint::read() const
{
    QStringList parts;
    if (!lead.isEmpty())
        parts << lead;
    parts << keys;
    if (!says.isEmpty())
        parts << says;
    return parts.join(u' ');
}

QList<Hint> hints(const Situation &situation, bool ctrl, const Paste &paste, bool overStrip)
{
    switch (situation.doing) {
    case Doing::Typing:
        if (situation.onChord && situation.empty)
            return {keyed({Enter}, QStringLiteral("Takes this chord off")), keyed({Escape}, QStringLiteral("Leaves it as it was")),
                    words(QStringLiteral("Or type the chord that is to be here"))};
        return {keyed({Enter}, QStringLiteral("Keeps it")), keyed({Tab}, QStringLiteral("Keeps it and goes on to the next word")),
                keyed({Up, Down}, QStringLiteral("Choose from what is offered")), keyed({Escape}, QStringLiteral("Throws it away")),
                words(QStringLiteral("A click on another word keeps it too"))};
    case Doing::Carrying:
        if (situation.fromStrip)
            return {words(QStringLiteral("Let go on a word to put this chord there"))};
        if (ctrl)
            return {words(QStringLiteral("Let go on a word to put a copy there"))};
        return {words(QStringLiteral("Let go on a word to move it there")), keyed({Ctrl}, QStringLiteral("Held as you let go, leaves a copy behind"))};
    default:
        break;
    }
    // (The pointer over the strip is not over the sheet: what is said of the spot,
    // which is only where it was left, gives way.)
    if (overStrip)
        return {words(QStringLiteral("You can drag these down to the text on any slide"))};
    // (Whatever is held: with nobody at the sheet there is no highlighted line to
    // speak of.)
    if (situation.doing == Doing::Nothing)
        return {words(QStringLiteral("Point at a word or a chord, and this line says what the keys do there."))};
    if (ctrl)
        return copying(paste);
    if (situation.doing == Doing::OnChord)
        return {keyed({Delete}, QStringLiteral("Takes this chord off")), keyed({Numbers}, QStringLiteral("Change it")), words(dragging),
                words(QStringLiteral("Click to type over it"))};
    return {keyed({Numbers}, QStringLiteral("Put one of the key’s chords here")), keyed({Letters}, QStringLiteral("Or a click: type one"))};
}

// ---- Fitting them in

int pixels(double usual, double size)
{
    return qRound(usual * size);
}

double widthOfKey(const QString &key, double size, const Measure &measure)
{
    return std::max(KeyLeast * size, std::ceil(measure(key, pixels(KeySize, size), true)) + 2 * KeyPad * size);
}

double widthOf(const Hint &hint, double size, const Measure &measure)
{
    int parts = 0;
    double width = 0;
    if (!hint.lead.isEmpty()) {
        width += std::ceil(measure(hint.lead, pixels(WordSize, size), false));
        ++parts;
    }
    for (const QString &key : hint.keys) {
        width += widthOfKey(key, size, measure);
        ++parts;
    }
    if (!hint.says.isEmpty()) {
        width += std::ceil(measure(hint.says, pixels(WordSize, size), false));
        ++parts;
    }
    return width + Within * size * std::max(0, parts - 1);
}

double widthOf(const QList<Hint> &hints, int count, double size, const Measure &measure)
{
    double width = 0;
    for (int n = 0; n < count; ++n)
        width += widthOf(hints.at(n), size, measure);
    return width + Between * size * std::max(0, count - 1);
}

Fit fit(const QList<Hint> &hints, double room, const Measure &measure)
{
    if (hints.isEmpty())
        return {0, false, 1};
    // How many of them, from the first, may not be left off
    int needed = 0;
    while (needed < hints.size() && hints.at(needed).needed)
        ++needed;
    for (int count = int(hints.size()); count >= std::max(1, needed); --count) {
        if (widthOf(hints, count, 1, measure) <= room)
            return {count, false, 1};
    }
    // (One hint, and no room for it: it is cut off at the right.)
    if (needed <= 1)
        return {1, false, 1};
    // The needed ones one under the other, at the first size at which each fits
    for (double size : Sizes) {
        if (std::all_of(hints.cbegin(), hints.cbegin() + needed, [&](const Hint &hint) { return widthOf(hint, size, measure) <= room; }))
            return {needed, true, size};
    }
    return {needed, true, Sizes.last()};
}

double heightOf(const Fit &fit)
{
    if (!fit.stacked)
        return Height;
    const double line = KeyHeight * fit.size;
    return std::max(Height, std::ceil(fit.count * line + (fit.count - 1) * Lines * fit.size + 2 * OverAndUnder));
}

}
