#!/usr/bin/env python3
"""text_layer.py - the TEXT-LAYER backend of transcribe_pdf.py (kb/Work PB4813): one PDF page's own text layer to Markdown.

transcribe_pdf.py is THE converter; this module is its third backend, beside the Message Batches API and the local model,
for an edition whose PDF carries a true text layer (2002). No model reads the page: the words, their fonts and their
positions ARE the transcription, so the standard's own wording, spelling and defects are kept as printed. `transcribe_pdf.py
text` calls `read_layout` (PyMuPDF, imported inside the function: CI has none) and `convert`; nothing here has a command line.

WHAT THE PAGE DECIDES (PDF-TEXT-LAYER.md, TRANSCRIPTION-STATE.md):
- Rows. Spans whose vertical centres agree are one printed row, read left to right; a page set in two columns (the index)
  is read column by column. Ligatures (fi, fl) become their letters. A row ending in a hyphen joins the next row with no
  space: the standard's own rule (2002 5.6, "Hyphens in text": a hyphen at the end of a line is part of the word it
  divides, and none is added to divide one).
- Furniture. The running header (the edition line and the clause title above the body), and the footer's folio and
  copyright line, are page furniture and are dropped; the folio is returned. The per-copy stamp is dropped by the caller's
  stamp test (verify_publishable.FORBIDDEN of the edition) and never reaches a page.
- Headings. A bold row of at least HEADING_SIZE points opening with a clause number (`14.8.24`, `A.1.2`) is a heading, its
  depth the number's; a run of large bold rows (an Annex title) is one heading.
- Rules and notes. A row opening with a separate label span (`1)`, `a)`, `1.`) is a numbered rule, its level the label's
  indentation; a small-type row opening `NOTE` is a note. Labels are escaped (`1\\)`, `1\\.`): Markdown re-parses a rule
  label as a list marker. Nothing is indented four spaces (that is a code block): a nested level is indented three.
- Paragraphs. A row continues the paragraph above when the gap between them is a line's leading and the row above runs to
  the column's right edge; a short row followed at a line's leading (the index, the contents, a word list) is a line of its
  own, a hard break, its sub-level marked by an em space per indentation step.
- Tables. A ruled table (PyMuPDF's line-strategy table finder) becomes a Markdown table at its place in the flow.
- Code. Rows in Courier become a <pre> block, columns placed from the measured x at the font's fixed pitch.
- General formats are NOT transcribed from text (FIGURE-STYLE.md): a general format's rows (the rows under a `General
  format` heading, and any row carrying a Symbol-font brace or bracket piece) become a FIGURE_MARK placeholder naming the
  page, the figure and its measured vertical extent, which the vector arm replaces with the drawn figure. The assembly
  refuses a page that still holds one.
- Ambiguity. A page whose structure the text layer does not decide is reported (`Converted.ambiguous`): an image, vector
  strokes outside every table and general format, a general-format glyph outside a General format clause, a row with
  column gaps (an unruled table), no text at all. The local model may read such a page only as a reading the page decides.

Self-test: scripts/spec/test_transcribe_pdf.py (planted layouts; with PyMuPDF present, a planted PDF it writes itself).
"""
from __future__ import annotations

import contextlib
import dataclasses
import io
import re
from typing import Any, Callable, Iterable

from spec_headings import GENERAL_FORMAT_TITLE  # THE general-format heading (kb/Work PB284)

#: The printed ligatures the text layer carries as one glyph (2002: "identiﬁer"), and their letters.
LIGATURES = {"ﬀ": "ff", "ﬁ": "fi", "ﬂ": "fl", "ﬃ": "ffi", "ﬄ": "ffl"}
#: The Symbol font's brace, bracket and parenthesis pieces (U+F8EB..U+F8FE in its private-use encoding): a general format's
#: drawn delimiters. A row carrying one is part of a figure, never prose.
FIGURE_GLYPHS = re.compile("[-]")
#: The placeholder a general format is written as until the vector arm draws it; the assembly refuses a page holding one.
FIGURE_MARK = "<!-- GENERAL FORMAT"
#: A heading's clause number: `9`, `9.2`, `14.8.24.1`, `A.1`, `E.1.3.2` (a lone letter is a word, not a number).
CLAUSE_NUMBER = re.compile(r"^(?:\d+(?:\.\d+)*|[A-Z](?:\.\d+)+)$")

