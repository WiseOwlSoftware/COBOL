#!/usr/bin/env python3
"""Replace every general format in the transcription with one generated from the printed page.

KEYED ON THE CLAUSE HIERARCHY, NEVER ON THE PAGE. A page number is a layout artifact — figures straddle page
breaks, and one clause's formats routinely span two pages — so a sweep keyed on "the nth block of page N"
mis-targets, and a mis-targeted replacement still looks like a figure. A figure's identity is its CLAUSE and
its FORMAT number (14.9.8.2 Format 2), which is what the printed page and the transcription agree on.

THE GATE, which is what makes this safe: a replacement is applied ONLY where the generated figure carries the
SAME WORDS as the text it replaces. Notation may change freely — that is the entire point of the sweep — but a
word appearing, vanishing or changing means the target is wrong or the page is one of the transcription's known
defects, and the figure is reported instead of written. `--report` shows what would happen without touching
anything; `--check` regenerates against the file and is the post-sweep regression gate.

THE TRANSCRIPTION DOES NOT FENCE EVERY FIGURE. Roughly a third of the general formats are one-liners set as
bare paragraphs — `\\>\\>compiler-instruction` on folio 54, `inline-invocation-1` and `identifier-2 object-view-1`
on folio 125. They are figures too, and they are where defects hide: folio 730 runs the NEXT format's label
into the figure line (`SET { identifier-7 } … TO identifier-8 Format 10 (data-pointer-arithmetic):`). Sweeping
only the fenced ones would leave the worst-transcribed third behind, in the old style.

    python scripts/spec/sweep_figures.py --report
    python scripts/spec/sweep_figures.py --report --pages 600 660
    python scripts/spec/sweep_figures.py --apply
    python scripts/spec/sweep_figures.py --check
    python scripts/spec/sweep_figures.py --check --edition 2002   # another edition (default 2023)
    python scripts/spec/sweep_figures.py --edition 1985 --cross-check --ledger F   # a scan: the two drawings compared
    python scripts/spec/sweep_figures.py --edition 1985 --apply   # a scan: the agreed and decided figures written

A SCANNED EDITION (1985) has no text layer, so its figures are found and worded by the transcription's own drawing and
regenerated from the measured ink (`regenerated`). A figure is written only when it is decided (`decide`): the two
drawings agree on every word and on the four things a general format carries, or a refuter's verdict in `verdicts_dir`
decides it against the printed page. Every other figure stays as drawn, and `--check` names it (kb/Work PB4849).
"""
from __future__ import annotations

import argparse
import collections
import io
import contextlib
import difflib
import pathlib
import re
import sys
from typing import NamedTuple

from spec_headings import GENERAL_FORMAT_NAMED, GENERAL_FORMAT_TITLE   # THE one spelling of the heading (kb/Work PB284)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)

PAGE_ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>\s*$')
MD_HEADING = re.compile(r"^\s*#{1,6}\s+\**(\d+(?:\.\d+)+)\s+(.+?)\**\s*$")
MD_FORMAT = re.compile(r"^\s*\**Formats?\s+(\d+)")
ADDENDUM_REF = re.compile(r"see the Addendum \(C\d+\)")
# A fence may carry a LANGUAGE TAG (```cobol on the example programs). Not matching it would leave the
# example's lines loose, where the bare-run scanner could mistake a COBOL example for a figure.
# `<pre>` carries a `line-height:1` style attribute (see render_figure.PRE_OPEN — it is what makes the
# box-drawing strokes meet), so the opening tag must be matched with its attributes, not as a bare tag.
FENCE = re.compile(r"^\s*(?:>\s?)?(?:```[a-zA-Z]*|</?pre(?:\s[^>]*)?>)\s*$")
FORMAT_LABEL = re.compile(r"^\s*(?:>\s?)?\*{0,2}Formats?\s+\d+\b")
HEADING = re.compile(r"^\s*#{1,6}\s")
# A row of a figure that carries only DELIMITERS — the blank row inside a group, drawn with the group's own
# stems. It is figure content, but it can neither open nor close a run: treating it as prose chopped every
# generated figure into as many spans as it had such rows.
NOTATION_ONLY = re.compile(r"^[\s│┤├╭╮╰╯┌┐└┘─|]*$")
ART_ONLY = re.compile(r"^[\sL⌐¬|┌┐└┘─│]*$")     # an ASCII-art corner row: `⌐   ¬` above, `L   ┘` below
# A blockquote, but NEVER a compiler directive: `>>POP` and `>> SOURCE FORMAT` are figure CONTENT. Matching
# them as quotes made the sweep re-emit a figure's own first line after the figure, as though it were a note.
BLOCKQUOTE = re.compile(r"^\s*>(?!>)")
HTML_LINE = re.compile(r"^\s*<")
HRULE = re.compile(r"^\s*-{3,}\s*$")           # a Markdown horizontal rule: page furniture by another name
# PAGE FURNITURE inside the transcription: the anchors, `## Page N` headings, running headers and folios the
# OCR carried across. It is not content, it is not a separator, and it is on its way OUT of the document
# (pages are not a thing in Markdown). Until then it must be TRANSPARENT: `**ISO/IEC 1989:2023 (E)**` passes
# the figure test — all upper case, no plain lower-case word — so it was being swept INTO figures, and a page
# break falling inside a figure split that figure into two spans that matched nothing.
#
# The running header and the reseller's stamp line are each edition's own (kb/Work PB4815): `FURNITURE_LINES` is keyed by
# edition, and an edition with no row exits naming the table (1985 and 2002 are written when their runs read those lines).
FURNITURE_LINES: dict[int, str] = {
    2023: r"""\#*\s*\**ISO/IEC\s+1989:2023\s*\(E\)\**
        |\**©\s*ISO/IEC\s+\d{4}\**
        |Licensed\ to\ .*""",
}


def page_furniture(edition: int) -> re.Pattern:
    """The pattern of a line of page furniture in `edition`'s transcription."""
    own = spec_pdf.edition_data(FURNITURE_LINES, edition, "page-furniture lines (sweep_figures.FURNITURE_LINES)")
    return re.compile(r"""^\s*(?:<a\ id="page-\d+"></a>
        |\#*\s*Page\ \d+
        |""" + own + r"""
        |\d{1,4})\s*$""", re.X)

