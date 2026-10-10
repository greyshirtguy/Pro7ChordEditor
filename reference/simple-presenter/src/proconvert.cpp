#include "proconvert.h"

#include "fontresolver.h"
#include "rtf.h"
#include "rtfwriter.h"
#include "sessionlog.h"
#include "timers.h"
#include "workspacefiles.h"

#include <QFile>
#include <QFileInfo>
#include <QFontDatabase>
#include <QImageReader>
#include <QUrl>
#include <cmath>
#include <QPointF>
#include <QRegularExpression>
#include <QSaveFile>
#include <QStringList>
#include <QtMath>

#include <algorithm>
#include <climits>

namespace proconvert {

namespace {

using Text = rv::data::Graphics::Text;
using Attributes = rv::data::Graphics::Text::Attributes;
using DataLink = rv::data::Slide::Element::DataLink;

int toQtVerticalAlignment(Text::VerticalAlignment alignment)
{
    switch (alignment) {
    case Text::VERTICAL_ALIGNMENT_TOP:
        return Qt::AlignTop;
    case Text::VERTICAL_ALIGNMENT_BOTTOM:
        return Qt::AlignBottom;
    default:
        return Qt::AlignVCenter;
    }
}

void addFontHint(RtfDefaults *defaults, const rv::data::Font &font)
{
    if (!font.name().empty() && !font.family().empty())
        defaults->familyForPostScriptName.insert(QString::fromStdString(font.name()), QString::fromStdString(font.family()));
}

Qt::Alignment toQtAlignment(Attributes::Alignment alignment)
{
    switch (alignment) {
    case Attributes::ALIGNMENT_CENTER:
        return Qt::AlignHCenter;
    case Attributes::ALIGNMENT_RIGHT:
        return Qt::AlignRight;
    case Attributes::ALIGNMENT_JUSTIFIED:
        return Qt::AlignJustify;
    default:
        return Qt::AlignLeft;
    }
}

// The format the file keeps beside the text, which is that of the start of the text.
// It is all there is to go by for a text box with nothing in it yet.
TextRun runFromAttributes(const Attributes &attributes)
{
    TextRun run;
    const rv::data::Font &font = attributes.font();
    const ResolvedFont resolved = resolvePostScriptName(QString::fromStdString(font.name()),
                                                        QString::fromStdString(font.family()));
    run.fontName = QString::fromStdString(font.name());
    run.family = resolved.family;
    run.bold = resolved.bold || font.bold();
    run.italic = resolved.italic || font.italic();
    run.size = font.size() > 0 ? font.size() : 60;
    run.fill = attributes.has_text_solid_fill() ? toColor(attributes.text_solid_fill()) : QColor(Qt::white);
    run.capitalization = int(attributes.capitalization());
    run.underline = attributes.underline_style().style() != Attributes::Underline::STYLE_NONE;
    run.strikethrough = attributes.strikethrough_style().style() != Attributes::Underline::STYLE_NONE;
    run.kerning = attributes.kerning();
    run.superscript = attributes.superscript();
    // A percentage of the font size, negative when the glyphs are filled as well.
    if (attributes.stroke_width() != 0) {
        run.strokeWidth = qAbs(attributes.stroke_width()) / 100.0 * run.size;
        run.fillVisible = attributes.stroke_width() < 0;
        run.stroke = toColor(attributes.stroke_color());
    }
    return run;
}

struct CapsRange
{
    int start;
    int end; // exclusive
    int capitalization;
};

// Gives each stretch of text the capitalisation of the range covering it. Ranges count
// characters through the whole text, a paragraph break counting as one.
void applyCapitalization(RichText *text, const QList<CapsRange> &ranges)
{
    const auto capsAt = [&ranges](int position) {
        for (const CapsRange &range : ranges) {
            if (position >= range.start && position < range.end)
                return range.capitalization;
        }
        return int(TextRun::NoCapitalization);
    };

    int offset = 0;
    for (TextParagraph &paragraph : text->paragraphs) {
        QList<TextRun> pieces;
        for (const TextRun &run : std::as_const(paragraph.runs)) {
            const int runStart = offset;
            const int runEnd = offset + int(run.text.size());
            // Cut the run wherever a range begins or ends inside it.
            QList<int> cuts {runStart, runEnd};
            for (const CapsRange &range : ranges) {
                for (const int edge : {range.start, range.end}) {
                    if (edge > runStart && edge < runEnd && !cuts.contains(edge))
                        cuts.append(edge);
                }
            }
            std::sort(cuts.begin(), cuts.end());
            for (qsizetype i = 0; i + 1 < cuts.size() || (i == 0 && runStart == runEnd); ++i) {
                TextRun piece = run;
                const int from = cuts.at(i);
                const int to = runStart == runEnd ? from : cuts.at(i + 1);
                piece.text = run.text.mid(from - runStart, to - from);
                piece.capitalization = capsAt(from);
                if (!pieces.isEmpty() && pieces.last().sameFormat(piece))
                    pieces.last().text += piece.text;
                else
                    pieces.append(piece);
                if (runStart == runEnd)
                    break;
            }
            offset = runEnd;
        }
        paragraph.runs = pieces;
        ++offset; // the paragraph break
    }
}

QString joined(const QStringList &lines, const QString &separator)
{
    QStringList kept;
    for (const QString &line : lines) {
        if (!line.trimmed().isEmpty())
            kept << line.trimmed();
    }
    return kept.join(separator);
}

QString onePerLine(const QString &text, bool words)
{
    QStringList parts;
    if (words) {
        parts = text.split(QRegularExpression(QStringLiteral("\\s+")), Qt::SkipEmptyParts);
    } else {
        for (const QChar c : text) {
            if (!c.isSpace())
                parts << QString(c);
        }
    }
    return parts.join(u'\n');
}

} // namespace

QString linkTransformed(const QString &text, int transform)
{
    using Link = DataLink::AlternateElementText;
    switch (transform) {
    case Link::TEXT_TRANSFORM_OPTION_REMOVE_LINE_RETURNS:
        return joined(text.split(u'\n'), QStringLiteral(" "));
    case Link::TEXT_TRANSFORM_OPTION_ONE_WORD_PER_LINE:
        return onePerLine(text, true);
    case Link::TEXT_TRANSFORM_OPTION_ONE_CHARACTER_PER_LINE:
        return onePerLine(text, false);
    default:
        return text;
    }
}

namespace {

// What a text element can do to its own text, however it came by it.
QString elementTransformed(const QString &text, int transform, const QString &delimiter)
{
    switch (transform) {
    case Text::TRANSFORM_SINGLE_LINE:
        return joined(text.split(u'\n'), QStringLiteral(" "));
    case Text::TRANSFORM_ONE_WORD_PER_LINE:
        return onePerLine(text, true);
    case Text::TRANSFORM_ONE_CHARACTER_PER_LINE:
        return onePerLine(text, false);
    case Text::TRANSFORM_REPLACE_LINE_RETURNS:
        return joined(text.split(u'\n'), delimiter);
    default:
        return text;
    }
}

void insertShadow(QVariantMap *map, const QString &prefix, const rv::data::Graphics::Shadow &shadow)
{
    const QColor base = toColor(shadow.color());
    QColor drawn = base;
    drawn.setAlphaF(base.alphaF() * qBound(0.0, shadow.opacity(), 1.0));
    // The angle is measured anticlockwise with y pointing up; slides have y pointing down.
    const double angle = qDegreesToRadians(shadow.angle());
    map->insert(prefix + "Enabled", shadow.enable());
    map->insert(prefix + "Color", drawn);
    map->insert(prefix + "OffsetX", shadow.offset() * qCos(angle));
    map->insert(prefix + "OffsetY", -shadow.offset() * qSin(angle));
    map->insert(prefix + "Radius", shadow.radius());
    map->insert(prefix + "Angle", shadow.angle());
    map->insert(prefix + "Offset", shadow.offset());
}

// A name for a kind of link this app does not act on, to show that it is there.
QString otherLinkLabel(const DataLink &link)
{
    switch (link.PropertyType_case()) {
    case DataLink::kTicker: return QStringLiteral("Ticker");
    case DataLink::kClockText: return QStringLiteral("Clock");
    case DataLink::kChordChart: return QStringLiteral("Chord chart");
    case DataLink::kOutputScreen: return QStringLiteral("Output screen");
    case DataLink::kPcoLive: return QStringLiteral("Planning Center Live");
    case DataLink::kAlternateFill: return QStringLiteral("Fill of another element");
    case DataLink::kStageMessage: return QStringLiteral("Stage message");
    case DataLink::kVideoCountdown: return QStringLiteral("Video countdown");
    case DataLink::kSlideImage: return QStringLiteral("Slide image");
    case DataLink::kCcliText: return QStringLiteral("CCLI");
    case DataLink::kGroupName: return QStringLiteral("Group name");
    case DataLink::kGroupColor: return QStringLiteral("Group colour");
    case DataLink::kPresentationNotes: return QStringLiteral("Presentation notes");
    case DataLink::kPlaylistItem: return QStringLiteral("Playlist item");
    case DataLink::kAutoAdvanceTimeRemaining: return QStringLiteral("Auto advance time");
    case DataLink::kCaptureStatusText: return QStringLiteral("Capture status");
    case DataLink::kCaptureStatusColor: return QStringLiteral("Capture status colour");
    case DataLink::kSlideCount: return QStringLiteral("Slide count");
    case DataLink::kAudioCountdown: return QStringLiteral("Audio countdown");
    case DataLink::kPresentation: return QStringLiteral("Presentation");
    case DataLink::kSlideLabelText: return QStringLiteral("Slide label");
    case DataLink::kSlideLabelColor: return QStringLiteral("Slide label colour");
    case DataLink::kRssFeed: return QStringLiteral("RSS feed");
    case DataLink::kFileFeed: return QStringLiteral("File feed");
    case DataLink::kChordProChart: return QStringLiteral("Chord chart");
    case DataLink::kPlaybackMarkerText: return QStringLiteral("Playback marker");
    case DataLink::kPlaybackMarkerColor: return QStringLiteral("Playback marker colour");
    case DataLink::kTimecodeText: return QStringLiteral("Timecode");
    case DataLink::kTimecodeStatus: return QStringLiteral("Timecode status");
    case DataLink::kMessageText: return QStringLiteral("Message");
    case DataLink::kKeyValueText: return QStringLiteral("Key value text");
    case DataLink::kKeyValueFill: return QStringLiteral("Key value fill");
    case DataLink::kZone: return QStringLiteral("Zone");
    default: return QString();
    }
}

// Whether what a link puts in an element is not words but a picture or a colour, which
// takes the place of the element's fill: a picture of a slide or of an output, say.
// The fill such an element has in the file is then only a stand-in for it.
bool linkIsPicture(const DataLink &link)
{
    switch (link.PropertyType_case()) {
    case DataLink::kChordChart:
    case DataLink::kOutputScreen:
    case DataLink::kAlternateFill:
    case DataLink::kSlideImage:
    case DataLink::kGroupColor:
    case DataLink::kCaptureStatusColor:
    case DataLink::kPresentation:
    case DataLink::kSlideLabelColor:
    case DataLink::kChordProChart:
    case DataLink::kPlaybackMarkerColor:
    case DataLink::kKeyValueFill:
        return true;
    default:
        return false;
    }
}

// The same for a visibility condition.
QString otherConditionLabel(const DataLink::VisibilityLink::Condition &condition)
{
    using Condition = DataLink::VisibilityLink::Condition;
    switch (condition.ConditionType_case()) {
    case Condition::kTimerVisibility: return QStringLiteral("Timer “%1”").arg(QString::fromStdString(condition.timer_visibility().timer_name()));
    case Condition::kVideoCountdownVisibility: return QStringLiteral("Video countdown");
    case Condition::kAudioCountdownVisibility: return QStringLiteral("Audio countdown");
    case Condition::kCaptureSessionVisibility: return QStringLiteral("Capture");
    case Condition::kVideoInputVisibility: return QStringLiteral("Video input");
    default: return QStringLiteral("Something else");
    }
}

using ShapeType = rv::data::Graphics::Path::Shape::Type;

// The shapes this app can make, by the names the element map knows them under. Any
// other shape of ProPresenter's is "other": it is drawn, from its outline, but not made.
QString shapeName(ShapeType type)
{
    switch (type) {
    case rv::data::Graphics::Path::Shape::TYPE_UNKNOWN:
    case rv::data::Graphics::Path::Shape::TYPE_RECTANGLE:
        return QStringLiteral("rectangle");
    case rv::data::Graphics::Path::Shape::TYPE_ROUNDED_RECTANGLE:
        return QStringLiteral("roundedRectangle");
    case rv::data::Graphics::Path::Shape::TYPE_ELLIPSE:
        return QStringLiteral("ellipse");
    case rv::data::Graphics::Path::Shape::TYPE_RIGHT_ARROW:
        return QStringLiteral("arrow");
    default:
        return QStringLiteral("other");
    }
}

// Writes the outline of one of those shapes as ProPresenter writes it: points on the
// unit square, each with the two points that bend the outline on its way in and on its
// way out (the same as the point itself where the outline is straight). The corners of
// a rounded rectangle are round in the element's own proportions, so its outline
// depends on the element's size, and is written again whenever that changes.
void setShapePath(rv::data::Graphics::Path *path, ShapeType type, double roundness, double width, double height)
{
    path->clear_points();
    path->set_closed(true);
    const auto add = [path](QPointF point, QPointF in, QPointF out, bool curved) {
        auto *made = path->add_points();
        made->mutable_point()->set_x(point.x());
        made->mutable_point()->set_y(point.y());
        made->mutable_q0()->set_x(in.x());
        made->mutable_q0()->set_y(in.y());
        made->mutable_q1()->set_x(out.x());
        made->mutable_q1()->set_y(out.y());
        if (curved)
            made->set_curved(true);
    };
    const auto corner = [&add](double x, double y) { add({x, y}, {x, y}, {x, y}, false); };
    // How far along a quarter of a circle's radius its bending points sit
    const double bend = 0.5522847498;
    path->mutable_shape()->set_type(type);
    switch (type) {
    case rv::data::Graphics::Path::Shape::TYPE_ELLIPSE: {
        const double near = 0.5 - bend / 2;
        const double far = 0.5 + bend / 2;
        add({0.5, 0}, {near, 0}, {far, 0}, true);
        add({1, 0.5}, {1, near}, {1, far}, true);
        add({0.5, 1}, {far, 1}, {near, 1}, true);
        add({0, 0.5}, {0, far}, {0, near}, true);
        break;
    }
    case rv::data::Graphics::Path::Shape::TYPE_ROUNDED_RECTANGLE: {
        roundness = qBound(0.0, roundness, 0.5);
        const double radius = roundness * qMin(width, height);
        const double rx = width > 0 ? radius / width : 0;
        const double ry = height > 0 ? radius / height : 0;
        const double cx = rx * (1 - bend);
        const double cy = ry * (1 - bend);
        add({rx, 0}, {cx, 0}, {rx, 0}, true);
        add({1 - rx, 0}, {1 - rx, 0}, {1 - cx, 0}, true);
        add({1, ry}, {1, cy}, {1, ry}, true);
        add({1, 1 - ry}, {1, 1 - ry}, {1, 1 - cy}, true);
        add({1 - rx, 1}, {1 - cx, 1}, {1 - rx, 1}, true);
        add({rx, 1}, {rx, 1}, {cx, 1}, true);
        add({0, 1 - ry}, {0, 1 - cy}, {0, 1 - ry}, true);
        add({0, ry}, {0, ry}, {0, cy}, true);
        path->mutable_shape()->mutable_rounded_rectangle()->set_roundness(roundness);
        break;
    }
    case rv::data::Graphics::Path::Shape::TYPE_RIGHT_ARROW:
        // A shaft half the height, and a head over the last two fifths of the width
        corner(0, 0.25);
        corner(0.6, 0.25);
        corner(0.6, 0);
        corner(1, 0.5);
        corner(0.6, 1);
        corner(0.6, 0.75);
        corner(0, 0.75);
        path->mutable_shape()->mutable_arrow()->mutable_corner()->set_x(0.6);
        path->mutable_shape()->mutable_arrow()->mutable_corner()->set_y(0.25);
        break;
    default:
        corner(0, 0);
        corner(1, 0);
        corner(1, 1);
        corner(0, 1);
        break;
    }
}

// The media a fill is made of, as its drawing settings: an image's or a video's.
const rv::data::Media::DrawingProperties &fillDrawing(const rv::data::Media &media)
{
    return media.has_video() ? media.video().drawing() : media.image().drawing();
}

QVariantMap toElementMap(const rv::data::Slide::Element &slideElement)
{
    const rv::data::Graphics::Element &element = slideElement.element();
    const rv::data::Graphics::Fill &fill = element.fill();
    QVariantMap map {
        {"id", QString::fromStdString(element.uuid().string())},
        {"name", QString::fromStdString(element.name())},
        {"x", element.bounds().origin().x()},
        {"y", element.bounds().origin().y()},
        {"width", element.bounds().size().width()},
        {"height", element.bounds().size().height()},
        {"rotation", element.rotation()},
        {"opacity", element.opacity()},
        {"locked", element.locked()},
        {"hidden", element.hidden()},
        {"fillOn", fill.enable()},
        {"fillKind", fill.has_color() ? QStringLiteral("color") : fill.has_gradient() ? QStringLiteral("gradient")
                     : fill.has_media() ? QStringLiteral("media")
                     : fill.FillType_case() == rv::data::Graphics::Fill::FILLTYPE_NOT_SET ? QStringLiteral("none")
                     : QStringLiteral("other")},
        {"fillEnabled", fill.enable() && fill.has_color()},
        {"fillColor", toColor(fill.color())},
        {"fillLinesOnly", element.text_line_mask().enabled()},
        {"lineMaskStyle", int(element.text_line_mask().mask_style())},
        {"lineMaskWidthOffset", element.text_line_mask().width_offset()},
        {"lineMaskHeightOffset", element.text_line_mask().height_offset()},
        {"lineMaskHorizontalOffset", element.text_line_mask().horizontal_offset()},
        {"lineMaskVerticalOffset", element.text_line_mask().vertical_offset()},
        {"strokeOn", element.stroke().enable()},
        {"strokeEnabled", element.stroke().enable() && element.stroke().width() > 0},
        {"strokeColor", toColor(element.stroke().color())},
        {"strokeWidth", element.stroke().width()},
    };
    insertShadow(&map, QStringLiteral("shadow"), element.shadow());
    insertShadow(&map, QStringLiteral("textShadow"), element.text().shadow());

    // The shape. A plain rectangle needs no outline to be drawn from; anything else
    // carries its own, as [x, y, in x, in y, out x, out y] for each point.
    const rv::data::Graphics::Path &path = element.path();
    const QString shape = path.points_size() < 2 ? QStringLiteral("rectangle") : shapeName(path.shape().type());
    map.insert("shape", shape);
    map.insert("roundness", path.shape().rounded_rectangle().roundness());
    QVariantList outline;
    if (shape != QLatin1String("rectangle")) {
        for (const auto &point : path.points()) {
            outline.append(QVariant(QVariantList {point.point().x(), point.point().y(), point.q0().x(), point.q0().y(),
                                                  point.q1().x(), point.q1().y()}));
        }
    }
    map.insert("outline", outline);

    // A gradient is drawn from its first colour to its last, along its angle.
    const rv::data::Graphics::Gradient &gradient = fill.gradient();
    const int stops = gradient.stops_size();
    map.insert("fillGradientFrom", stops > 0 ? toColor(gradient.stops(0).color()) : QColor(Qt::white));
    map.insert("fillGradientTo", stops > 0 ? toColor(gradient.stops(stops - 1).color()) : QColor(Qt::black));
    map.insert("fillGradientAngle", gradient.angle());
    // A media fill: the file's name, and the file if it is a picture that can be found.
    QString mediaName;
    QString mediaPath;
    if (fill.has_media()) {
        mediaName = workspace::fileNameOf(fill.media().url());
        mediaPath = workspace::findMediaFile(fill.media().url());
    }
    const bool mediaIsVideo = fill.media().has_video();
    map.insert("fillMediaName", mediaName);
    map.insert("fillMediaPath", mediaPath);
    map.insert("fillMediaSource", mediaPath.isEmpty() || mediaIsVideo ? QUrl() : QUrl::fromLocalFile(mediaPath));
    map.insert("fillMediaVideo", mediaIsVideo);
    map.insert("fillMediaScale", fill.has_media() ? qMin(2, int(fillDrawing(fill.media()).scale_behavior())) % 3 : 0);
    // Whether there is a fill that is drawn: on, and a colour, a gradient or a picture
    map.insert("fillShown", fill.enable() && (fill.has_color() || fill.has_gradient()
                                              || (fill.has_media() && !mediaIsVideo && !mediaPath.isEmpty())));
    map.insert("featherOn", element.feather().enable() && element.feather().radius() > 0);
    map.insert("featherRadius", element.feather().radius());

    const Text &text = element.text();
    const RichText words = readText(text);
    map.insert("text", QVariant::fromValue(words));
    // Whether the file gives the element a text at all, with words in it or none. (The
    // map has "text" either way: an element with none gets an empty one, as if it had.)
    map.insert("textBox", element.has_text());
    // The start of its own words on one line, for naming an element that has no name
    map.insert("words", words.plainText().simplified().left(60));
    map.insert("verticalAlignment", toQtVerticalAlignment(text.vertical_alignment()));
    map.insert("textScale", int(text.scale_behavior()));
    map.insert("marginLeft", text.margins().left());
    map.insert("marginTop", text.margins().top());
    map.insert("marginRight", text.margins().right());
    map.insert("marginBottom", text.margins().bottom());
    map.insert("textTransform", int(text.transform()));
    map.insert("textTransformDelimiter", QString::fromStdString(text.transformdelimiter()));
    // Its chords, if it has any. Most elements have none, and are spared the key.
    QVariantList chordList;
    for (const auto &custom : text.attributes().custom_attributes()) {
        if (custom.has_chord())
            chordList.append(QVariantMap {{"at", int(custom.range().start())}, {"name", QString::fromStdString(custom.chord())}});
    }
    if (!chordList.isEmpty())
        map.insert("chords", chordList);
    // How it draws the chords of the slide whose words it shows (a stage layout's
    // text box). A colour that was never set is white, which is what ProPresenter
    // starts one at.
    map.insert("chordsOn", text.chord_pro().enabled());
    map.insert("chordNotation", int(text.chord_pro().notation()));
    map.insert("chordColor", text.chord_pro().has_color() ? toColor(text.chord_pro().color()) : QColor(Qt::white));
    if (text.chord_pro().enabled()) {
        // Words with chords over them are drawn line by line by ChordedText.qml, which
        // is plain QML text and wants the element's style as plain values. Only an
        // element that draws chords is given them.
        const TextRun first = words.firstRun();
        const Qt::Alignment alignment = words.paragraphs.isEmpty() ? Qt::AlignLeft : words.paragraphs.first().alignment;
        map.insert("chordStyle", QVariantMap {
            {"family", first.family},
            {"size", first.size},
            {"bold", first.bold},
            {"italic", first.italic},
            {"color", first.fill},
            {"capitals", first.capitalization == TextRun::AllCaps},
            {"alignment", alignment & Qt::AlignHCenter ? 1 : alignment & Qt::AlignRight ? 2 : 0},
        });
    }

    map.insert("linkKind", QStringLiteral("none"));
    map.insert("linkElementId", QString());
    map.insert("linkElementName", QString());
    map.insert("linkTransform", 0);
    map.insert("linkTimerId", QString());
    map.insert("linkTimerName", QString());
    map.insert("linkTimerHours", 0);
    map.insert("linkTimerMinutes", 0);
    map.insert("linkTimerSeconds", 0);
    map.insert("linkTimerHundredths", 0);
    map.insert("linkTimerHundredthsUnderMinute", false);
    map.insert("linkTimerPattern", QString());
    map.insert("linkSlideNext", false);
    map.insert("linkSlideSource", 0);
    map.insert("linkSlideName", QString());
    map.insert("linkLabel", QString());
    map.insert("linkPicture", false);
    map.insert("visibilityRules", false);
    map.insert("visibilityCriterion", 0);
    map.insert("visibilityConditions", QVariantList());
    map.insert("visibilityTimed", false);
    for (const DataLink &link : slideElement.data_links()) {
        if (link.has_alternate_text()) {
            map.insert("linkKind", QStringLiteral("element"));
            map.insert("linkElementId", QString::fromStdString(link.alternate_text().other_element_uuid().string()));
            map.insert("linkElementName", QString::fromStdString(link.alternate_text().other_element_name()));
            map.insert("linkTransform", int(link.alternate_text().text_transform()));
        } else if (link.has_timer_text()) {
            const DataLink::TimerText &timer = link.timer_text();
            map.insert("linkKind", QStringLiteral("timer"));
            map.insert("linkTimerId", QString::fromStdString(timer.timer_uuid().string()));
            map.insert("linkTimerName", QString::fromStdString(timer.timer_name()));
            map.insert("linkTimerHours", int(timer.timer_format().hour()));
            map.insert("linkTimerMinutes", int(timer.timer_format().minute()));
            map.insert("linkTimerSeconds", int(timer.timer_format().second()));
            // The file calls them milliseconds; what ProPresenter shows is hundredths.
            map.insert("linkTimerHundredths", int(timer.timer_format().millisecond()));
            map.insert("linkTimerHundredthsUnderMinute", timer.timer_format().show_milliseconds_under_minute_only());
            map.insert("linkTimerPattern", QString::fromStdString(timer.timer_format_string()));
        } else if (link.has_slide_text()) {
            const DataLink::SlideText &words = link.slide_text();
            map.insert("linkKind", QStringLiteral("slideText"));
            map.insert("linkSlideNext", words.source_slide() == DataLink::SLIDE_SOURCE_TYPE_NEXT_SLIDE);
            map.insert("linkSlideSource", int(words.source_option()));
            map.insert("linkSlideName", QString::fromStdString(words.name_to_match()));
            map.insert("linkTransform", int(words.element_text_transform()));
        } else if (link.has_visibility_link()) {
            QVariantList conditions;
            for (int index = 0; index < link.visibility_link().conditions_size(); ++index) {
                const auto &condition = link.visibility_link().conditions(index);
                using Criterion = DataLink::VisibilityLink::Condition::ElementVisibility;
                if (condition.has_element_visibility()) {
                    const auto &other = condition.element_visibility();
                    conditions.append(QVariantMap {
                        {"kind", QStringLiteral("element")},
                        {"elementId", QString::fromStdString(other.other_element_uuid().string())},
                        {"elementName", QString::fromStdString(other.other_element_name())},
                        {"hasText", other.visibility_criterion() == Criterion::ELEMENT_VISIBILITY_CRITERION_HAS_TEXT},
                    });
                } else {
                    QVariantMap other {
                        {"kind", QStringLiteral("other")},
                        {"index", index},
                        {"label", otherConditionLabel(condition)},
                    };
                    // Of the others, one about a timer can be told: see toSlideMap.
                    if (condition.has_timer_visibility()) {
                        const auto &timer = condition.timer_visibility();
                        other.insert("timed", true);
                        other.insert("timerId", QString::fromStdString(timer.timer_uuid().string()));
                        other.insert("timerName", QString::fromStdString(timer.timer_name()));
                        other.insert("timerCriterion", int(timer.visibility_criterion()));
                    }
                    conditions.append(other);
                }
            }
            map.insert("visibilityRules", true);
            map.insert("visibilityCriterion", int(link.visibility_link().visibility_criterion()));
            map.insert("visibilityConditions", conditions);
        } else if (!otherLinkLabel(link).isEmpty()) {
            map.insert("linkKind", QStringLiteral("other"));
            map.insert("linkLabel", otherLinkLabel(link));
            map.insert("linkPicture", linkIsPicture(link));
        }
    }
    return map;
}

// Whether what an element shows is not text of the slide's but something that changes
// while the slide is on show: a timer's time, or the words of the slide that is live or
// of the one after it, which is what a stage layout shows. (The clock is another the
// file format has.)
bool isLive(const QVariantMap &element)
{
    const QString kind = element.value("linkKind").toString();
    return kind == QLatin1String("timer") || kind == QLatin1String("slideText");
}

// The element a link or a visibility rule refers to: by id, or failing that by name,
// which is how a rule copied from another slide still finds its target.
qsizetype findElement(const QVariantList &elements, const QString &id, const QString &name)
{
    for (qsizetype i = 0; i < elements.size(); ++i) {
        if (!id.isEmpty() && elements.at(i).toMap().value("id").toString() == id)
            return i;
    }
    for (qsizetype i = 0; i < elements.size(); ++i) {
        if (!name.isEmpty() && elements.at(i).toMap().value("name").toString() == name)
            return i;
    }
    return -1;
}

rv::data::Slide::Element *findSlideElement(rv::data::Slide *slide, const QString &id)
{
    const std::string wanted = id.toStdString();
    for (rv::data::Slide::Element &element : *slide->mutable_elements()) {
        if (element.element().uuid().string() == wanted)
            return &element;
    }
    return nullptr;
}

void applyShadow(rv::data::Graphics::Shadow *shadow, const QString &prefix, const QVariantMap &changes)
{
    if (changes.contains(prefix + "Enabled"))
        shadow->set_enable(changes.value(prefix + "Enabled").toBool());
    if (changes.contains(prefix + "Color")) {
        // The colour as drawn: how see-through it is is the shadow's opacity.
        const QColor drawn = changes.value(prefix + "Color").value<QColor>();
        QColor solid = drawn;
        solid.setAlphaF(1);
        setColor(shadow->mutable_color(), solid);
        shadow->set_opacity(drawn.alphaF());
    }
    if (changes.contains(prefix + "Angle"))
        shadow->set_angle(changes.value(prefix + "Angle").toDouble());
    if (changes.contains(prefix + "Offset"))
        shadow->set_offset(qMax(0.0, changes.value(prefix + "Offset").toDouble()));
    if (changes.contains(prefix + "Radius"))
        shadow->set_radius(qMax(0.0, changes.value(prefix + "Radius").toDouble()));
}

bool anyKeyStartsWith(const QVariantMap &map, const QString &prefix)
{
    const auto from = map.lowerBound(prefix);
    return from != map.constEnd() && from.key().startsWith(prefix);
}

// Removes the links for which `matches` holds.
template <typename Matches>
void removeLinks(rv::data::Slide::Element *element, Matches matches)
{
    for (int i = element->data_links_size() - 1; i >= 0; --i) {
        if (matches(element->data_links(i)))
            element->mutable_data_links()->DeleteSubrange(i, 1);
    }
}

} // namespace

bool readPresentation(const QString &path, rv::data::Presentation *presentation, QString *error)
{
    QFile file(path);
    if (!file.open(QIODevice::ReadOnly)) {
        *error = QStringLiteral("Cannot open %1: %2").arg(QFileInfo(path).fileName(), file.errorString());
        return false;
    }
    const QByteArray data = file.readAll();
    if (!presentation->ParseFromArray(data.constData(), int(data.size()))) {
        *error = QStringLiteral("%1 is not a ProPresenter 7 presentation").arg(QFileInfo(path).fileName());
        return false;
    }
    return true;
}

QString writePresentation(const QString &path, const rv::data::Presentation &presentation)
{
    std::string bytes;
    if (!presentation.SerializeToString(&bytes))
        return QStringLiteral("Cannot encode %1").arg(QFileInfo(path).fileName());
    QSaveFile file(path);
    if (!file.open(QIODevice::WriteOnly) || file.write(bytes.data(), qint64(bytes.size())) != qint64(bytes.size())
        || !file.commit())
        return QStringLiteral("Cannot write %1: %2").arg(QFileInfo(path).fileName(), file.errorString());
    SessionLog::write("saved", path);
    return {};
}

QColor toColor(const rv::data::Color &color)
{
    const auto unit = [](float v) { return qBound(0.0f, v, 1.0f); };
    return QColor::fromRgbF(unit(color.red()), unit(color.green()), unit(color.blue()), unit(color.alpha()));
}

void setColor(rv::data::Color *target, const QColor &color)
{
    target->set_red(color.redF());
    target->set_green(color.greenF());
    target->set_blue(color.blueF());
    target->set_alpha(color.alphaF());
}

RichText readText(const Text &text)
{
    const Attributes &attributes = text.attributes();
    // A text box with nothing in it still has a format, for what is typed into it or
    // linked into it: one empty paragraph says so.
    const auto blank = [&attributes] {
        RichText rich;
        TextParagraph paragraph;
        paragraph.alignment = toQtAlignment(attributes.paragraph_style().alignment());
        paragraph.runs.append(runFromAttributes(attributes));
        rich.paragraphs.append(paragraph);
        return rich;
    };
    if (text.rtf_data().empty())
        return blank();
    RtfDefaults defaults;
    if (attributes.has_text_solid_fill())
        defaults.textColor = toColor(attributes.text_solid_fill());
    addFontHint(&defaults, attributes.font());
    QList<CapsRange> ranges;
    for (const auto &custom : attributes.custom_attributes()) {
        if (custom.has_original_font())
            addFontHint(&defaults, custom.original_font());
        if (custom.has_capitalization())
            ranges.append({custom.range().start(), custom.range().end(), int(custom.capitalization())});
    }
    RichText rich = parseRtf(QByteArray::fromStdString(text.rtf_data()), defaults);
    if (rich.paragraphs.isEmpty())
        return blank();

    // With no ranges, the capitalisation given for the text as a whole applies to all of it.
    if (ranges.isEmpty() && attributes.capitalization() != Attributes::CAPITALIZATION_NONE)
        ranges.append({0, INT_MAX, int(attributes.capitalization())});
    if (!ranges.isEmpty())
        applyCapitalization(&rich, ranges);
    return rich;
}

QList<chords::Chord> readChords(const Text &text)
{
    QList<chords::Chord> result;
    for (const auto &custom : text.attributes().custom_attributes()) {
        if (custom.has_chord())
            result.append({int(custom.range().start()), QString::fromStdString(custom.chord())});
    }
    std::stable_sort(result.begin(), result.end(), [](const chords::Chord &a, const chords::Chord &b) { return a.at < b.at; });
    return result;
}

namespace {

void addChordRanges(Attributes *attributes, const QString &plainText, const QList<chords::Chord> &chordList)
{
    const QList<chords::Range> written = chords::ranges(plainText, chordList);
    for (const chords::Range &range : written) {
        auto *custom = attributes->add_custom_attributes();
        custom->mutable_range()->set_start(range.start);
        custom->mutable_range()->set_end(range.end);
        custom->set_chord(range.name.toStdString());
    }
}

}

void writeChords(Text *text, const QList<chords::Chord> &chordList)
{
    // Everything but the chords stays, in its order; the chords go after, in theirs.
    Attributes *attributes = text->mutable_attributes();
    QList<Attributes::CustomAttribute> others;
    for (const auto &custom : attributes->custom_attributes()) {
        if (!custom.has_chord())
            others.append(custom);
    }
    attributes->clear_custom_attributes();
    for (const auto &custom : std::as_const(others))
        *attributes->add_custom_attributes() = custom;
    addChordRanges(attributes, readText(*text).plainText(), chordList);
}

void writeText(Text *text, const RichText &rich)
{
    // Ranges are tied to character positions. The ones this app does not understand
    // (a gradient over part of the text, say) can stay only while the characters do:
    // when the text is being given a new format but is otherwise the same. Chords it
    // does understand, and they go over to the new words (see chords::carried).
    const QString before = readText(*text).plainText();
    const QString after = rich.plainText();
    const QList<chords::Chord> chordList = chords::carried(before, after, readChords(*text));
    QList<Attributes::CustomAttribute> kept;
    if (before == after) {
        for (const auto &custom : text->attributes().custom_attributes()) {
            switch (custom.Attribute_case()) {
            // These are written afresh below, or describe a scaling of the old font.
            case Attributes::CustomAttribute::kCapitalization:
            case Attributes::CustomAttribute::kOriginalFont:
            case Attributes::CustomAttribute::kOriginalFontSize:
            case Attributes::CustomAttribute::kFontScaleFactor:
            case Attributes::CustomAttribute::kChord:
                break;
            default:
                kept.append(custom);
                break;
            }
        }
    }

    text->set_rtf_data(writeRtf(rich).toStdString());

    Attributes *attributes = text->mutable_attributes();
    const TextRun first = rich.firstRun();
    const QString fontName = first.fontName.isEmpty() ? postScriptNameFor(first.family, first.bold, first.italic)
                                                      : first.fontName;
    rv::data::Font *font = attributes->mutable_font();
    font->set_name(fontName.toStdString());
    font->set_family(first.family.toStdString());
    font->set_size(first.size);
    font->set_bold(first.bold);
    font->set_italic(first.italic);
    font->clear_face();

    attributes->set_capitalization(Attributes::Capitalization(first.capitalization));
    // A gradient or media fill for the text is not something the text itself can say;
    // it stays as it is, and only a plain colour is kept in step with the text.
    if (attributes->fill_case() == Attributes::kTextSolidFill || attributes->fill_case() == Attributes::FILL_NOT_SET)
        setColor(attributes->mutable_text_solid_fill(), first.fill);
    attributes->mutable_underline_style()->set_style(first.underline ? Attributes::Underline::STYLE_SINGLE
                                                                    : Attributes::Underline::STYLE_NONE);
    attributes->mutable_strikethrough_style()->set_style(first.strikethrough ? Attributes::Underline::STYLE_SINGLE
                                                                            : Attributes::Underline::STYLE_NONE);
    attributes->set_kerning(first.kerning);
    attributes->set_superscript(first.superscript);
    // The stroke is a percentage of the font size, negative when the glyphs are filled too.
    if (first.strokeWidth > 0 && first.size > 0) {
        const double percent = first.strokeWidth / first.size * 100;
        attributes->set_stroke_width(first.fillVisible ? -percent : percent);
        setColor(attributes->mutable_stroke_color(), first.stroke);
    } else {
        attributes->set_stroke_width(0);
        attributes->clear_stroke_color();
    }
    if (!rich.paragraphs.isEmpty()) {
        const Qt::Alignment alignment = rich.paragraphs.first().alignment;
        Attributes::Paragraph *paragraph = attributes->mutable_paragraph_style();
        // "Natural" alignment is left for left-to-right text; keep it if that is what was there.
        const bool natural = paragraph->alignment() == Attributes::ALIGNMENT_NATURAL;
        paragraph->set_alignment(alignment & Qt::AlignHCenter ? Attributes::ALIGNMENT_CENTER
                                 : alignment & Qt::AlignRight ? Attributes::ALIGNMENT_RIGHT
                                 : alignment & Qt::AlignJustify ? Attributes::ALIGNMENT_JUSTIFIED
                                 : natural ? Attributes::ALIGNMENT_NATURAL : Attributes::ALIGNMENT_LEFT);
    }

    // Ranges count characters through the whole text, a paragraph break counting as one,
    // and must stay inside it: there is no break after the last paragraph.
    attributes->clear_custom_attributes();
    QList<CapsRange> capitalized;
    const auto capitalize = [&capitalized](int start, int end, int capitalization) {
        if (capitalization == TextRun::NoCapitalization || end <= start)
            return;
        // Neighbouring stretches of the same capitalisation share a range.
        if (!capitalized.isEmpty() && capitalized.last().end == start && capitalized.last().capitalization == capitalization)
            capitalized.last().end = end;
        else
            capitalized.append({start, end, capitalization});
    };
    struct FontRange
    {
        int start;
        int end;
        const TextRun *run;
    };
    QList<FontRange> fonts;

    int offset = 0;
    for (qsizetype p = 0; p < rich.paragraphs.size(); ++p) {
        const TextParagraph &paragraph = rich.paragraphs.at(p);
        const int paragraphStart = offset;
        for (const TextRun &run : paragraph.runs) {
            const int end = offset + int(run.text.size());
            capitalize(offset, end, run.capitalization);
            // The RTF names fonts by PostScript name only. The family of the first is
            // kept above; the family of any other goes with its range, where
            // ProPresenter keeps the font a stretch of text was set in.
            if (end > offset && !run.fontName.isEmpty() && run.fontName != fontName)
                fonts.append({offset, end, &run});
            offset = end;
        }
        if (p + 1 < rich.paragraphs.size()) {
            // A paragraph with nothing in it has only its break to carry its capitalisation.
            if (offset == paragraphStart && !paragraph.runs.isEmpty())
                capitalize(offset, offset + 1, paragraph.runs.first().capitalization);
            ++offset;
        }
    }

    for (const CapsRange &range : std::as_const(capitalized)) {
        auto *custom = attributes->add_custom_attributes();
        custom->mutable_range()->set_start(range.start);
        custom->mutable_range()->set_end(range.end);
        custom->set_capitalization(Attributes::Capitalization(range.capitalization));
    }
    for (const FontRange &range : std::as_const(fonts)) {
        auto *custom = attributes->add_custom_attributes();
        custom->mutable_range()->set_start(range.start);
        custom->mutable_range()->set_end(range.end);
        rv::data::Font *original = custom->mutable_original_font();
        original->set_name(range.run->fontName.toStdString());
        original->set_family(range.run->family.toStdString());
        original->set_size(range.run->size);
        original->set_bold(range.run->bold);
        original->set_italic(range.run->italic);
    }
    for (const auto &custom : std::as_const(kept))
        *attributes->add_custom_attributes() = custom;
    addChordRanges(attributes, after, chordList);
}

QVariantMap toSlideMap(const rv::data::Slide &slide, const QString &label)
{
    QVariantList elements;
    for (const rv::data::Slide::Element &element : slide.elements())
        elements.append(toElementMap(element));

    // The text each element shows: its own, or that of the element it is linked to,
    // then put through its own transform.
    QList<RichText> shown;
    for (const QVariant &entry : std::as_const(elements)) {
        const QVariantMap element = entry.toMap();
        const RichText own = element.value("text").value<RichText>();
        RichText display = own;
        const auto restyled = [&own](const QString &text) {
            return RichText::plain(text, own.firstRun(),
                                   own.paragraphs.isEmpty() ? Qt::AlignHCenter : own.paragraphs.first().alignment);
        };
        if (element.value("linkKind").toString() == QLatin1String("element")) {
            const qsizetype source = findElement(elements, element.value("linkElementId").toString(),
                                                 element.value("linkElementName").toString());
            if (source >= 0) {
                const QString text = elements.at(source).toMap().value("text").value<RichText>().plainText();
                display = restyled(linkTransformed(text, element.value("linkTransform").toInt()));
            }
        } else if (element.value("linkKind").toString() == QLatin1String("timer")) {
            // What it shows changes while the slide is on show, so it is not known here:
            // whatever draws the element puts it in (see SlideElement.qml). This stands
            // in for it, as text of the same shape: the timer at nothing.
            const Timers::Format format {element.value("linkTimerHours").toInt(), element.value("linkTimerMinutes").toInt(),
                                         element.value("linkTimerSeconds").toInt(),
                                         element.value("linkTimerHundredths").toInt(),
                                         element.value("linkTimerHundredthsUnderMinute").toBool()};
            display = restyled(Timers::linked(0, true, format, element.value("linkTimerPattern").toString()));
        } else if (isLive(element)) {
            // The words of whatever slide is live when this one is shown: none, here.
            display = restyled(QString());
        } else if (element.value("linkKind").toString() == QLatin1String("other")) {
            // Something this app does not follow, the clock say. The text the element
            // has of its own is then only a sample of it ("1:23 PM"), which shown would
            // pass for the real thing; so it shows nothing, and the editor, where a
            // sample is what is wanted, draws the element's own text itself.
            display = restyled(QString());
        }
        const int transform = element.value("textTransform").toInt();
        if (transform != Text::TRANSFORM_NONE)
            display = restyled(elementTransformed(display.plainText(), transform,
                                                  element.value("textTransformDelimiter").toString()));
        shown.append(display);
    }

    QStringList texts;
    for (qsizetype i = 0; i < elements.size(); ++i) {
        QVariantMap element = elements.at(i).toMap();
        bool visible = !element.value("hidden").toBool();
        if (visible && element.value("visibilityRules").toBool()) {
            // A rule about something this app does not track counts as met. One about a
            // timer does too, here: whether it is met changes while the slide is on
            // show, so what draws the slide asks the timers (see Slide.qml), and is
            // told by `visibilityTimed` that it has to and by each condition's `met`
            // how the conditions that are settled here came out.
            int met = 0;
            bool timed = false;
            QVariantList conditions = element.value("visibilityConditions").toList();
            for (QVariant &entry : conditions) {
                QVariantMap condition = entry.toMap();
                bool holds = true;
                if (condition.value("kind").toString() == QLatin1String("element")) {
                    const qsizetype other = findElement(elements, condition.value("elementId").toString(),
                                                        condition.value("elementName").toString());
                    const bool hasText = other >= 0 && !shown.at(other).plainText().trimmed().isEmpty();
                    holds = hasText == condition.value("hasText").toBool();
                } else if (condition.value("timed").toBool()) {
                    timed = true;
                }
                if (holds)
                    ++met;
                condition.insert("met", holds);
                entry = condition;
            }
            element.insert("visibilityConditions", conditions);
            element.insert("visibilityTimed", timed);
            switch (element.value("visibilityCriterion").toInt()) {
            case DataLink::VisibilityLink::VISIBILITY_CRITERION_ANY:
                visible = conditions.isEmpty() || met > 0;
                break;
            case DataLink::VisibilityLink::VISIBILITY_CRITERION_NONE:
                visible = met == 0;
                break;
            default:
                visible = met == conditions.size();
                break;
            }
        }
        element.insert("displayText", QVariant::fromValue(shown.at(i)));
        // One whose text comes while it is shown has text, as far as drawing it goes.
        element.insert("hasText", !shown.at(i).isEmpty() || isLive(element));
        element.insert("visible", visible);
        elements[i] = element;
        // The slide's words are those of the elements that show, once each: an element
        // that repeats another's text adds nothing to them, and nor does one that shows
        // something that is not words of the slide at all, such as a timer.
        const bool repeats = element.value("linkKind").toString() == QLatin1String("element")
            && findElement(elements, element.value("linkElementId").toString(), element.value("linkElementName").toString()) >= 0;
        const QString text = shown.at(i).plainText().trimmed();
        if (visible && !repeats && !isLive(element) && !text.isEmpty())
            texts << text;
    }

    const bool hasSize = slide.size().width() > 0 && slide.size().height() > 0;
    return {
        {"width", hasSize ? slide.size().width() : 1920.0},
        {"height", hasSize ? slide.size().height() : 1080.0},
        {"drawsBackground", slide.draws_background_color()},
        {"backgroundColor", toColor(slide.background_color())},
        {"label", label},
        {"group", QString()},
        {"groupColor", QString()},
        {"groupStart", false},
        {"mediaName", QString()},
        {"mediaForeground", false},
        {"mediaVideo", false},
        {"mediaPlayback", 0},
        {"mediaLoopCount", 0},
        {"mediaLoopSeconds", 0.0},
        {"actions", QVariantList()},
        {"plainText", texts.join(u'\n')},
        {"elements", elements},
    };
}

bool applyChanges(rv::data::Slide *slide, const QString &elementId, const QVariantMap &changes)
{
    rv::data::Slide::Element *slideElement = findSlideElement(slide, elementId);
    if (!slideElement)
        return false;
    rv::data::Graphics::Element *element = slideElement->mutable_element();
    const auto number = [&changes](const char *key) { return changes.value(QLatin1String(key)).toDouble(); };
    const auto name = [slide](const QString &id) {
        const rv::data::Slide::Element *other = findSlideElement(slide, id);
        return other ? other->element().name() : std::string();
    };

    if (changes.contains("x"))
        element->mutable_bounds()->mutable_origin()->set_x(number("x"));
    if (changes.contains("y"))
        element->mutable_bounds()->mutable_origin()->set_y(number("y"));
    if (changes.contains("width"))
        element->mutable_bounds()->mutable_size()->set_width(qMax(1.0, number("width")));
    if (changes.contains("height"))
        element->mutable_bounds()->mutable_size()->set_height(qMax(1.0, number("height")));
    if (changes.contains("opacity"))
        element->set_opacity(qBound(0.0, number("opacity"), 1.0));
    // Degrees, clockwise, from 0 up to but not 360
    if (changes.contains("rotation"))
        element->set_rotation(std::fmod(std::fmod(number("rotation"), 360) + 360, 360));
    if (changes.contains("locked"))
        element->set_locked(changes.value("locked").toBool());
    if (changes.contains("hidden"))
        element->set_hidden(changes.value("hidden").toBool());
    if (changes.contains("name")) {
        const std::string renamed = changes.value("name").toString().trimmed().toStdString();
        if (!renamed.empty()) {
            element->set_name(renamed);
            // The other elements' links and rules name this one as well as identify it.
            const std::string id = element->uuid().string();
            for (rv::data::Slide::Element &other : *slide->mutable_elements()) {
                for (DataLink &link : *other.mutable_data_links()) {
                    if (link.has_alternate_text() && link.alternate_text().other_element_uuid().string() == id)
                        link.mutable_alternate_text()->set_other_element_name(renamed);
                    if (!link.has_visibility_link())
                        continue;
                    for (auto &condition : *link.mutable_visibility_link()->mutable_conditions()) {
                        if (condition.has_element_visibility()
                            && condition.element_visibility().other_element_uuid().string() == id)
                            condition.mutable_element_visibility()->set_other_element_name(renamed);
                    }
                }
            }
        }
    }

    // A rounded rectangle's outline is in its own proportions: see setShapePath().
    const bool rounded = element->path().shape().type() == rv::data::Graphics::Path::Shape::TYPE_ROUNDED_RECTANGLE;
    if (rounded && (changes.contains("width") || changes.contains("height") || changes.contains("roundness"))) {
        setShapePath(element->mutable_path(), rv::data::Graphics::Path::Shape::TYPE_ROUNDED_RECTANGLE,
                     changes.contains("roundness") ? number("roundness") : element->path().shape().rounded_rectangle().roundness(),
                     element->bounds().size().width(), element->bounds().size().height());
    }

    if (changes.contains("fillColor"))
        setColor(element->mutable_fill()->mutable_color(), changes.value("fillColor").value<QColor>());
    // What kind of fill it is. Going to a gradient starts from the colour there was.
    const auto gradient = [element]() {
        rv::data::Graphics::Fill *fill = element->mutable_fill();
        if (!fill->has_gradient() || fill->gradient().stops_size() < 2) {
            const QColor from = fill->has_color() ? toColor(fill->color()) : QColor::fromRgbF(0.13f, 0.59f, 0.95f);
            rv::data::Graphics::Gradient *made = fill->mutable_gradient();
            made->clear_stops();
            made->set_angle(270);
            made->set_length(1);
            for (const QColor &color : {from, from.darker(300)}) {
                auto *stop = made->add_stops();
                setColor(stop->mutable_color(), color);
                stop->set_blend_point(0.5);
            }
        }
        return fill->mutable_gradient();
    };
    if (changes.contains("fillKind")) {
        const QString kind = changes.value("fillKind").toString();
        rv::data::Graphics::Fill *fill = element->mutable_fill();
        if (kind == QLatin1String("gradient")) {
            gradient();
        } else if (kind == QLatin1String("color") && !fill->has_color()) {
            setColor(fill->mutable_color(), fill->has_gradient() && fill->gradient().stops_size() > 0
                                                ? toColor(fill->gradient().stops(0).color())
                                                : QColor::fromRgbF(0.13f, 0.59f, 0.95f));
        }
    }
    if (changes.contains("fillGradientFrom"))
        setColor(gradient()->mutable_stops(0)->mutable_color(), changes.value("fillGradientFrom").value<QColor>());
    if (changes.contains("fillGradientTo")) {
        rv::data::Graphics::Gradient *made = gradient();
        setColor(made->mutable_stops(made->stops_size() - 1)->mutable_color(), changes.value("fillGradientTo").value<QColor>());
    }
    if (changes.contains("fillGradientAngle"))
        gradient()->set_angle(std::fmod(std::fmod(number("fillGradientAngle"), 360) + 360, 360));
    // A picture or a video to fill it with, by its path; how it was scaled is kept.
    if (changes.contains("fillMediaPath")) {
        rv::data::Graphics::Fill *fill = element->mutable_fill();
        const auto scale = fill->has_media() ? fillDrawing(fill->media()).scale_behavior() : rv::data::Media::SCALE_BEHAVIOR_FIT;
        *fill->mutable_media() = workspace::mediaElement(changes.value("fillMediaPath").toString(), workspace::openWorkspace());
        rv::data::Media *media = fill->mutable_media();
        (media->has_video() ? media->mutable_video()->mutable_drawing() : media->mutable_image()->mutable_drawing())
            ->set_scale_behavior(scale);
        fill->set_enable(true);
    }
    if (changes.contains("fillMediaScale") && element->fill().has_media()) {
        rv::data::Media *media = element->mutable_fill()->mutable_media();
        (media->has_video() ? media->mutable_video()->mutable_drawing() : media->mutable_image()->mutable_drawing())
            ->set_scale_behavior(rv::data::Media::ScaleBehavior(qBound(0, changes.value("fillMediaScale").toInt(), 2)));
    }
    if (changes.contains("featherOn")) {
        element->mutable_feather()->set_enable(changes.value("featherOn").toBool());
        if (element->feather().enable() && element->feather().radius() <= 0)
            element->mutable_feather()->set_radius(0.05);
    }
    if (changes.contains("featherRadius"))
        element->mutable_feather()->set_radius(qBound(0.0, number("featherRadius"), 0.5));
    if (changes.contains("fillOn")) {
        element->mutable_fill()->set_enable(changes.value("fillOn").toBool());
        // Turned on with nothing to fill with, it is filled with black.
        if (element->fill().enable() && element->fill().FillType_case() == rv::data::Graphics::Fill::FILLTYPE_NOT_SET)
            setColor(element->mutable_fill()->mutable_color(), Qt::black);
    }
    if (changes.contains("fillLinesOnly"))
        element->mutable_text_line_mask()->set_enabled(changes.value("fillLinesOnly").toBool());

    if (changes.contains("strokeColor"))
        setColor(element->mutable_stroke()->mutable_color(), changes.value("strokeColor").value<QColor>());
    if (changes.contains("strokeWidth"))
        element->mutable_stroke()->set_width(qMax(0.0, number("strokeWidth")));
    if (changes.contains("strokeOn")) {
        element->mutable_stroke()->set_enable(changes.value("strokeOn").toBool());
        if (element->stroke().enable() && element->stroke().width() <= 0)
            element->mutable_stroke()->set_width(3);
    }

    if (anyKeyStartsWith(changes, QStringLiteral("shadow")))
        applyShadow(element->mutable_shadow(), QStringLiteral("shadow"), changes);
    if (anyKeyStartsWith(changes, QStringLiteral("textShadow")))
        applyShadow(element->mutable_text()->mutable_shadow(), QStringLiteral("textShadow"), changes);

    if (changes.contains("textScale"))
        element->mutable_text()->set_scale_behavior(Text::ScaleBehavior(qBound(0, changes.value("textScale").toInt(), 4)));
    if (changes.contains("verticalAlignment")) {
        const int alignment = changes.value("verticalAlignment").toInt();
        element->mutable_text()->set_vertical_alignment(alignment & Qt::AlignTop ? Text::VERTICAL_ALIGNMENT_TOP
                                                        : alignment & Qt::AlignBottom ? Text::VERTICAL_ALIGNMENT_BOTTOM
                                                        : Text::VERTICAL_ALIGNMENT_MIDDLE);
    }
    if (changes.contains("marginLeft"))
        element->mutable_text()->mutable_margins()->set_left(qMax(0.0, number("marginLeft")));
    if (changes.contains("marginTop"))
        element->mutable_text()->mutable_margins()->set_top(qMax(0.0, number("marginTop")));
    if (changes.contains("marginRight"))
        element->mutable_text()->mutable_margins()->set_right(qMax(0.0, number("marginRight")));
    if (changes.contains("marginBottom"))
        element->mutable_text()->mutable_margins()->set_bottom(qMax(0.0, number("marginBottom")));

    // Chords: whether the element draws those of the slide whose words it shows, and how.
    // The colour is written with the switch as ProPresenter writes it, which always
    // has one.
    if (changes.contains("chordsOn") || changes.contains("chordNotation") || changes.contains("chordColor")) {
        auto *chordPro = element->mutable_text()->mutable_chord_pro();
        if (changes.contains("chordsOn"))
            chordPro->set_enabled(changes.value("chordsOn").toBool());
        if (changes.contains("chordNotation"))
            chordPro->set_notation(rv::data::Graphics::Text::ChordPro::Notation(qBound(0, changes.value("chordNotation").toInt(), 3)));
        if (changes.contains("chordColor"))
            setColor(chordPro->mutable_color(), changes.value("chordColor").value<QColor>());
        else if (!chordPro->has_color())
            setColor(chordPro->mutable_color(), QColor(Qt::white));
    }

    // Where the text comes from: the element itself, another element of the slide, a
    // timer, or the slide that is live or the one after it. It is one of them, so making
    // it one takes away the link for another.
    static const char *const linkKeys[] = {"linkKind", "linkElementId", "linkTransform", "linkTimerId", "linkTimerName",
                                           "linkTimerHours", "linkTimerMinutes", "linkTimerSeconds", "linkTimerHundredths",
                                           "linkSlideNext", "linkSlideSource", "linkSlideName"};
    const bool linkChanges = std::any_of(std::begin(linkKeys), std::end(linkKeys),
                                         [&changes](const char *key) { return changes.contains(QLatin1String(key)); });
    if (linkChanges) {
        const auto existing = [slideElement](bool (DataLink::*has)() const) -> DataLink * {
            for (DataLink &link : *slideElement->mutable_data_links()) {
                if ((link.*has)())
                    return &link;
            }
            return nullptr;
        };
        const QString was = existing(&DataLink::has_alternate_text) ? QStringLiteral("element")
                          : existing(&DataLink::has_timer_text) ? QStringLiteral("timer")
                          : existing(&DataLink::has_slide_text) ? QStringLiteral("slideText") : QStringLiteral("none");
        const QString kind = changes.value("linkKind", was).toString();
        // The links of the other kinds go first, which may move the one that stays.
        removeLinks(slideElement, [&kind](const DataLink &link) {
            return (link.has_alternate_text() && kind != QLatin1String("element"))
                || (link.has_timer_text() && kind != QLatin1String("timer"))
                || (link.has_slide_text() && kind != QLatin1String("slideText"));
        });
        const auto transform = [&changes] {
            return DataLink::AlternateElementText::TextTransformOption(qBound(0, changes.value("linkTransform").toInt(), 3));
        };
        if (kind == QLatin1String("element")) {
            DataLink *link = existing(&DataLink::has_alternate_text);
            auto *alternate = (link ? link : slideElement->add_data_links())->mutable_alternate_text();
            if (changes.contains("linkElementId")) {
                const QString source = changes.value("linkElementId").toString();
                alternate->mutable_other_element_uuid()->set_string(source.toStdString());
                alternate->set_other_element_name(name(source));
            }
            if (changes.contains("linkTransform"))
                alternate->set_text_transform(transform());
        } else if (kind == QLatin1String("timer")) {
            DataLink *link = existing(&DataLink::has_timer_text);
            const bool fresh = link == nullptr;
            DataLink::TimerText *timer = (fresh ? slideElement->add_data_links() : link)->mutable_timer_text();
            // The link names the timer as well as identifying it, which is what finds
            // it in a workspace where the same timer was made separately.
            if (changes.contains("linkTimerId"))
                timer->mutable_timer_uuid()->set_string(changes.value("linkTimerId").toString().toStdString());
            if (changes.contains("linkTimerName"))
                timer->set_timer_name(changes.value("linkTimerName").toString().toStdString());
            const auto style = [&changes](const char *key) {
                return rv::data::Timer::Format::Style(qBound(0, changes.value(QLatin1String(key)).toInt(), 4));
            };
            if (changes.contains("linkTimerHours"))
                timer->mutable_timer_format()->set_hour(style("linkTimerHours"));
            if (changes.contains("linkTimerMinutes"))
                timer->mutable_timer_format()->set_minute(style("linkTimerMinutes"));
            if (changes.contains("linkTimerSeconds"))
                timer->mutable_timer_format()->set_second(style("linkTimerSeconds"));
            if (changes.contains("linkTimerHundredths"))
                timer->mutable_timer_format()->set_millisecond(style("linkTimerHundredths"));
            // What ProPresenter writes for a link that shows the time and nothing else
            if (fresh)
                timer->set_timer_format_string("${timer}");
        } else if (kind == QLatin1String("slideText")) {
            DataLink *link = existing(&DataLink::has_slide_text);
            DataLink::SlideText *words = (link ? link : slideElement->add_data_links())->mutable_slide_text();
            if (changes.contains("linkSlideNext"))
                words->set_source_slide(changes.value("linkSlideNext").toBool() ? DataLink::SLIDE_SOURCE_TYPE_NEXT_SLIDE
                                                                                : DataLink::SLIDE_SOURCE_TYPE_CURRENT_SLIDE);
            if (changes.contains("linkSlideSource"))
                words->set_source_option(DataLink::SlideText::TextSourceOption(qBound(0, changes.value("linkSlideSource").toInt(), 2)));
            if (changes.contains("linkSlideName"))
                words->set_name_to_match(changes.value("linkSlideName").toString().toStdString());
            if (changes.contains("linkTransform"))
                words->set_element_text_transform(transform());
        }
    }

    // When it is shown
    if (changes.contains("visibilityRules") && !changes.value("visibilityRules").toBool()) {
        removeLinks(slideElement, [](const DataLink &link) { return link.has_visibility_link(); });
    } else if (changes.contains("visibilityRules") || changes.contains("visibilityCriterion")
               || changes.contains("visibilityConditions")) {
        DataLink *existing = nullptr;
        for (DataLink &link : *slideElement->mutable_data_links()) {
            if (link.has_visibility_link()) {
                existing = &link;
                break;
            }
        }
        auto *rules = (existing ? existing : slideElement->add_data_links())->mutable_visibility_link();
        if (changes.contains("visibilityCriterion"))
            rules->set_visibility_criterion(DataLink::VisibilityLink::VisibilityCriterion(
                qBound(0, changes.value("visibilityCriterion").toInt(), 2)));
        if (changes.contains("visibilityConditions")) {
            const DataLink::VisibilityLink before = *rules;
            rules->clear_conditions();
            const QVariantList conditions = changes.value("visibilityConditions").toList();
            for (const QVariant &entry : conditions) {
                const QVariantMap condition = entry.toMap();
                if (condition.value("kind").toString() == QLatin1String("element")) {
                    using Criterion = DataLink::VisibilityLink::Condition::ElementVisibility;
                    const QString other = condition.value("elementId").toString();
                    const std::string otherName = name(other);
                    auto *added = rules->add_conditions()->mutable_element_visibility();
                    added->mutable_other_element_uuid()->set_string(other.toStdString());
                    added->set_other_element_name(otherName.empty() ? condition.value("elementName").toString().toStdString()
                                                                    : otherName);
                    added->set_visibility_criterion(condition.value("hasText").toBool()
                                                        ? Criterion::ELEMENT_VISIBILITY_CRITERION_HAS_TEXT
                                                        : Criterion::ELEMENT_VISIBILITY_CRITERION_HAS_NO_TEXT);
                } else {
                    // A condition on something this app does not track goes back as it was.
                    const int index = condition.value("index", -1).toInt();
                    if (index >= 0 && index < before.conditions_size())
                        *rules->add_conditions() = before.conditions(index);
                }
            }
        }
    }
    return true;
}

namespace {

// What a shape and a media element start from: a text box with no words in it, which is
// what every element of ProPresenter's is underneath, in the middle of the slide at the
// size given, stepping down and right while that place is taken.
rv::data::Slide::Element makeBareElement(const rv::data::Slide &slide, const QString &name, double width, double height)
{
    rv::data::Slide::Element result = makeTextElement(slide, nullptr);
    result.clear_info();
    rv::data::Graphics::Element *element = result.mutable_element();
    element->set_name(uniqueElementName(slide, name).toStdString());

    const bool hasSize = slide.size().width() > 0 && slide.size().height() > 0;
    const double slideWidth = hasSize ? slide.size().width() : 1920;
    const double slideHeight = hasSize ? slide.size().height() : 1080;
    double x = qRound((slideWidth - width) / 2);
    double y = qRound((slideHeight - height) / 2);
    const auto taken = [&slide](double x, double y) {
        for (const rv::data::Slide::Element &other : slide.elements()) {
            const auto &origin = other.element().bounds().origin();
            if (qAbs(origin.x() - x) < 1 && qAbs(origin.y() - y) < 1)
                return true;
        }
        return false;
    };
    while (taken(x, y) && y + 30 + height < slideHeight) {
        x += 30;
        y += 30;
    }
    element->mutable_bounds()->mutable_origin()->set_x(x);
    element->mutable_bounds()->mutable_origin()->set_y(y);
    element->mutable_bounds()->mutable_size()->set_width(width);
    element->mutable_bounds()->mutable_size()->set_height(height);

    // No words, but set as words typed into it later will be: smaller than a text
    // box's, since a shape is smaller than a slide.
    RichText words = readText(element->text());
    TextRun format = words.firstRun();
    format.size = qRound(slideHeight / 25);
    writeText(element->mutable_text(), RichText::plain(QString(), format, Qt::AlignHCenter));
    return result;
}

} // namespace

rv::data::Slide::Element makeShapeElement(const rv::data::Slide &slide, const QString &shape)
{
    using Shape = rv::data::Graphics::Path::Shape;
    const bool hasSize = slide.size().width() > 0 && slide.size().height() > 0;
    const double unit = (hasSize ? slide.size().height() : 1080) / 1080;
    struct Kind { const char *name; Shape::Type type; double width; double height; };
    const Kind kind = shape == QLatin1String("roundedRectangle") ? Kind {"Rounded Rectangle", Shape::TYPE_ROUNDED_RECTANGLE, 420, 300}
                    : shape == QLatin1String("ellipse") ? Kind {"Ellipse", Shape::TYPE_ELLIPSE, 360, 360}
                    : shape == QLatin1String("arrow") ? Kind {"Arrow", Shape::TYPE_RIGHT_ARROW, 420, 220}
                    : Kind {"Rectangle", Shape::TYPE_RECTANGLE, 420, 300};
    rv::data::Slide::Element result = makeBareElement(slide, QString::fromLatin1(kind.name), qRound(kind.width * unit),
                                                      qRound(kind.height * unit));
    rv::data::Graphics::Element *element = result.mutable_element();
    setShapePath(element->mutable_path(), kind.type, 0.2, element->bounds().size().width(), element->bounds().size().height());
    // A shape starts out filled with a plain colour: the blue ProPresenter starts one with.
    setColor(element->mutable_fill()->mutable_color(), QColor::fromRgbF(0.13f, 0.59f, 0.95f));
    element->mutable_fill()->set_enable(true);
    return result;
}

rv::data::Slide::Element makeMediaElement(const rv::data::Slide &slide, const QString &file)
{
    const bool hasSize = slide.size().width() > 0 && slide.size().height() > 0;
    const double slideWidth = hasSize ? slide.size().width() : 1920;
    const double slideHeight = hasSize ? slide.size().height() : 1080;
    // The picture's own size, if that is no more than half the slide each way, and
    // otherwise as large as fits in that; a video, whose size is not looked up, as a
    // screen of that width.
    QSizeF size = workspace::isVideo(file) ? QSizeF(16, 9) : QSizeF(QImageReader(file).size());
    if (!size.isValid() || size.isEmpty())
        size = QSizeF(16, 9);
    const QSizeF room(slideWidth / 2, slideHeight / 2);
    if (workspace::isVideo(file) || size.width() > room.width() || size.height() > room.height())
        size.scale(room, Qt::KeepAspectRatio);
    rv::data::Slide::Element result = makeBareElement(slide, QFileInfo(file).completeBaseName(), qRound(size.width()),
                                                      qRound(size.height()));
    rv::data::Graphics::Fill *fill = result.mutable_element()->mutable_fill();
    *fill->mutable_media() = workspace::mediaElement(file, workspace::openWorkspace());
    fill->set_enable(true);
    return result;
}

rv::data::Cue *addBlankCue(rv::data::Presentation *presentation, const std::string &name, const QSizeF &size)
{
    rv::data::Cue *cue = presentation->add_cues();
    cue->mutable_uuid()->set_string(workspace::newUuid());
    cue->set_name(name);
    cue->set_completion_action_type(rv::data::Cue::COMPLETION_ACTION_TYPE_LAST);
    cue->mutable_hot_key();
    cue->set_isenabled(true);

    rv::data::Action *slide = cue->add_actions();
    slide->mutable_uuid()->set_string(workspace::newUuid());
    slide->mutable_label()->set_text(name);
    slide->set_isenabled(true);
    slide->set_type(rv::data::Action::ACTION_TYPE_PRESENTATION_SLIDE);
    rv::data::Slide *base = slide->mutable_slide()->mutable_presentation()->mutable_base_slide();
    base->mutable_size()->set_width(size.width());
    base->mutable_size()->set_height(size.height());
    base->mutable_uuid()->set_string(workspace::newUuid());
    return cue;
}

QString uniqueElementName(const rv::data::Slide &slide, const QString &base)
{
    const auto taken = [&slide](const QString &name) {
        for (const rv::data::Slide::Element &element : slide.elements()) {
            if (QString::fromStdString(element.element().name()) == name)
                return true;
        }
        return false;
    };
    QString name = base;
    for (int n = 2; taken(name); ++n)
        name = QStringLiteral("%1 %2").arg(base).arg(n);
    return name;
}

rv::data::Slide::Element makeTextElement(const rv::data::Slide &slide, const rv::data::Slide::Element *like)
{
    const bool hasSize = slide.size().width() > 0 && slide.size().height() > 0;
    const double slideWidth = hasSize ? slide.size().width() : 1920;
    const double slideHeight = hasSize ? slide.size().height() : 1080;

    rv::data::Slide::Element result;
    result.set_info(rv::data::Slide::Element::INFO_IS_TEXT_ELEMENT);
    // The settings ProPresenter gives a text box that does not scroll.
    result.mutable_text_scroller()->set_scroll_rate(0.5);
    result.mutable_text_scroller()->set_should_repeat(true);
    result.mutable_text_scroller()->set_repeat_distance(0.05);

    rv::data::Graphics::Element *element = result.mutable_element();
    element->mutable_uuid()->set_string(workspace::newUuid());
    element->set_name(uniqueElementName(slide, QStringLiteral("Text")).toStdString());
    element->set_opacity(1);

    // In the middle of the slide, stepping down and right while that place is taken.
    const double width = qRound(slideWidth * 0.5);
    const double height = qRound(slideHeight * 0.2);
    double x = qRound((slideWidth - width) / 2);
    double y = qRound((slideHeight - height) / 2);
    const auto taken = [&slide](double x, double y) {
        for (const rv::data::Slide::Element &other : slide.elements()) {
            const auto &origin = other.element().bounds().origin();
            if (qAbs(origin.x() - x) < 1 && qAbs(origin.y() - y) < 1)
                return true;
        }
        return false;
    };
    while (taken(x, y) && y + 30 + height < slideHeight) {
        x += 30;
        y += 30;
    }
    element->mutable_bounds()->mutable_origin()->set_x(x);
    element->mutable_bounds()->mutable_origin()->set_y(y);
    element->mutable_bounds()->mutable_size()->set_width(width);
    element->mutable_bounds()->mutable_size()->set_height(height);

    // A rectangle, as a path on the unit square.
    rv::data::Graphics::Path *path = element->mutable_path();
    path->set_closed(true);
    for (const QPointF &corner : {QPointF(0, 0), QPointF(1, 0), QPointF(1, 1), QPointF(0, 1)}) {
        auto *point = path->add_points();
        for (rv::data::Graphics::Point *part : {point->mutable_point(), point->mutable_q0(), point->mutable_q1()}) {
            part->set_x(corner.x());
            part->set_y(corner.y());
        }
    }
    path->mutable_shape()->set_type(rv::data::Graphics::Path::Shape::TYPE_RECTANGLE);

    // No fill, stroke or shadow to begin with, but set up so that turning one on shows.
    setColor(element->mutable_fill()->mutable_color(), QColor(0, 0, 0, 128));
    element->mutable_stroke()->set_width(3);
    setColor(element->mutable_stroke()->mutable_color(), Qt::white);
    for (rv::data::Graphics::Shadow *shadow : {element->mutable_shadow(), element->mutable_text()->mutable_shadow()}) {
        shadow->set_angle(315);
        shadow->set_offset(5);
        shadow->set_radius(5);
        setColor(shadow->mutable_color(), Qt::black);
        shadow->set_opacity(0.75);
    }
    element->mutable_feather();

    rv::data::Graphics::Text *text = element->mutable_text();
    TextRun format;
    Qt::Alignment alignment = Qt::AlignHCenter;
    if (like && like->element().has_text()) {
        const RichText model = readText(like->element().text());
        format = model.firstRun();
        if (!model.paragraphs.isEmpty())
            alignment = model.paragraphs.first().alignment;
        // Its text shadow too, since that is part of how the text looks.
        *text->mutable_shadow() = like->element().text().shadow();
    } else {
        format.family = QFontDatabase::systemFont(QFontDatabase::GeneralFont).family();
        format.fontName = postScriptNameFor(format.family, false, false);
        format.size = qRound(slideHeight / 15);
        format.fill = Qt::white;
    }
    text->set_vertical_alignment(Text::VERTICAL_ALIGNMENT_MIDDLE);
    text->mutable_margins();
    text->set_is_superscript_standardized(true);
    text->mutable_attributes()->mutable_paragraph_style()->set_line_height_multiple(1);
    writeText(text, RichText::plain(QStringLiteral("Text"), format, alignment));
    return result;
}

} // namespace proconvert