#: A rule label printed as its own span: `1)`, `12)`, `a)`, `iv)`, `1.`, `a.`.
LABEL = re.compile(r"^(?:\d{1,3}|[a-z]|[ivx]{1,5})[.)]$")
CAPTION = re.compile(r"^(?:Table|Figure)\s+[A-Z]?\.?\d")
#: The footer row: a folio, the copyright line, or both on one row in either order (the folio sits outside, so it is left
#: on an even page and right on an odd one).
_FOLIO = r"(?:\d{1,4}|[ivxlcdm]{1,8})"
FOOTER = re.compile(rf"^(?:(?P<a>{_FOLIO})\s+)?©\s?ISO/IEC\s+\d{{4}}(?:\s*[-–]\s*All rights reserved)?(?:\s+(?P<b>{_FOLIO}))?$"
                    rf"|^(?P<c>{_FOLIO})$")
DOT_LEADER = re.compile(r"(?:\s*\.){4,}\s*")
#: A row of the contents is an entry, a leader, a page number: the leader is written as this, and such a row is a line of its own.
LEADER = " . . . "
#: The fraction of the page height above which a row is the running header, and below which it is the footer.
HEADER_FRACTION = 0.09
FOOTER_FRACTION = 0.94
HEADING_SIZE = 10.0
TITLE_SIZE = 13.5
#: The deepest rule level the standard prints; a row indented further is centred, not nested.
MAX_LEVEL = 4
NOTE_SIZE = 8.5
#: One indentation step of the standard's rule levels, in points (measured: labels at 56.7, 74.7, 92.7 on an even page).
INDENT_STEP = 18.0
#: A row continues the paragraph above when the gap between them is under this many of its font sizes (a line's leading is
#: 2 pt at 9 pt, a paragraph's 13 pt).
LEADING = 0.6
#: A row "runs to the right edge" when it ends within this many points of the column's right edge (a ragged line falls
#: short by at most a long word).
FULL_SLACK = 75.0
#: A gap between two spans of one row wider than this many font sizes is a column gap (an unruled table), not a space.
COLUMN_GAP = 3.0


@dataclasses.dataclass(frozen=True)
class Span:
    text: str
    font: str
    size: float
    x0: float
    y0: float
    x1: float
    y1: float

    @property
    def bold(self) -> bool:
        return "Bold" in self.font or "Black" in self.font

    @property
    def mono(self) -> bool:
        return "Courier" in self.font


@dataclasses.dataclass(frozen=True)
class Table:
    """A ruled table: its box, and per row the box of each cell (None where a merged cell spans it)."""
    bbox: tuple[float, float, float, float]
    rows: tuple[tuple[tuple[float, float, float, float] | None, ...], ...]


@dataclasses.dataclass(frozen=True)
class Layout:
    """One page as the text layer gives it: its size, its text spans, its ruled tables, its images and vector strokes."""
    width: float
    height: float
    spans: tuple[Span, ...]
    tables: tuple[Table, ...] = ()
    images: int = 0
    strokes: tuple[tuple[float, float, float, float], ...] = ()


@dataclasses.dataclass
class Row:
    spans: list[Span]

    @property
    def x0(self) -> float:
        return self.spans[0].x0

    @property
    def x1(self) -> float:
        return max(s.x1 for s in self.spans)

    @property
    def y0(self) -> float:
        return min(s.y0 for s in self.spans)

    @property
    def y1(self) -> float:
        return max(s.y1 for s in self.spans)

    @property
    def size(self) -> float:
        return max((s.size for s in self.spans if s.text.strip()), default=self.spans[0].size)

    @property
    def text(self) -> str:
        return join_spans(self.spans)

    @property
    def bold(self) -> bool:
        return all(s.bold for s in self.spans if s.text.strip())

    @property
    def mono(self) -> bool:
        return all(s.mono for s in self.spans if s.text.strip())

    @property
    def figure(self) -> bool:
        return any(FIGURE_GLYPHS.search(s.text) for s in self.spans)