# Notation is expected to change; WORDS are not. Everything here is stripped before the comparison: the house
# box-drawing set, the Miscellaneous Technical extensions the transcription used until now, ASCII delimiters,
# and the two ellipsis spellings.
NOTATION = (set(chr(c) for c in range(0x2500, 0x2580)) | set(chr(c) for c in range(0x239B, 0x23AE))
            # `/` separates stacked alternatives when the transcription sets a group inline; `⌐ ¬ L` are
            # corners in the older ASCII-art figures. All three are notation, none is a word.
            | set("[]{}()|…/⌐¬"))


def unescape(line: str) -> str:
    """Markdown escaping the transcription applies to figure text (`\\>\\>SOURCE`, `\\*`, `\\_`)."""
    return re.sub(r"\\([>*_`\[\]])", r"\1", line)


def words_of(text: str, furniture: re.Pattern, drop_notes: bool = False) -> collections.Counter:
    """The WORDS of a figure, with everything that is not a word removed.

    Tags, fence markers and back-ticks are markup, not content, and counting them made every comparison fail:
    the first run reported `<u>BY</u>` against `BY` and 229 "word differences" that were nothing of the kind.
    """
    # Strip a blockquote prefix, but never the first `>` of a COMPILER DIRECTIVE: `>>SOURCE FORMAT` is figure
    # text, and eating one of its angle brackets turned every directive format into a word mismatch.
    src = text.splitlines()
    if drop_notes:
        # ... but only OUTSIDE a fence. A good deal of the transcription sets its figures INSIDE a blockquote,
        # so every content line begins with `>` too; dropping those discarded the figures themselves.
        inside, filtered = False, []
        for l in src:
            if FENCE.match(l):
                inside = not inside
                filtered.append(l)
                continue
            if inside or not BLOCKQUOTE.match(l):
                filtered.append(l)
        src = filtered
    # PAGE FURNITURE carries no words either. `targets` already steps over it, but the comparison did not, so
    # a figure that straddles a page break was measured as containing "## Page 156".
    kept = [re.sub(r"^\s*>(?!>)\s?", "", l) for l in src
            if not FENCE.match(l) and not furniture.match(l) and not HRULE.match(l)]
    # `<u>` is ZERO-WIDTH — underlining never introduces a word boundary. Replacing it with a space split
    # `>><u>CALL-CONVENTION</u>` into two words and reported a difference against a markdown form whose
    # rendered text is identical, which is the wrong direction for a gate: it hides a real change behind a
    # spurious one. Every other tag still becomes a space, since those do separate content.
    body = re.sub(r"</?u>", "", unescape("\n".join(kept)))
    body = re.sub(r"</?[A-Za-z][^>]*>", " ", body).replace("`", " ")
    body = re.sub(r"&[a-zA-Z]+;?", " ", body)          # HTML entities used for indentation
    # The standard sets a dash four ways in figures — hyphen, figure dash, en dash, minus sign — and the
    # transcription picked one per site. Which glyph is used is typography, not a difference in the language.
    body = body.translate(str.maketrans({"‒": "-", "–": "-", "−": "-", "—": "-"}))
    # An ASCII-ART CORNER ROW carries no words. The older figures draw a bracket with `⌐ ¬` on top and
    # `L ┘` beneath, so the foot `L` reads as a capital letter — the only place a bare `L` appears in a
    # figure. Dropping whole art-only lines removes it without making `L` un-wordable everywhere.
    body = "\n".join("" if ART_ONLY.match(l) else l for l in body.splitlines())
    plain = "".join(" " if ch in NOTATION else ch for ch in body.replace("*", " "))
    plain = plain.replace("...", " ")
    # The separator period is punctuation, not a word, and the two sides disagree about whether it is attached:
    # the printed page gives `FACTORY.` as one token where the transcription writes `<u>FACTORY</u>.`. Trailing
    # punctuation is split off and standalone punctuation dropped, so the comparison stays about WORDS.
    out = collections.Counter()
    for w in plain.split():
        w = w.strip(".,;:")
        if w:
            out[w] += 1
    return out


def figure_ish(line: str, furniture: re.Pattern) -> bool:
    """Whether a bare markdown line could be a general format rather than prose.

    Same test the generator uses on the printed page: a figure's lower-case terms are all hyphenated
    (identifier-1, rounded-phrase), while prose is full of plain words.
    """
    # The blockquote test runs on the RAW line. Unescaping first turns the transcription's `\>\>SOURCE FORMAT`
    # into `>>SOURCE FORMAT`, which reads as a blockquote — so every compiler-directive format was discarded
    # as prose and its clause reported as having no figure at all.
    if not line.strip() or BLOCKQUOTE.match(line) or furniture.match(line):
        return False
    # Strip tags BEFORE the html test: parts of the transcription already carry their underlining, and
    # `<u>NULL</u>` or `<u>FACTORY</u>. [ … ]` starts with `<`, so those clauses were reported as having no
    # figure at all when in fact they were the ones already closest to the target form.
    t = re.sub(r"</?[A-Za-z][^>]*>", "", unescape(line)).strip()
    if not t or HEADING.match(t) or HTML_LINE.match(t) or FORMAT_LABEL.match(t):
        return False
    if t.startswith("|") or t.startswith("---"):
        return False                                    # table row or rule
    toks = [w.strip(" []{}.,;:()<>") for w in t.split()]
    plain = [w for w in toks if re.fullmatch(r"[a-z]{2,}", w)]
    # As on the printed side: a plain lower-case word usually means prose, but the standard writes a few
    # metavariables unhyphenated — `identifier-1( leftmost-position : [ length ] )` and
    # `qualified-data-name-1 [ ( subscript … ) ]` are figures. NOTATION with only a word or two of lower case
    # settles it; prose in these regions carries no brackets, braces or ellipsis at all.
    if plain and not (len(plain) <= 2 and re.search(r"[\[\]{}…]|\.\.\.", t)):
        return False
    return bool(re.search(r"[a-z]+-[a-z0-9]|[A-Z]{2,}|^>>", t))


def md_index(lines):
    """{(clause, format-number) -> (start, end)} — where each figure's content lives in the transcription.

    The same structure the generator reads off the printed page: a numbered `… General format(s)` heading opens
    a region, the next heading of any level closes it, and `Format N (…)` labels divide it.
    """
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if not GENERAL_FORMAT_TITLE.search(title):
            continue
        end = heads[n + 1][0] if n + 1 < len(heads) else len(lines)
        marks = [(j, int(m.group(1))) for j in range(i + 1, end) if (m := MD_FORMAT.match(lines[j]))]
        if not marks:
            out[(clause, None)] = (i + 1, end)
            continue
        for k, (j, num) in enumerate(marks):
            out[(clause, num)] = (j + 1, marks[k + 1][0] if k + 1 < len(marks) else end)
    return out


def clause_spans(lines):
    """{clause -> (start, end)} for every `… General format(s)` region, labels or not."""
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if GENERAL_FORMAT_TITLE.search(title):
            out[clause] = (i + 1, heads[n + 1][0] if n + 1 < len(heads) else len(lines))
    return out


def targets(lines, lo, hi, furniture):
    """Ordered figure spans inside one page slice, as (start, end_exclusive, kind).

    Both shapes are found: a fenced block, and a bare run of figure-ish lines. Anything else — prose, notes,
    headings, tables, the `Format N (…)` labels — is skipped.
    """
    out, i, in_fence, start = [], lo, False, None
    run = None
    while i < hi:
        line = lines[i]
        if FENCE.match(line):
            if in_fence:
                out.append((start, i + 1, "fence"))
                in_fence = False
            else:
                if run is not None:
                    out.append((run, i, "bare"))
                    run = None
                in_fence, start = True, i
            i += 1
            continue
        if not in_fence:
            if figure_ish(line, furniture):
                if run is None:
                    run = i
            elif run is not None and (not line.strip() or furniture.match(line)
                                      or NOTATION_ONLY.match(line)):
                pass                                     # blanks, furniture and delimiter-only rows keep it open
            elif run is not None:
                out.append((run, i, "bare"))
                run = None
        i += 1
    if run is not None:
        out.append((run, hi, "bare"))
    # A FENCED block is a figure by construction — its lines are code, and the transcription blockquotes some
    # of them, so the prose test would throw them away. Only a BARE run has to prove it is not prose.
    keep = [(a, b, k) for a, b, k in out
            if k == "fence" or any(figure_ish(lines[j], furniture) for j in range(a, b))]

    # ONE PRINTED FIGURE IS SOMETIMES SET AS SEVERAL BLOCKS. UNSTRING is split into two fences and a bare
    # `[ END-UNSTRING ]`; the options paragraph sets `OPTIONS.` above the fence holding its clauses. Adjacent
    # spans are therefore merged — but only across blanks, page furniture and the figure-notes blockquotes.
    #
    # A `Format N (…)` LABEL between them is the thing that must NOT be crossed, and it is exactly what
    # separates the qualification clause's ten figures from one another. Both cases are separated by a figure
    # note; only one is separated by a label, and that is the whole difference.
    def only_furniture(a, b):
        return all(not lines[j].strip() or furniture.match(lines[j]) or HRULE.match(lines[j])
                   for j in range(a, b))

    merged = []
    for span in keep:
        if merged and only_furniture(merged[-1][1], span[0]):
            merged[-1] = (merged[-1][0], span[1], span[2])
            continue
        merged.append(span)
    return merged


def note_lines(lines, a, b):
    """The figure NOTES inside a span — blockquote paragraphs only, never a blockquoted figure.

    A good deal of the transcription sets its figures INSIDE a blockquote, so their content lines carry the
    same `>` prefix a note does. Collecting by prefix alone re-emitted an entire old figure after its
    replacement; the fence state is what separates the two.
    """
    out, inside = [], False
    for j in range(a, b):
        if FENCE.match(lines[j]):
            inside = not inside
            continue
        if not inside and BLOCKQUOTE.match(lines[j]):
            out.append(lines[j])
    return out


def regroup(lines, spans, figures, furniture):
    """Group CONTIGUOUS markdown spans so that each group matches one printed figure, word for word.

    One printed figure is sometimes set as several blocks — UNSTRING is split into two fences and a bare
    `[ END-UNSTRING ]` — and consecutive figures are sometimes separated by nothing but a figure note. No
    fixed merge rule separates those two cases: crossing figure notes fixed UNSTRING and broke the
    file-control entry, whose formats are also separated by notes alone.

    So the grouping is not guessed, it is SOLVED, using the word gate as the decision procedure: find the
    partition of the spans into as many contiguous groups as there are figures, each group's words equal to
    its figure's. Returns None when no such partition exists, or when more than one does — an ambiguous
    grouping is reported rather than picked.
    """
    n, m = len(figures), len(spans)
    if not n or m < n:
        return None
    want = [words_of(chr(10).join(b), furniture) for _, _, b in figures]
    solutions = []

    def walk(i, j, acc):
        if len(solutions) > 1:
            return
        if i == n:
            if j == m:
                solutions.append(list(acc))
            return
        for k in range(j, m):
            # Measure the MERGED RANGE, not the sum of the spans in it. A group spans the gaps between its
            # blocks too, and those gaps hold the figure notes — summing spans accepted groupings that would
            # have swallowed a note into the figure, which is how the file-control entry slipped through.
            total = words_of(chr(10).join(lines[spans[j][0]:spans[k][1]]), furniture, drop_notes=True)
            if total == want[i]:
                acc.append((spans[j][0], spans[k][1], "group"))
                walk(i + 1, k + 1, acc)
                acc.pop()
            elif not (want[i] - total):
                break                                   # already past the figure's words
    walk(0, 0, [])
    return solutions[0] if len(solutions) == 1 else None


def generated(doc, extract, classify_page, R):
    """Every figure in the standard, in reading order, grouped by CLAUSE.

    Grouped by clause and not by (clause, Format): a Format label does not identify a figure uniquely. The
    qualification clause ends with two unnumbered `where … is:` definitions that inherit the last label, so
    keying on the label merged three distinct figures into one and the clause then looked short by two.
    Within a clause both sides run top to bottom, so the nth figure is the nth figure.
    """
    out, failed = collections.OrderedDict(), []
    for pno in range(1, doc.page_count + 1):
        page = doc[pno - 1]
        for band in R.find_figures(page, extract):
            clause, fmt = R.figure_key(doc, pno, band)
            try:
                with contextlib.redirect_stdout(io.StringIO()):
                    grid, marks = R.build(page, band[0], band[1], extract, classify_page)
            except SystemExit as exc:
                failed.append((clause, pno, str(exc)))
                continue
            if grid is None:
                continue
            out.setdefault(clause, []).append((pno, fmt, R.render(grid, marks, False)))
    return out, failed