@dataclasses.dataclass
class Converted:
    """A converted page: its Markdown, its general formats awaiting the vector arm (vertical extents), what the text layer
    left ambiguous, its printed folio, whether a General format clause is still open at its end, and its headings."""
    markdown: str
    figures: list[tuple[float, float]]
    ambiguous: list[str]
    folio: str | None
    format_open: bool
    headings: list[tuple[str, str]]


def letters(text: str) -> str:
    for lig, rep in LIGATURES.items():
        text = text.replace(lig, rep)
    return text


def join_spans(spans: list[Span]) -> str:
    """The spans of one row, left to right, a space between two that the page sets apart."""
    out = ""
    prev: Span | None = None
    for s in spans:
        if prev is not None and s.x0 - prev.x1 > 0.15 * s.size and not out.endswith(" ") and not s.text.startswith(" "):
            out += " "
        out += s.text
        prev = s
    return DOT_LEADER.sub(LEADER, re.sub(r"\s+", " ", letters(out))).strip()


def join_lines(parts: Iterable[str]) -> str:
    """Rows of one paragraph: a row ending in a hyphen after a letter or digit joins the next with no space (2002 5.6)."""
    out = ""
    for p in parts:
        if not out:
            out = p
        elif re.search(r"[A-Za-z0-9]-$", out):
            out += p
        else:
            out += " " + p
    return out


def escape_inline(text: str) -> str:
    """Prose Markdown would re-parse (TRANSCRIPTION-STATE.md): a backslash, emphasis and code marks, a tag, a pipe."""
    text = text.replace("\\", "\\\\")
    text = re.sub(r"([`*~|])", r"\\\1", text)
    text = re.sub(r"&(?=[A-Za-z#]\w*;)", "&amp;", text)
    return text.replace("<", "&lt;")


def escape_start(text: str) -> str:
    """A paragraph's first characters that Markdown reads as block syntax: a rule label (`1)`, `1.`), a heading, a quote,
    a list mark, a setext underline."""
    m = re.match(r"^(\d{1,9})([.)])(?=\s|$)", text)
    if m:
        return m.group(1) + "\\" + text[len(m.group(1)):]
    if re.match(r"^(?:#|>|\+|-|=)", text):
        return "\\" + text
    return text