class Drawn(NamedTuple):
    """One `<pre>` block of the transcription: lines[start] its opening tag and lines[end] its closing one, on the page
    whose `## Page N` heading precedes it, under the nearest clause heading and label. `general_format` says whether the
    block sits where a general format is printed: under a heading that names one (`spec_headings.GENERAL_FORMAT_NAMED`)
    or with a `Format N` label. The batch also set COBOL examples, flow diagrams and tables in `<pre>` (the table
    definitions of PDF page 96, the appendix's examples from page 763): those are not general formats, and their layout
    is their content, so no agreement ever rewrites one (`decide`)."""
    page: int
    start: int
    end: int
    rows: list
    clause: str | None
    label: str | None
    general_format: bool


PAGE_HEADING = re.compile(r"^## Page (\d+)\s*$")
PRE_OPEN_LINE = re.compile(r"^\s*<pre(?:\s[^>]*)?>\s*$")
CLAUSE_LINE = re.compile(r"^\s*#*\s*\**(\d+(?:\.\d+)+)\s+(.+?)\**\s*$")


def drawn_figures(lines: list[str]) -> list[Drawn]:
    """Every `<pre>` block of a transcription assembled page by page (`## Page N`, kb/Work PB4816), in order."""
    out, page, clause, label, heading, lead, start = [], None, None, None, "", "", None
    for i, line in enumerate(lines):
        if start is not None:
            if line.lstrip().startswith("</pre>"):
                fmt = bool(label) or any(GENERAL_FORMAT_NAMED.search(t) for t in (heading, lead))
                out.append(Drawn(page, start, i, lines[start + 1:i], clause, label, fmt))
                start = None
            continue
        if (m := PAGE_HEADING.match(line)):
            page = int(m.group(1))
            continue
        if PRE_OPEN_LINE.match(line):
            start = i
            continue
        if line.strip():
            lead = line                  # the text just before a block: "The general format ... is as follows:" (page 290)
        if (m := CLAUSE_LINE.match(line)):
            clause, label, heading = f"{m.group(1)} {m.group(2)}", None, m.group(2)
        elif HEADING.match(line):
            label, heading = None, re.sub(r"</?u>", "", line)
        elif FORMAT_LABEL.match(re.sub(r"</?u>", "", line)):
            label = re.sub(r"</?u>", "", line).strip()
    return [d for d in out if d.page is not None]


def regenerated(edition: int, doc, drawn: list[Drawn], R) -> list[tuple]:
    """The RASTER arm (kb/Work PB4849): every drawn figure regenerated from its scanned page, as
    [(figure, generated lines or None, pairing disagreements, measured boxes around it)] in `drawn`'s order.

    A scan has no text layer to find a figure by, so the figure is found by its WORDS: the transcription's drawing
    gives them, `figure_raster.measure_figures` places each figure on its page and pairs each word with the measured
    ink, and `render_figure.build` draws the figure from the measurement, exactly as for a vector page. The words are
    the drawing's; everything else (grouping, choice bars, underlines, alignment, level-2 boxes) is measured."""
    import figure_raster as FR
    import figure_reading
    from figure_extract import page_extractor
    from figure_geometry import page_classifier

    extract, classify_page = page_extractor(edition), page_classifier(edition)
    by_page = collections.defaultdict(list)
    for d in drawn:
        by_page[d.page].append(d)
    out = {}
    for pno, figs in by_page.items():
        rp, bands = FR.measure_figures(edition, pno, [figure_reading.token_rows(d.rows) for d in figs], doc)
        for k, (d, band) in enumerate(zip(figs, bands)):
            bad = [x for x in rp.disagreements if x.get("figure") == k and x.get("reason") != FR.TRIM_NOTE]
            body = None
            if band is not None:
                with contextlib.redirect_stdout(io.StringIO()):
                    try:
                        grid, marks = R.build(rp, band[0], band[1], extract, classify_page)
                        body = R.render(grid, marks, False) if grid else None
                    except SystemExit as exc:
                        bad.append({"reason": f"render_figure refused the figure: {exc}"})
            # The boxes `build` draws are the ones reaching into the band (`classify_page`'s box sides, FIGURE-STYLE.md
            # rule 9). A box is drawn only around words that were read, so one whose words stayed unread is lost from
            # the drawing, and that loss is named, never silent.
            boxes = [b for b in rp.boxes if band is not None and not (b["y1"] < band[0] or b["y0"] > band[1])]
            if body is not None:
                drawn_boxes = sum(1 for e in figure_reading.enclosures(body) if e.family == "box")
                if drawn_boxes < len(boxes):
                    bad.append({"reason": f"{len(boxes) - drawn_boxes} of {len(boxes)} measured level-2 box(es) not "
                                          "drawn: no word they hold was read"})
            out[d.start] = (d, body, bad, boxes)
    return [out[d.start] for d in drawn]


def cross_check(edition: int, doc, lines: list[str], R, pages: tuple | None, limit: int, ledger: str | None) -> int:
    """Regenerate every drawn figure of a raster edition and compare the two drawings on the four things a general
    format carries (`figure_reading.compare`). A figure the two agree on is ACCEPTED; every other is listed with what
    differs, for a refuter to decide against the printed page (kb/Work PB4849). Writes the result to `ledger` as JSON."""
    import json
    import figure_reading
    every = drawn_figures(lines)
    keyed = [(d, k) for d, k in zip(every, figure_ordinals(every)) if pages is None or pages[0] <= d.page <= pages[1]]
    drawn = [d for d, _ in keyed]
    results, stats = [], collections.Counter()
    for (d, k), (_, body, bad, boxes) in zip(keyed, regenerated(edition, doc, drawn, R)):
        diff = figure_reading.compare(d.rows, body) if body is not None else None
        things = [t for t in ("words",) + figure_reading.THINGS if diff and diff[t]]
        verdict = ("unlocated" if body is None else "unpaired" if bad else "disagree" if things else "agree")
        stats[verdict] += 1
        for t in things:
            stats[f"disagree on {t}"] += 1
        stats["boxes measured"] += len(boxes)
        # `page` and `figure` are the key a refuter's verdict file names, and `drawn` the block's lines it copies as its
        # `drawn_lines` (`load_verdicts`, `decide`); `general_format` is whether the block sits where a general format
        # is printed (`Drawn.general_format`).
        results.append({"page": d.page, "figure": k, "lines": [d.start + 1, d.end + 1], "clause": d.clause,
                        "label": d.label, "general_format": d.general_format, "verdict": verdict, "drawn": d.rows,
                        "differences": {t: diff[t] for t in things} if diff else {},
                        "pairing": bad, "boxes": len(boxes), "generated": body})
    print(f"drawn figures                    : {len(drawn)}")
    for key in ("agree", "disagree", "unpaired", "unlocated"):
        print(f"  {key:<31}: {stats[key]}")
    for t in ("words",) + figure_reading.THINGS:
        print(f"  disagree on {t:<19}: {stats[f'disagree on {t}']}")
    print(f"  measured level-2 boxes         : {stats['boxes measured']}")
    shown = [r for r in results if r["verdict"] != "agree"][:limit]
    for r in shown:
        print(f"\n  page {r['page']} lines {r['lines'][0]}-{r['lines'][1]} {r['clause'] or ''} {r['label'] or ''}: {r['verdict']}")
        for t, items in r["differences"].items():
            print(f"    {t}: {'; '.join(items[:3])}{' ...' if len(items) > 3 else ''}")
        for p in r["pairing"][:3]:
            print(f"    pairing: {p.get('reason')} {p.get('text', '')}")
    if ledger:
        pathlib.Path(ledger).write_text(json.dumps({"edition": edition, "figures": results}, indent=1, ensure_ascii=False),
                                        encoding="utf-8", newline="\n")
        print(f"\nwrote {ledger}")
    return 0


#: Which reading of a disputed figure the printed page bears out: the regenerated one, the batch's drawing, or neither
#: (the refuter drew it afresh); each of these verdicts carries the decided figure itself, drawn by FIGURE-STYLE.md.
#: Or NOT_A_FORMAT: the block is a COBOL example, a diagram or a table the batch set in `<pre>`, and stays as drawn.
NOT_A_FORMAT = "not-a-format"
DECIDED = ("generated", "drawn", "neither", NOT_A_FORMAT)


class Verdict(NamedTuple):
    """A refuter's decision on one disputed figure of a scanned edition (kb/Work PB4849): the figure is the `figure`-th
    `<pre>` block (from 0) of PDF page `page`, and `drawn` is that block's lines as the batch drew them when the refuter
    decided it (the cross-check ledger's `drawn`). The ordinal is the key and `drawn` the identity: `<pre>` blocks
    added or removed earlier on the page shift every later ordinal, and the blocks of a page usually share one clause
    and label, so only the block's own lines can say the ordinal still names it (`decide`)."""
    page: int
    figure: int
    drawn: tuple
    decided: str
    lines: tuple
    reason: str


def verdicts_dir(edition: int) -> pathlib.Path:
    """Where the refuters write their verdicts on `edition`'s figures: one JSON file per figure, `p<page>-<figure>.json`,
    beside the reconciliation's other ledgers (docs/rearchitecture/spec-reconciliation/README.md)."""
    return spec_pdf.REPO / "docs" / "rearchitecture" / "spec-reconciliation" / f"figures-{edition}"


def load_verdicts(directory: pathlib.Path) -> dict[tuple[int, int], Verdict]:
    """Every verdict file in `directory`, keyed by (page, figure). A malformed file or two files naming one figure is
    FATAL: a verdict that is silently dropped puts the batch's or the measurer's reading in the file unchecked."""
    import json
    out = {}
    for f in sorted(directory.glob("*.json")) if directory.is_dir() else []:
        try:
            j = json.loads(f.read_text(encoding="utf-8"))
            v = Verdict(int(j["page"]), int(j["figure"]), tuple(j["drawn_lines"]), j["decided"],
                        tuple(j.get("figure_lines") or ()), str(j.get("reason", "")))
        except (ValueError, KeyError, TypeError) as exc:
            sys.exit(f"FATAL: {f} is not a figure verdict ({exc}); it needs page, figure, drawn_lines and decided")
        if not v.drawn or not all(isinstance(x, str) for x in v.drawn):
            sys.exit(f"FATAL: {f}: `drawn_lines` must be the block's lines as drawn (the ledger's `drawn`), "
                     "the identity that says which block the verdict decides")
        if v.decided not in DECIDED:
            sys.exit(f"FATAL: {f}: `decided` must be one of {DECIDED}")
        if (v.decided == NOT_A_FORMAT) == bool(v.lines) or not all(isinstance(x, str) for x in v.lines):
            sys.exit(f"FATAL: {f}: a `{v.decided}` verdict " + ("carries no figure_lines" if v.decided == NOT_A_FORMAT
                     else "needs `figure_lines`, the decided figure's lines"))
        if not v.reason.strip():
            sys.exit(f"FATAL: {f}: a verdict states its `reason` (what the printed page shows)")
        if (v.page, v.figure) in out:
            sys.exit(f"FATAL: {f} and another verdict both decide figure {v.figure} of page {v.page}")
        out[(v.page, v.figure)] = v
    return out


def figure_ordinals(drawn: list[Drawn]) -> list[int]:
    """Each drawn figure's place among its page's figures, from 0: with the page, a figure's identity on a scan."""
    seen = collections.Counter()
    out = []
    for d in drawn:
        out.append(seen[d.page])
        seen[d.page] += 1
    return out


def decide(d: Drawn, figure: int, body: list[str] | None, bad: list, diff: dict | None,
           verdict: Verdict | None) -> tuple[list[str] | None, str]:
    """What a scanned edition's `<pre>` block is written as, and why:
    - (the decided figure, "verdict"): a refuter's verdict decides it, or (None, NOT_A_FORMAT) the refuter found it no
      general format, and it stays as drawn;
    - (the regeneration, "agree"): a general format whose every word was read and whose two drawings carry the same
      words and the same four things (and boxes), so writing it changes the notation only (PB4812 rule 1: a general
      format is drawn from the measured page);
    - (None, "unclassified"): a block that is not where a general format is printed (`Drawn.general_format`). Two
      drawings of a COBOL example can agree on every word while the regeneration loses the example's indentation, so
      no agreement rewrites one: it waits for a refuter to say what it is;
    - (None, "awaiting"): any other general format stays as the batch drew it until a refuter decides it."""
    if verdict is not None:
        # The block the ordinal names holds either the lines the refuter decided on, or (written) the decided figure;
        # a `not-a-format` block stays as drawn. Any other block is a different one, shifted under the ordinal.
        if tuple(d.rows) not in (verdict.drawn,) + ((verdict.lines,) if verdict.lines else ()):
            sys.exit(f"FATAL: the verdict on figure {figure} of page {d.page} decides a block that begins "
                     f"{verdict.drawn[0]!r}, but figure {figure} of that page is now the block that begins "
                     f"{(d.rows or [''])[0]!r}: the transcription moved under the verdict")
        return (None, NOT_A_FORMAT) if verdict.decided == NOT_A_FORMAT else (list(verdict.lines), "verdict")
    if not d.general_format:
        return None, "unclassified"
    if body is not None and not bad and diff is not None and not any(diff.values()):
        return body, "agree"
    return None, "awaiting"