def escape_pre(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def rows_of(spans: Iterable[Span]) -> list[Row]:
    """Spans grouped into printed rows (vertical centres within half a font size), each read left to right, top to bottom."""
    rows: list[Row] = []
    for s in sorted(spans, key=lambda s: ((s.y0 + s.y1) / 2, s.x0)):
        c = (s.y0 + s.y1) / 2
        for r in rows[-4:]:
            rc = (r.y0 + r.y1) / 2
            if abs(rc - c) < 0.5 * min(r.size, s.size) and not any(o.x0 < s.x1 - 1 and s.x0 < o.x1 - 1 for o in r.spans):
                r.spans.append(s)
                r.spans.sort(key=lambda o: o.x0)
                break
        else:
            rows.append(Row([s]))
    rows.sort(key=lambda r: (r.y0, r.x0))
    return rows


def two_columns(spans: list[Span], width: float) -> bool:
    """A page set in two columns: text on both sides of the middle and no span crossing it."""
    mid = width / 2
    right = [s for s in spans if s.x0 >= mid - 5]
    left = [s for s in spans if s.x1 <= mid + 5]
    crossing = [s for s in spans if s.x0 < mid - 5 and s.x1 > mid + 5]
    return len(right) >= 3 and len(left) >= 3 and not crossing


def inside(s: Span, box: tuple[float, float, float, float]) -> bool:
    cx, cy = (s.x0 + s.x1) / 2, (s.y0 + s.y1) / 2
    return box[0] - 1 <= cx <= box[2] + 1 and box[1] - 1 <= cy <= box[3] + 1


def heading_of(row: Row) -> tuple[str, str] | None:
    """(clause number, title) when the row is a clause heading."""
    if not row.bold or row.size < HEADING_SIZE or row.mono:
        return None
    words = row.text.split(" ", 1)
    if len(words) == 2 and CLAUSE_NUMBER.match(words[0]) and not CAPTION.match(row.text):
        return words[0], words[1]
    return None


def opens_format(layout: Layout, stamp: Callable[[str], bool]) -> bool | None:
    """Whether the page's LAST heading opens a General format clause (None: the page has no heading), so the page after it
    begins inside a general format."""
    last = None
    for r in rows_of(s for s in layout.spans if s.text.strip()):
        h = heading_of(r)
        if h and not stamp(r.text):
            last = bool(GENERAL_FORMAT_TITLE.fullmatch(h[1]))
    return last


def column_starts(r: Row) -> list[float]:
    """Where a row's columns begin: its first word, and each word after a gap wider than COLUMN_GAP font sizes. A rule
    label's gap is not a column's."""
    first = join_spans([r.spans[0]])
    labelled = len(r.spans) > 1 and LABEL.match(first) and r.spans[1].x0 - r.spans[0].x1 > 0.3 * r.size
    spans = r.spans[1:] if labelled else r.spans
    return [spans[0].x0] + [b.x0 for a, b in zip(spans, spans[1:]) if b.x0 - a.x1 > COLUMN_GAP * r.size]


def unruled_tables(rows: list[Row], skip: set[int]) -> list[Table]:
    """Runs of two or more consecutive rows that each have column gaps: an unruled table. A column begins where some row
    starts a column after a wide gap and no word of ANY row of the run straddles that x: so a row whose cells the print
    barely separates (2002 E.1.3.2: "Random or dynamic" set 3.5 pt before "Automatic or manual") is divided where the
    other rows divide it, and a header centred over its column (2002 8.8.4.2.4.3) moves no division."""
    out: list[Table] = []
    run: list[Row] = []
    for i, r in enumerate([*rows, None]):
        if r is not None and i not in skip and not r.mono and not heading_of(r) and len(column_starts(r)) > 1:
            run.append(r)
            continue
        if len(run) >= 2:
            table = grid(run)
            if table is not None:
                out.append(table)
        run = []
    return out


def grid(run: list[Row]) -> Table | None:
    size = max(r.size for r in run)
    words = [s for r in run for s in r.spans]
    edges: list[float] = []
    for x in sorted(x for r in run for x in column_starts(r)[1:]):
        if edges and x - edges[-1] < size:
            continue
        if not any(s.x0 < x - 1 and s.x1 > x + 1 for s in words):
            edges.append(x)
    if not edges:
        return None
    left, right = min(s.x0 for s in words) - 1, max(s.x1 for s in words) + 1
    bounds = [left, *(x - 0.5 for x in edges), right]
    cells = tuple(tuple((bounds[k], r.y0, bounds[k + 1], r.y1) for k in range(len(bounds) - 1)) for r in run)
    return Table((left, run[0].y0, right, run[-1].y1), cells)


@dataclasses.dataclass
class Block:
    kind: str                     # para | lines | note | heading | title | pre | table | figure | label
    rows: list[Row]
    level: int = 0
    label: str = ""
    text: str = ""


def convert(layout: Layout, pno: int, stamp: Callable[[str], bool], format_open: bool = False) -> Converted:
    """One page to Markdown (the module's rules). `stamp` says whether a row is the per-copy stamp; `format_open` whether the
    page begins inside a General format clause that an earlier page opened."""
    ambiguous: list[str] = []
    folio: str | None = None
    head_y, foot_y = HEADER_FRACTION * layout.height, FOOTER_FRACTION * layout.height
    body: list[Span] = []
    for r in rows_of(s for s in layout.spans if s.text.strip()):
        t = r.text
        if stamp(t):
            continue
        if r.y1 <= head_y:
            continue
        m = FOOTER.match(t) if r.y0 >= foot_y else None
        if m:
            folio = m.group("a") or m.group("b") or m.group("c") or folio
            continue
        body.extend(r.spans)
    if layout.images:
        ambiguous.append(f"{layout.images} image(s)")
    if not body:
        if not layout.images:
            ambiguous.append("no text")
        return Converted("", [], ambiguous, folio, format_open, [])

    columns = [body]
    if two_columns([s for s in body if not any(inside(s, t.bbox) for t in layout.tables)], layout.width):
        mid = layout.width / 2
        columns = [[s for s in body if s.x0 < mid - 5], [s for s in body if s.x0 >= mid - 5]]

    blocks: list[Block] = []
    figures: list[tuple[float, float]] = []
    headings: list[tuple[str, str]] = []
    in_format = format_open
    covered: list[tuple[float, float]] = []   # vertical extents explained by a table or a general format
    for col in columns:
        rows = rows_of(col)
        tables = [t for t in layout.tables if len(t.rows) * max((len(r) for r in t.rows), default=0) > 1]
        right = max(r.x1 for r in rows)
        margin = min(r.x0 for r in rows)
        items: list[tuple[float, Any]] = []
        in_fmt = in_format
        fmt_rows: set[int] = set()
        for i, r in enumerate(rows):
            h = heading_of(r)
            if h:
                in_fmt = bool(GENERAL_FORMAT_TITLE.fullmatch(h[1]))
                continue
            if in_fmt or r.figure:
                fmt_rows.add(i)
                if r.figure and not in_fmt:
                    ambiguous.append(f"a general-format glyph outside a General format clause (y {r.y0:.0f})")
        in_format = in_fmt
        fmt_extent = [(rows[i].y0, rows[i].y1) for i in fmt_rows]
        kept_tables = [t for t in tables
                       if not any(t.bbox[1] - 1 <= (a + b) / 2 <= t.bbox[3] + 1 for a, b in fmt_extent)
                       and any(inside(s, t.bbox) for s in col)]
        ruled = {i for i, r in enumerate(rows) if any(inside(s, t.bbox) for t in kept_tables for s in r.spans)}
        kept_tables += unruled_tables(rows, fmt_rows | ruled)
        for t in kept_tables:
            items.append((t.bbox[1], t))
            covered.append((t.bbox[1], t.bbox[3]))
        for i, r in enumerate(rows):
            if any(inside(s, t.bbox) for t in kept_tables for s in r.spans) and i not in fmt_rows:
                continue
            items.append((r.y0, (i, r)))
        items.sort(key=lambda it: it[0])
        prev: Row | None = None
        for _, it in items:
            if isinstance(it, Table):
                blocks.append(Block("table", [], text=table_markdown(it, col)))
                prev = None
                continue
            i, r = it
            h = heading_of(r)
            if h:
                headings.append(h)
                blocks.append(Block("heading", [r], level=len(h[0].split(".")), label=h[0], text=h[1]))
                prev = None
                continue
            if r.bold and r.size >= TITLE_SIZE:
                if blocks and blocks[-1].kind == "title" and prev is not None and r.y0 - prev.y1 < 2 * r.size:
                    blocks[-1].rows.append(r)
                else:
                    blocks.append(Block("title", [r]))
                prev = r
                continue
            if blocks and blocks[-1].kind == "title" and r.size >= TITLE_SIZE and prev is not None and r.y0 - prev.y1 < 2 * r.size:
                blocks[-1].rows.append(r)   # "(normative)" under "Annex A", in a regular weight
                prev = r
                continue
            is_label = r.bold and r.text.endswith(":") and r.size < HEADING_SIZE
            if i in fmt_rows and not is_label and not (r.size <= NOTE_SIZE and r.text.startswith("NOTE")):
                if blocks and blocks[-1].kind == "figure" and prev is not None and prev is blocks[-1].rows[-1]:
                    blocks[-1].rows.append(r)
                else:
                    blocks.append(Block("figure", [r]))
                prev = r
                continue
            if r.mono:
                if blocks and blocks[-1].kind == "pre" and prev is not None and prev.mono:
                    blocks[-1].rows.append(r)
                else:
                    blocks.append(Block("pre", [r]))
                prev = r
                continue
            first = join_spans([r.spans[0]])
            labelled = len(r.spans) > 1 and LABEL.match(first) and r.spans[1].x0 - r.spans[0].x1 > 0.3 * r.size
            if len(column_starts(r)) > 1:
                ambiguous.append(f"a single row with column gaps (y {r.y0:.0f})")
            level = max(0, round((r.x0 - margin) / INDENT_STEP))
            if level > MAX_LEVEL or CAPTION.match(r.text):
                level = 0   # a centred row (a caption, a title) is not an indented rule
            if labelled:
                blocks.append(Block("para", [Row(r.spans[1:])], level=level, label=first))
            elif r.size <= NOTE_SIZE and r.text.startswith("NOTE"):
                blocks.append(Block("note", [r], level=level))
            elif (prev is not None and blocks and blocks[-1].kind in ("para", "note", "lines")
                  and r.y0 - prev.y1 < LEADING * r.size and not prev.mono):
                full = prev.x1 >= right - FULL_SLACK and LEADER not in prev.text
                if full and blocks[-1].kind != "lines":
                    blocks[-1].rows.append(r)
                else:
                    if blocks[-1].kind == "para" and not blocks[-1].label and len(blocks[-1].rows) == 1:
                        blocks[-1].kind = "lines"
                    if blocks[-1].kind == "lines":
                        blocks[-1].rows.append(r)
                    else:
                        blocks.append(Block("lines", [r], level=level))
            else:
                blocks.append(Block("para", [r], level=level))
            prev = r
    for b in blocks:
        if b.kind == "figure":
            y0, y1 = min(r.y0 for r in b.rows), max(r.y1 for r in b.rows)
            figures.append((y0, y1))
            covered.append((y0, y1))
    strays = [s for s in layout.strokes if not any(a - 6 <= s[1] and s[3] <= b + 6 for a, b in covered)]
    if strays:
        ambiguous.append(f"{len(strays)} vector stroke(s) outside every table and general format")
    return Converted(render(blocks, pno), figures, list(dict.fromkeys(ambiguous)), folio, in_format, headings)


def table_markdown(t: Table, spans: list[Span]) -> str:
    """A ruled table as a Markdown table, the first row its header. A cell's rows are joined as a paragraph's are when the
    row above runs to the cell's right edge, and kept apart (`<br>`) when it stops short: the cell lists items."""
    width = max(len(r) for r in t.rows)

    def cell(box: tuple[float, float, float, float] | None) -> str:
        if box is None:
            return ""
        parts: list[str] = []
        prev: Row | None = None
        for r in rows_of(s for s in spans if inside(s, box)):
            text = escape_inline(r.text)
            if prev is not None and prev.x1 >= box[2] - min(FULL_SLACK, 0.3 * (box[2] - box[0])):
                parts[-1] = join_lines([parts[-1], text])
            else:
                parts.append(text)
            prev = r
        return "<br>".join(parts)

    lines = []
    for n, r in enumerate(t.rows):
        cells = [cell(c) for c in r] + [""] * (width - len(r))
        lines.append("| " + " | ".join(cells) + " |")
        if n == 0:
            lines.append("|" + "---|" * width)
    return "\n".join(lines)


def render(blocks: list[Block], pno: int) -> str:
    out: list[str] = []
    figure_no = 0
    for b in blocks:
        pad = "   " if b.level > 0 else ""
        if b.kind == "heading":
            out.append("#" * min(6, b.level + 2) + f" {b.label} {escape_inline(b.text)}")
        elif b.kind == "title":
            out.append("### " + escape_inline(" ".join(r.text for r in b.rows)))
        elif b.kind == "table":
            out.append(b.text)
        elif b.kind == "figure":
            figure_no += 1
            y0, y1 = min(r.y0 for r in b.rows), max(r.y1 for r in b.rows)
            out.append(f"{FIGURE_MARK} p{pno:04} #{figure_no} y {y0:.1f}-{y1:.1f}: drawn from the page by the vector arm, "
                       "never typed from its text -->")
        elif b.kind == "pre":
            out.append(pre_block(b.rows))
        elif b.kind == "lines":
            base = b.rows[0].x0
            lines = []
            for r in b.rows:
                step = max(0, round((r.x0 - base) / INDENT_STEP))
                lines.append("&emsp;" * step + escape_start(escape_inline(r.text)))
            out.append(pad + ("\\\n" + pad).join(lines))
        else:
            text = escape_inline(join_lines(r.text for r in b.rows))
            if b.label:
                label = re.sub(r"^(\d+)([.)])$", r"\1\\\2", b.label)
                out.append(f"{pad}{label} {text}")
            else:
                out.append(pad + escape_start(text))
    return "\n\n".join(out) + ("\n" if out else "")


def pre_block(rows: list[Row]) -> str:
    """Courier rows at their measured columns (the font's fixed pitch is 0.6 of its size)."""
    x = min(r.x0 for r in rows)
    lines: list[str] = []
    prev: Row | None = None
    for r in rows:
        if prev is not None and r.y0 - prev.y1 > r.size:
            lines.append("")
        line = ""
        for s in r.spans:
            col = max(len(line) + 1 if line else 0, round((s.x0 - x) / (0.6 * s.size)))  # a word apart is a space apart
            line = line.ljust(col) + letters(s.text).rstrip()
        lines.append(escape_pre(line.rstrip()))
        prev = r
    return '<pre style="line-height:1">\n' + "\n".join(lines) + "\n</pre>"


# -- PyMuPDF (imported inside the functions: CI's self-test jobs install no PDF library) -----------------------------------

def read_layout(doc: Any, pno: int) -> Layout:
    """Page `pno` (1-based) of an open PyMuPDF document as a Layout: its WORDS as spans, ruled tables (the line strategy),
    images and the bounding boxes of its vector strokes. A word is a run of glyphs of one font with no gap between them,
    measured per glyph: a span of the text layer is not a printed run (the 2002 glossary sets each defined term in bold
    OVER the leading blanks of the regular span that follows it, and an unruled table's columns share one span)."""
    page = doc[pno - 1]
    spans: list[Span] = []
    images = 0
    for b in page.get_text("rawdict")["blocks"]:
        if b["type"] != 0:
            images += 1
            continue
        for ln in b["lines"]:
            for s in ln["spans"]:
                size = round(s["size"], 2)
                word: list[Any] = []
                for ch in [*s["chars"], None]:
                    if ch is not None and ch["c"].strip() and (not word or ch["bbox"][0] - word[-1]["bbox"][2] < 0.12 * size):
                        word.append(ch)
                        continue
                    if word:
                        spans.append(Span("".join(c["c"] for c in word), s["font"], size, word[0]["bbox"][0],
                                          min(c["bbox"][1] for c in word), word[-1]["bbox"][2], max(c["bbox"][3] for c in word)))
                    word = [ch] if ch is not None and ch["c"].strip() else []
    with contextlib.redirect_stdout(io.StringIO()):   # the table finder prints an advertisement for another package
        found = page.find_tables(strategy="lines")
    tables = tuple(Table(tuple(t.bbox), tuple(tuple(tuple(c) if c else None for c in r.cells) for r in t.rows))
                   for t in found.tables)
    strokes: list[tuple[float, float, float, float]] = []
    for d in page.get_drawings():   # one path may hold a whole table's rules: each item is a stroke of its own
        for item in d["items"]:
            pts = [p for p in item[1:] if hasattr(p, "x")]
            if pts:
                strokes.append((min(p.x for p in pts), min(p.y for p in pts), max(p.x for p in pts), max(p.y for p in pts)))
            elif hasattr(item[1], "x0"):
                strokes.append(tuple(item[1]))
    return Layout(page.rect.width, page.rect.height, tuple(spans), tables, images, tuple(strokes))