def raster_sweep(edition: int, doc, lines: list[str], R, pages: tuple | None, clause: str | None, mode: str,
                 limit: int, spec_md: pathlib.Path) -> int:
    """`--report`, `--apply` and `--check` for a scanned edition (kb/Work PB4849). Every `<pre>` block is regenerated and
    written as `decide` says; `--check` is clean when every block is in its decided form, none awaits a verdict and
    every verdict names a block of the file."""
    import figure_reading
    every = drawn_figures(lines)
    keyed = [(d, k) for d, k in zip(every, figure_ordinals(every))
             if (pages is None or pages[0] <= d.page <= pages[1])
             and (clause is None or (d.clause or "").split(" ")[0] == clause)]
    verdicts = load_verdicts(verdicts_dir(edition))
    whole = pages is None and clause is None
    stale = sorted(set(verdicts) - {(d.page, k) for d, k in keyed}) if whole else []
    stats, edits, awaiting = collections.Counter(), [], []
    for (d, k), (_, body, bad, _boxes) in zip(keyed, regenerated(edition, doc, [d for d, _ in keyed], R)):
        diff = figure_reading.compare(d.rows, body) if body is not None else None
        want, why = decide(d, k, body, bad, diff, verdicts.get((d.page, k)))
        stats[why] += 1
        if want is None:
            if why != NOT_A_FORMAT:
                awaiting.append(d)
            continue
        repl = [R.PRE_OPEN] + want
        if lines[d.start:d.end] == repl:
            stats["already"] += 1
        else:
            edits.append((d, repl, why))
    print(f"drawn <pre> blocks               : {len(keyed)}")
    print(f"  decided by a verdict           : {stats['verdict']}")
    print(f"  no general format (a verdict)  : {stats[NOT_A_FORMAT]}")
    print(f"  agreed (written regenerated)   : {stats['agree']}")
    print(f"  awaiting a verdict             : {stats['awaiting']}")
    print(f"  not where a format is printed  : {stats['unclassified']} (awaiting a verdict)")
    print(f"  already in their decided form  : {stats['already']}")
    print(f"  to write                       : {len(edits)}")
    print(f"  verdicts naming no figure      : {len(stale)}")
    for page, k in stale:
        print(f"    figure {k} of page {page}: no such drawn figure")
    if mode in ("report", "check"):
        for d, repl, why in edits[:limit]:
            print(f"  page {d.page} {d.clause or ''} {d.label or ''} ({why}), {spec_md.name} lines {d.start + 1}-{d.end}")
            for line in difflib.unified_diff(lines[d.start:d.end], repl, "transcription", "decided", n=0, lineterm=""):
                print(f"    {line}")
    if mode == "apply":
        if not edits:
            print("\nnothing to apply")
            return 0
        out = list(lines)
        for d, repl, _ in sorted(edits, key=lambda e: e[0].start, reverse=True):
            out[d.start:d.end] = repl
        spec_md.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied {len(edits)} figure(s) to {spec_md}")
        return 0
    if mode == "check":
        ok = not edits and not awaiting and not stale
        print("\nCHECK: clean — every figure is in its decided form." if ok else
              f"\nCHECK: {len(edits)} figure(s) drifted from their decided form, {len(awaiting)} await a verdict, "
              f"{len(stale)} verdict(s) name no figure.")
        return 0 if ok else 1
    return 0


def self_test() -> int:
    """`drawn_figures`, `figure_ordinals`, `decide` and `load_verdicts` on planted inputs (no PDF is read)."""
    import json
    import tempfile
    fails, ran = [], []

    def check(name, ok, detail=""):
        ran.append(name)
        if not ok:
            fails.append(f"{name} {detail}")

    def exits(fn) -> str | None:
        try:
            fn()
        except SystemExit as exc:
            return str(exc)
        return None

    md = ["## Page 304", "", "### 6.6.2 General Format", "", "<u>Format 1</u>:", "", "<pre>", "<u>ADD</u> x", "</pre>", "",
          "## Page 305", "", "<u>Format 2</u>:", "", '<pre style="line-height:1">', "ADD y", "", "    GIVING z",
          "</pre>Numeric Literals", "", "6.7.2 General Format", "", "<pre>", "ALTER", "</pre>",
          "## Page 306", "### 4.1 TABLE DEFINITION", "", "<pre>", "01  TABLE-1.", "    02  DOG  PIC X.", "</pre>",
          "The general format of a sign condition is as follows:", "", "## Page 307", "<pre>", "identifier IS ZERO",
          "</pre>", "## GENERAL FORMAT FOR COBOL VERBS", "<pre>", "<u>ACCEPT</u> x", "</pre>"]
    got = drawn_figures(md)
    want = [Drawn(304, 6, 8, ["<u>ADD</u> x"], "6.6.2 General Format", "Format 1:", True),
            Drawn(305, 14, 18, ["ADD y", "", "    GIVING z"], "6.6.2 General Format", "Format 2:", True),
            Drawn(305, 22, 24, ["ALTER"], "6.7.2 General Format", None, True),
            Drawn(306, 28, 31, ["01  TABLE-1.", "    02  DOG  PIC X."], "4.1 TABLE DEFINITION", None, False),
            Drawn(307, 35, 37, ["identifier IS ZERO"], "4.1 TABLE DEFINITION", None, True),
            Drawn(307, 39, 41, ["<u>ACCEPT</u> x"], "4.1 TABLE DEFINITION", None, True)]
    check("drawn_figures, each block's general-format context", got == want, str(got))
    check("a figure's ordinal counts from 0 on each page", figure_ordinals(got) == [0, 0, 1, 0, 0, 1],
          str(figure_ordinals(got)))

    d = got[0]
    house = ["<u>ADD</u> x"]
    none = {"words": [], "grouping": [], "bars": [], "underlines": [], "alignment": [], "boxes": []}
    check("an agreed figure is written regenerated", decide(d, 0, house, [], none, None) == (house, "agree"))
    check("a disagreement awaits a verdict",
          decide(d, 0, house, [], dict(none, underlines=["x: drawn [], generated [0]"]), None) == (None, "awaiting"))
    check("an unread word awaits a verdict", decide(d, 0, house, [{"reason": "unpaired"}], none, None) == (None, "awaiting"))
    check("an unlocated figure awaits a verdict", decide(d, 0, None, [], None, None) == (None, "awaiting"))
    v = Verdict(304, 0, ("<u>ADD</u> x",), "neither", ("<u>ADD</u> <u>x</u>",), "the page underlines x")
    check("a verdict outranks an agreement", decide(d, 0, house, [], none, v) == (["<u>ADD</u> <u>x</u>"], "verdict"))
    written = d._replace(rows=["<u>ADD</u> <u>x</u>"])
    check("a written verdict names its own block",
          decide(written, 0, house, [], none, v) == (["<u>ADD</u> <u>x</u>"], "verdict"))
    moved = v._replace(drawn=("<u>SUBTRACT</u> y",))
    check("a verdict on a figure that moved is fatal", (exits(lambda: decide(d, 0, house, [], none, moved)) or "")
          .startswith("FATAL: the verdict on figure 0 of page 304"))
    # The review's scenario (wave 20261897, R1-1): three blocks on one page under one clause and no label; the
    # furniture block 0 is removed, so the format the verdict on figure 1 decides becomes figure 0, and figure 1 is the
    # trailing furniture, under the same clause and label. The verdict must refuse it, not decide the furniture.
    page = ["## Page 202", "### 5.1 FILE DESCRIPTION", "<pre>", "GENERAL FORMAT", "</pre>", "<pre>", "<u>FD</u> file-name",
            "</pre>", "<pre>", "V-10", "</pre>"]
    before, after = drawn_figures(page), drawn_figures(page[:2] + page[5:])
    fd = Verdict(202, 1, tuple(before[1].rows), "neither", ("<u>FD</u> <u>file-name</u>",), "the page underlines")
    check("the planted page keeps one clause and label on every block",
          {(b.clause, b.label) for b in before + after} == {("5.1 FILE DESCRIPTION", None)}, str(before + after))
    check("a verdict on a block shifted by an earlier block's removal is fatal",
          (exits(lambda: decide(after[1], 1, house, [], none, fd)) or "").startswith("FATAL: the verdict on figure 1 of page 202"))
    check("the shifted block's verdict decides it while its ordinal stands",
          decide(before[1], 1, house, [], none, fd) == (["<u>FD</u> <u>file-name</u>"], "verdict"))
    example = got[3]
    check("an agreed example is never rewritten", decide(example, 0, ["01 TABLE-1."], [], none, None) == (None, "unclassified"))
    nf = Verdict(306, 0, tuple(example.rows), NOT_A_FORMAT, (), "a COBOL example of a table")
    check("a not-a-format verdict leaves the block as drawn",
          decide(example, 0, ["01 TABLE-1."], [], none, nf) == (None, NOT_A_FORMAT))

    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        good = {"page": 304, "figure": 0, "drawn_lines": ["<u>ADD</u> x"], "decided": "neither",
                "figure_lines": ["<u>ADD</u> <u>x</u>"], "reason": "the page underlines x"}
        (root / "p0304-0.json").write_text(json.dumps(good), encoding="utf-8", newline="\n")
        check("a verdict file loads", load_verdicts(root) == {(304, 0): v}, str(load_verdicts(root)))
        check("no directory, no verdicts", load_verdicts(root / "absent") == {})
        (root / "p0304-0b.json").write_text(json.dumps(good), encoding="utf-8", newline="\n")
        check("two verdicts on one figure are fatal", "both decide figure 0 of page 304" in (exits(lambda: load_verdicts(root)) or ""))
        (root / "p0304-0b.json").write_text(json.dumps(dict(good, figure=1, decided="batch")), encoding="utf-8", newline="\n")
        check("an unknown decision is fatal", "`decided` must be one of" in (exits(lambda: load_verdicts(root)) or ""))
        (root / "p0304-0b.json").write_text(json.dumps({"page": 304}), encoding="utf-8", newline="\n")
        check("a malformed verdict is fatal", "is not a figure verdict" in (exits(lambda: load_verdicts(root)) or ""))
        second = root / "p0304-0b.json"
        for name, body, says in (
                ("a not-a-format verdict carrying a figure is fatal", dict(good, figure=1, decided=NOT_A_FORMAT),
                 "carries no figure_lines"),
                ("a decided verdict without its figure is fatal", dict(good, figure=1, figure_lines=[]), "needs `figure_lines`"),
                ("a verdict without its reason is fatal", dict(good, figure=1, reason=" "), "states its `reason`"),
                ("a verdict without the block's drawn lines is fatal", dict(good, figure=1, drawn_lines=[]),
                 "`drawn_lines` must be the block's lines")):
            second.write_text(json.dumps(body), encoding="utf-8", newline="")
            check(name, says in (exits(lambda: load_verdicts(root)) or ""))

    total = len(ran)
    print(f"sweep_figures self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
    for f in fails:
        print("  FAIL", f)
    return 1 if fails else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the self-test (no PDF is read)")
    ap.add_argument("--report", action="store_true", help="show what would change; write nothing")
    ap.add_argument("--apply", action="store_true", help="rewrite the transcription in place")
    ap.add_argument("--check", action="store_true", help="regenerate and diff against the file (regression gate)")
    ap.add_argument("--cross-check", action="store_true",
                    help="a scanned edition: regenerate each drawn figure and compare the two drawings (kb/Work PB4849)")
    ap.add_argument("--ledger", help="with --cross-check: write every figure's result to this JSON file")
    ap.add_argument("--pages", nargs=2, type=int, metavar=("FROM", "TO"), help="a scanned edition: only these PDF pages")
    ap.add_argument("--clause", help="restrict to one clause number, e.g. 14.9.8.2")
    ap.add_argument("--limit", type=int, default=40, help="how many problems to list")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if spec_pdf.geometry(args.edition) == "raster":
        modes = [m for m in ("cross_check", "report", "apply", "check") if getattr(args, m)]
        if len(modes) != 1:
            sys.exit("FATAL: a scanned edition takes exactly one of --cross-check, --report, --apply, --check")
        import pymupdf
        sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
        import render_figure as R
        doc = pymupdf.open(spec_pdf.require(args.edition))
        spec_md = spec_pdf.markdown(args.edition)
        lines = spec_md.read_text(encoding="utf-8").splitlines()
        pages = tuple(args.pages) if args.pages else None
        if args.cross_check:
            return cross_check(args.edition, doc, lines, R, pages, args.limit, args.ledger)
        return raster_sweep(args.edition, doc, lines, R, pages, args.clause, modes[0], args.limit, spec_md)
    if args.cross_check or args.pages:
        sys.exit(f"FATAL: --cross-check and --pages are a scanned edition's; the {args.edition} edition is vector, and "
                 "--check regenerates it exactly")
    pdf = spec_pdf.require(args.edition)
    spec_md = spec_pdf.markdown(args.edition)
    furniture = page_furniture(args.edition)
    folio_offset = spec_pdf.folio_offset(args.edition)
    import pymupdf

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import render_figure as R
    from figure_extract import page_extractor
    from figure_geometry import page_classifier

    extract, classify_page = page_extractor(args.edition), page_classifier(args.edition)
    doc = pymupdf.open(pdf)
    lines = spec_md.read_text(encoding="utf-8").splitlines()
    index = md_index(lines)
    clause_span = clause_spans(lines)
    figs, failed = generated(doc, extract, classify_page, R)

    stats = collections.Counter()
    edits, problems = [], []
    for clause, figures in figs.items():
        if args.clause and clause != args.clause:
            continue
        stats["figures"] += len(figures)
        if clause is None:
            problems.append((clause, figures[0][0], None, "no clause could be resolved"))
            stats["no_clause"] += len(figures)
            continue
        whole = clause_span.get(clause)
        if whole is None:
            problems.append((clause, figures[0][0], None, "no such clause region in the transcription"))
            stats["no_target"] += len(figures)
            continue
        spans = targets(lines, *whole, furniture)
        if len(spans) != len(figures):
            spans = regroup(lines, spans, figures, furniture)
        if spans is None or len(spans) != len(figures):
            found = len(targets(lines, *whole, furniture))
            problems.append((clause, figures[0][0], None,
                             f"{len(figures)} printed figure(s) but {found} in the transcription, "
                             f"and no grouping of them matches"))
            stats["count_mismatch"] += len(figures)
            continue
        for (a, b, _kind), (pno, fmt, body) in zip(spans, figures):
            have = words_of(chr(10).join(lines[a:b]), furniture, drop_notes=True)
            want = words_of(chr(10).join(body), furniture)
            # A DOCUMENTED CORRECTION outranks the printed page. Where the transcription deliberately departs
            # from the standard it says so in place and lists the change in the Addendum, so regenerating from
            # the page would silently revert it — 12.3.6.2 corrects the standard's `locae-name-1` under C3.
            near = chr(10).join(lines[max(a - 3, 0):min(b + 8, len(lines))])
            if have != want and ADDENDUM_REF.search(near):
                stats["documented_correction"] += 1
                continue
            if have != want:
                problems.append((clause, pno, fmt, f"words differ — generated-only "
                                 f"{list((want - have).elements())[:4]}, markdown-only "
                                 f"{list((have - want).elements())[:4]}"))
                stats["word_mismatch"] += 1
                continue
            # Any figure NOTES that sat between the fragments are carried through, after the figure they
            # describe. They are the transcription's own analysis of the printed page and must not be lost.
            notes = note_lines(lines, a, b)
            repl = [R.PRE_OPEN] + body + ["</pre>"]
            if notes:
                repl += [""] + notes
            if lines[a:b] == repl:
                stats["already_current"] += 1
                continue
            edits.append((a, b, repl, clause, pno, fmt))
            stats["replaceable"] += 1

    print(f"figures generated                : {stats['figures']}")
    print(f"  replaceable (words match)      : {stats['replaceable']}")
    print(f"  already in generated form      : {stats['already_current']}")
    print(f"  no clause resolved             : {stats['no_clause']}")
    print(f"  no such clause region          : {stats['no_target']}")
    print(f"  figure-count mismatch          : {stats['count_mismatch']}")
    print(f"  documented correction (kept)   : {stats['documented_correction']}")
    print(f"  words differ                   : {stats['word_mismatch']}")
    print(f"  generator failed               : {len(failed)}")

    if (args.report or args.check) and problems:
        print(f"\nneeding attention ({len(problems)}):")
        for clause, pno, fmt, why in problems[:args.limit]:
            lbl = f" Format {fmt}" if fmt else ""
            print(f"  §{clause}{lbl} (folio {pno - folio_offset})  {why}")
        if len(problems) > args.limit:
            print(f"  … and {len(problems) - args.limit} more")

    # NAME EVERY DRIFTED FIGURE, with the change `--apply` would make. A count alone sent the reader of a red
    # check hunting for which figure moved and how (kb/Work PB3530: the one drifted figure was found by hand),
    # and the diff is the evidence a reviewer needs to tell a renderer regression from a stale transcription.
    if (args.report or args.check) and edits:
        print(f"\ndrifted from the generated form ({len(edits)}):")
        for a, b, repl, clause, pno, fmt in edits[:args.limit]:
            lbl = f" Format {fmt}" if fmt else ""
            print(f"  §{clause}{lbl} (folio {pno - folio_offset}), {spec_md.name} lines {a + 1}-{b}")
            for line in difflib.unified_diff(lines[a:b], repl, "transcription", "generated", n=0, lineterm=""):
                print(f"    {line}")
        if len(edits) > args.limit:
            print(f"  … and {len(edits) - args.limit} more")

    if args.apply:
        if not edits:
            print("\nnothing to apply")
            return 0
        out = list(lines)
        for a, b, repl, *_ in sorted(edits, key=lambda e: e[0], reverse=True):
            out[a:b] = repl
        spec_md.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied {len(edits)} figure(s) to {spec_md}")
        return 0
    if args.check:
        ok = not stats["replaceable"]
        print("\nCHECK: clean — every located figure matches its generated form." if ok else
              f"\nCHECK: {stats['replaceable']} figure(s) have DRIFTED from the generated form.")
        return 0 if ok else 1
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
