"""conformance_source.py — the ONE reader of the conformance statement (kb/Work PB5395, PB5429, PB5430;
DESIGN-orchestrator-loop.md §16.8).

Every script that needs a register, a section, a row or a determination of the conformance statement reads it HERE, and no
other module opens the document's storage or keeps a parser of its tables: `load(root)` renders the sources under
`docs/conformance/` (one directory per section, one file per entry: `read_sources`) once into a `Conformance`, and
`parse(text)` builds the same from a text (a self-test's fabricated body). `docs/CONFORMANCE.md` is that render, a
gitignored VIEW (`conformance_doc.py`, `rendered_views.py`) that nothing reads back.

THE READER KNOWS THE DOCUMENT'S SECTIONS BY NUMBER (`## 7. …`, `### 3.2 …`), never by their title: a title is prose and is
reworded, and a number is the key every citation of the document already uses. A section runs from its heading to the next
heading of the same or a shallower level, so section 3 holds its subsections 3.1 to 3.11 and section 3.2 does not.

THE DOCUMENT HAS ONE REGULAR SHAPE (step B1, kb/Work PB5429), the one the cut into one file per entry renders back: each
section is of a kind (`KINDS`), its own text is a preamble, its keyed entries in natural key order, a table's row bodies
under `#### <key>`, and an epilogue after `EPILOGUE_MARKER`; `render_section` is the one layout. `check` (`--check`, run
by the DOC AUDITS in `docchecks/register.py`) refuses every departure from it, so the document stays in the shape the cut
needs whoever edits it.

NOTHING IN HERE MAY BE IMPORTED BY A MODULE THE COMPILER'S TESTS RUN. `inventory_schema.py` and `generate.py` are in a compiler
test's import closure (kb/Work PB4992) and read the registers' EXTRACT (`tests/version-matrix/conformance-register.json`,
`inventory_schema.register_extract`), which `docchecks/register.py` writes from this reader; a Markdown file is inert
(kb/Work PB4453), so a reader of the document must not reach a compiler test.
"""
from __future__ import annotations

import json
import pathlib
import re
import sys
from typing import NamedTuple

sys.path.append(str(pathlib.Path(__file__).resolve().parent))  # scripts/spec, for inventory_schema
from inventory_schema import DerivationRow  # noqa: E402  (the §8 row's shape, which the registers' extract shares)

REPO = pathlib.Path(__file__).resolve().parents[2]
#: THE document's SOURCES, relative to a repository root (step B2, kb/Work PB5430): one directory per section, named by its
#: number, each holding `index.md` (frontmatter `heading:`, and `columns:` for a table; its body the section's preamble), an
#: optional `epilogue.md`, and one file per entry named by the entry's key; `head.md` is the text before section 1.
SOURCE_REL = "docs/conformance"
#: The document's RENDERED VIEW (gitignored; `rendered_views.py`'s `conformance` row renders it with `conformance_doc.py`):
#: what a citation, a diagnostic and a user name. Nothing reads the view; every reader reads the sources through `load`.
PATH_REL = "docs/CONFORMANCE.md"
HEAD_FILE = "head.md"
INDEX_FILE = "index.md"
EPILOGUE_FILE = "epilogue.md"
#: The directory's page on GitHub (hand-written: what the directory is and how to render the whole); no section's source.
README_FILE = "README.md"

#: The section numbers the readers address (the document's own, `## <n>. …`).
A3_SECTION = "2"
EXTENSIONS_SECTION = "3.2"
FACILITIES_SECTION = "4"
A4_SECTION = "5"
A1_SECTION = "7"

#: A section heading: `## 7. Annex A.1 …` or `### 3.2 Nonstandard extensions …` — its level, its number, nothing after the
#: number but the full stop the top-level headings carry.
HEADING = re.compile(r"^(#{2,3}) ([0-9]+(?:\.[0-9]+)*)\.?(?=\s)", re.MULTILINE)
#: A table separator row's cells: `---`, `:---:`.
_SEPARATOR = set("-: ")
#: A numbered, bold-titled facility of section 4: `1. **Message control system …**`.
_FACILITY = re.compile(r"^[0-9]+\.\s+\*\*(.+?)\*\*", re.MULTILINE)

# ── the document's regular shape (step B1, kb/Work PB5429) ──────────────────────────────────────────────────────────
#
# Every section is one of four kinds, and its text after the heading is: a PREAMBLE (prose), then the section's ENTRIES,
# then — for a table section — the BODIES of the rows that have extended prose, each under `#### <key>`, then an optional
# EPILOGUE after the marker line. A prose section has no entries: its whole text is its preamble. A key is unique in the
# whole document (every key is an anchor), and the entries of every kind but BLOCKS stand in natural key order.

#: A section of prose alone (sections 1, 3.3 to 3.11 and 6): its text is its preamble.
PROSE = "prose"
#: A section of determinations: top-level bullets, each opening `- **<D-KEY>` (sections 3 and 3.1).
BULLETS = "bullets"
#: A section whose entries are the rows of one table, keyed by the first cell (sections 2, 3.2, 5, 7 and 8).
TABLE = "table"
#: A section whose entries are `#### <key>` blocks in the order written (§4, the facilities and their determinations).
BLOCKS = "blocks"

#: The kind of every section that has entries; every other numbered section is PROSE. A new section with entries is a row
#: here, read by the shape check and the cut alike.
KINDS = {"2": TABLE, "3": BULLETS, "3.1": BULLETS, "3.2": TABLE, "4": BLOCKS, "5": TABLE, "7": TABLE, "8": TABLE}

#: The line that opens a section's epilogue: the text after it speaks for the whole section, not for one entry.
EPILOGUE_MARKER = "<!-- conformance: epilogue -->"
#: A determination's key: `D-` and upper-case words joined by hyphens or full stops (`D-SEQ`, `D-F2-85`, `D-IO-STATUS-04`).
DETERMINATION_KEY = r"D-[A-Z0-9]+(?:[-.][A-Z0-9]+)*"
#: A determination bullet's opening line, the key followed by a space or the bold's close.
_BULLET = re.compile(r"^- \*\*(" + DETERMINATION_KEY + r")(?=[ *])")
#: A keyed block's or a row body's heading: `#### <key>`, the key one word.
_BLOCK = re.compile(r"^#### (\S+)$")
#: Any Markdown heading line: the reader lists every one of them.
_ANY_HEADING = re.compile(r"^(#{1,6}) (.*)$")
#: A numbered section heading at its level: `## 7. …` (one number, a full stop), `### 3.2 …` or `### 3.7.1 …`.
_NUMBERED = {2: re.compile(r"^## ([0-9]+)\. "), 3: re.compile(r"^### ([0-9]+(?:\.[0-9]+){1,2}) ")}


def natural_key(key: str) -> tuple:
    """The order the document's entries stand in: digit runs compared as numbers, so `DOC-A.1-9` precedes `DOC-A.1-10` and
    `DOC-A.1-92` precedes `DOC-A.1-92b`; a digit run sorts before text at the same place (`DRV-1985-…` before `DRV-GR-…`)."""
    return tuple((0, int(t), "") if t.isdigit() else (1, 0, t) for t in re.findall(r"[0-9]+|[^0-9]+", key))


class Entry(NamedTuple):
    """One keyed entry of a section: a determination bullet, a table row, a `#### <key>` block or a row's body."""

    key: str
    text: str   #: its lines as written, trailing blank lines dropped (a row: the row line; a block or body: after the heading)
    line: int   #: the 1-based line of its first line (a block or body: of its heading)


class Shape(NamedTuple):
    """One section's text classified: what precedes the entries, the entries, the row bodies, the epilogue, and the
    findings of every line that fits none of them."""

    kind: str
    preamble: str
    table_head: str   #: a table section's header row and separator row, as written; "" for every other kind
    entries: list[Entry]
    bodies: list[Entry]
    epilogue: str | None
    findings: list[str]


class Section7Row(NamedTuple):
    """One data row of the §7 Annex A.1 register, by cell."""

    key: str            #: the row's UNIQUE key — the inventory rule-id it discharges, `DOC-A.1-<n>`, plus a letter on an item's further determinations (`DOC-A.1-92b`, kb/Work PB3840)
    element: str        #: the A.1 element the row claims to be about
    determination: str  #: WHAT WE DO — the determination itself
    pinned: str         #: the spec-derived test-ref(s) that pin it, or '—'


class Section(NamedTuple):
    """One section of the document: its heading line, and the text after it up to the section's end."""

    number: str
    heading: str   #: the heading line, `## 7. Annex A.1 — …`
    body: str      #: the text after the heading line
    line: int      #: the 1-based line of the heading in the document


def number_of(heading: str) -> str:
    """The section number a heading text names (`## 8. Rules with …` → `8`, `### 3.2 …` → `3.2`)."""
    m = HEADING.match(heading.strip())
    if m is None:
        raise ValueError(f"{heading!r} does not open with a section number")
    return m.group(2)


def cells(line: str) -> list[str]:
    """A markdown table row's content cells, split on UNESCAPED pipes.

    ⚠ Splitting on a bare '|' is wrong here and was wrong once: the item-82 determination writes an absolute
    value as `\\|v\\|`, so a naive split reports that row with two extra cells and mis-places anything appended
    to it.
    """
    parts = [c.replace("\\|", "|").strip() for c in re.split(r"(?<!\\)\|", line.rstrip("\n"))]
    return parts[1:-1]


class Conformance:
    """The conformance statement's text and the readers over it. Immutable; every answer is computed from `text`."""

    def __init__(self, text: str) -> None:
        self.text = text
        #: The document's lines, split on `\\n` alone as the C# twin (`tests/_shared/ConformanceRegister.cs`) splits them,
        #: so a line number here is a line number in the file.
        self.lines = text.split("\n")
        self._headings = [(m.start(), len(m.group(1)), m.group(2)) for m in HEADING.finditer(text)]

    # ── sections ────────────────────────────────────────────────────────────────────────────────────────────────────

    def find_section(self, number: str) -> Section | None:
        """The section numbered `number`, or None when the document carries none (a fabricated body carries one or two)."""
        for i, (start, level, num) in enumerate(self._headings):
            if num != number:
                continue
            end = next((s for s, lvl, _ in self._headings[i + 1:] if lvl <= level), len(self.text))
            eol = self.text.find("\n", start)
            body_start = len(self.text) if eol < 0 else eol + 1
            return Section(number, self.text[start:eol if eol >= 0 else len(self.text)],
                           self.text[body_start:max(end, body_start)], self.text.count("\n", 0, start) + 1)
        return None

    def section(self, number: str) -> Section:
        """The section numbered `number`; a section the document lost is a SystemExit naming it, never an empty answer."""
        found = self.find_section(number)
        if found is None:
            raise SystemExit(f"⛔ {PATH_REL} has no §{number}: the reader of the document cannot invent it")
        return found

    def body_of(self, number: str) -> str:
        """The text of section `number` after its heading line, "" when the document carries no such section."""
        found = self.find_section(number)
        return "" if found is None else found.body

    def numbers(self) -> list[str]:
        """Every numbered section of the document, in the order written."""
        return [num for _, _, num in self._headings]

    def own_section(self, number: str) -> Section:
        """Section `number` WITHOUT its subsections: its heading and the text up to the next numbered heading of any level
        (section 3's own text ends at section 3.1, where `section("3")` runs to section 4)."""
        for i, (start, _, num) in enumerate(self._headings):
            if num != number:
                continue
            end = self._headings[i + 1][0] if i + 1 < len(self._headings) else len(self.text)
            eol = self.text.find("\n", start)
            body_start = len(self.text) if eol < 0 else eol + 1
            return Section(number, self.text[start:eol if eol >= 0 else len(self.text)],
                           self.text[body_start:max(end, body_start)], self.text.count("\n", 0, start) + 1)
        raise SystemExit(f"⛔ {PATH_REL} has no §{number}: the reader of the document cannot invent it")

    def shape(self, number: str) -> Shape:
        """Section `number`'s own text classified by its kind (`KINDS`): preamble, entries, row bodies, epilogue, and a
        finding for every line that fits none of them (kb/Work PB5429)."""
        section = self.own_section(number)
        return _classify(number, KINDS.get(number, PROSE), section.body.split("\n"), section.line + 1)

    def determinations(self) -> dict[str, Entry]:
        """`{key -> entry}` for the determinations of sections 3 and 3.1, every one keyed `D-<KEY>` since step B1."""
        return {e.key: e for number in ("3", "3.1") for e in self.shape(number).entries}

    # ── a table section's rows ──────────────────────────────────────────────────────────────────────────────────────

    def rows_at(self, number: str) -> list[tuple[int, list[str]]]:
        """Every data row of section `number`'s table as `(1-based line in the document, cells)`, in document order.

        The line is for a reader that meets the document LINE BY LINE and must know which lines are register rows — the
        Phase-B dossier tags a citing line with its row key (kb/Work PB3209). The table's FIRST row is its header, and a
        separator row or a row of one cell is no data row. A body that does not carry the section has no rows of it: the
        same answer the C# reader gives, because both are driven against FABRICATED bodies carrying one section at a time;
        a section lost from the live document is caught by the POPULATION assertions its gates make, never by a parser
        raising here.

        ⛔ A SECTION ENDS AT ITS NEXT HEADING, and that is not tidiness. Until §8 existed §7 was the LAST section, so
        reading to end-of-file was indistinguishable from reading the section — and the moment a second register was
        appended, every §8 row would have been served as an A.1 determination keyed by something that is not an A.1 item.
        """
        section = self.find_section(number)
        if section is None:
            return []
        rows: list[tuple[int, list[str]]] = []
        header_seen = False
        for offset, line in enumerate(section.body.split("\n")):
            if not line.startswith("| "):
                continue
            row = cells(line)
            if not header_seen:
                header_seen = True
                continue
            if len(row) < 2 or set(row[0]) <= _SEPARATOR:
                continue
            rows.append((section.line + 1 + offset, row))
        return rows

    def rows(self, number: str) -> list[list[str]]:
        """Every data row of section `number`'s table, cells in column order, in document order."""
        return [row for _, row in self.rows_at(number)]

    def row_line(self, key: str) -> str | None:
        """The line of a table row anywhere in the document whose first cell is `key` (`| DOC-A.1-62 |`), or None."""
        prefix = f"| {key} |"
        return next((ln for ln in self.lines if ln.startswith(prefix)), None)

    # ── §7 and §8 ───────────────────────────────────────────────────────────────────────────────────────────────────

    def a1_rows_at(self) -> list[tuple[int, Section7Row]]:
        """Every data row of the §7 Annex A.1 register with its 1-based line, in document order."""
        return [(line, Section7Row(c[0], c[1], _cell(c, 2), _cell(c, 3))) for line, c in self.rows_at(A1_SECTION)]

    def a1_rows(self) -> list[Section7Row]:
        """Every data row of the §7 Annex A.1 register, in document order."""
        return [row for _, row in self.a1_rows_at()]

    def a1_determinations(self) -> dict[str, str]:
        """`{row key -> determination cell}` for the §7 register.

        A row key is UNIQUE (kb/Work PB3840): an item with SEVERAL determinations (A.1-92 has two) keys the first by the
        item's inventory rule-id — the one its obligation and its inventory row anchor on — and each further one by that id
        plus a letter (`DOC-A.1-92b`). A repeated key raises: a lookup that silently kept one of two rows is how ten code
        sites came to cite a determination they could not name. `audit_annex_a1.py` reports the same defect as a finding.
        """
        rows = self.a1_rows()
        keys = [r.key for r in rows]
        if len(set(keys)) != len(keys):
            repeated = sorted({k for k in keys if keys.count(k) > 1})
            raise ValueError(f"{PATH_REL} §7 repeats the row key(s) {repeated}: a row key names ONE "
                             f"determination (file a further determination of an item under its id plus a letter)")
        return {r.key: r.determination for r in rows}

    def derivation_rows(self, number: str) -> list[DerivationRow]:
        """Every data row of the §8 derivation register (section `number`), in document order."""
        return [DerivationRow(c[0], c[1], _cell(c, 2), _cell(c, 3), _cell(c, 4), _cell(c, 5)) for c in self.rows(number)]

    # ── the dispositions and the facilities ─────────────────────────────────────────────────────────────────────────

    def a3_dispositions(self) -> list[dict[str, str]]:
        """§2's table: the A.3 item cell (which may name a range, `28–30`), the element, the § citation, the disposition and
        the note (a note that holds an unescaped pipe is the rest of the row). A row of fewer than five cells is none."""
        return [{"items": c[0], "element": c[1], "section": c[2], "disposition": c[3], "note": "|".join(c[4:])}
                for c in self.rows(A3_SECTION) if len(c) >= 5]

    def a4_dispositions(self) -> list[dict[str, str]]:
        """§5's table: the A.4 module number, its name, its disposition and its note, one dict a row."""
        return [{"clause": c[0], "module": c[1], "disposition": c[2], "note": "|".join(c[3:])}
                for c in self.rows(A4_SECTION) if len(c) >= 3]

    def facility_titles(self) -> list[str]:
        """The titles of §4's numbered facilities (`1. **Message control system …**`), in the order written."""
        return _FACILITY.findall(self.section(FACILITIES_SECTION).body)

    # ── a determination's own text ──────────────────────────────────────────────────────────────────────────────────

    def bullet(self, prefix: str) -> str | None:
        """The top-level bullet whose text begins `- **<prefix>`, up to the next top-level bullet; None when there is none."""
        opening = "- **" + prefix
        at = self.text.find(opening)
        if at < 0:
            return None
        end = self.text.find("\n- **", at + len(opening))
        return self.text[at:len(self.text) if end < 0 else end]

    def paragraph(self, key: str) -> str | None:
        """The opening paragraph of the determination `**<key> …**` — its text to the first blank line; None when absent."""
        found = re.search(r"\*\*" + re.escape(key) + r" .*?\n\n", self.text, re.DOTALL)
        return None if found is None else found.group(0)


def _cell(row: list[str], i: int) -> str:
    return row[i] if len(row) > i else ""


def row_line(row: list[str]) -> str:
    """A table row's one spelling from its cells: `| a | b |`, a pipe inside a cell escaped (`cells` inverted)."""
    return "| " + " | ".join(c.replace("|", "\\|") for c in row) + " |"


def _fenced(lines: list[str]) -> list[bool]:
    """For each line, whether it lies inside a fenced code block (its fence lines included): a `#` or `|` there is text."""
    out, inside = [], False
    for line in lines:
        fence = line.lstrip().startswith("```")
        out.append(inside or fence)
        if fence:
            inside = not inside
    return out


def _trim(lines: list[str]) -> str:
    return "\n".join(lines).strip("\n")


def _classify(number: str, kind: str, lines: list[str], first: int) -> Shape:
    """`lines` (the section's own text, line `first` its first) as a Shape of kind `kind`. The parse never stops at a
    misfit: it records the finding and goes on, so `--check` names every line out of shape at once."""
    where = f"{PATH_REL} §{number}"
    findings: list[str] = []
    fenced = _fenced(lines)
    epilogue: str | None = None
    markers = [i for i, line in enumerate(lines) if line == EPILOGUE_MARKER]
    if markers:
        epilogue = _trim(lines[markers[0] + 1:])
        lines = lines[:markers[0]]
        if kind == PROSE:
            findings.append(f"{where} line {first + markers[0]}: a prose section has no entries, so no epilogue marker")
        findings += [f"{where} line {first + i}: a second epilogue marker" for i in markers[1:]]
    blocks = [i for i, line in enumerate(lines) if not fenced[i] and line.startswith("#### ")]
    for i in blocks:
        if _BLOCK.match(lines[i]) is None:
            findings.append(f"{where} line {first + i}: `{lines[i]}` is no `#### <key>` heading (a key is one word)")
    if kind == PROSE:
        findings += [f"{where} line {first + i}: a prose section holds no `#### <key>` entry" for i in blocks]
        return Shape(kind, _trim(lines), "", [], [], epilogue, findings)
    entries: list[Entry] = []
    bodies: list[Entry] = []
    head: list[str] = []
    if kind == BULLETS:
        start = next((i for i, line in enumerate(lines) if line.startswith("- ")), len(lines))
        findings += [f"{where} line {first + i}: a determination section holds no `#### <key>` heading" for i in blocks]
        current: list[str] = []
        at = 0

        def close() -> None:
            if current:
                found = _BULLET.match(current[0])
                if found is None:
                    findings.append(f"{where} line {first + at}: an entry with no key (`- **D-<KEY> …**`): {current[0][:80]}")
                else:
                    entries.append(Entry(found.group(1), _trim(current), first + at))

        for i in range(start, len(lines)):
            line = lines[i]
            if line.startswith("- "):
                close()
                current, at = [line], i
            elif line == "" or line.startswith(" ") or fenced[i]:
                current.append(line)
            elif i in blocks:
                continue   # reported above
            elif i == 0 or lines[i - 1] == "" or lines[i - 1].startswith(("- ", " ")):
                findings.append(f"{where} line {first + i}: text after the section's entries, outside an entry and the "
                                f"epilogue: {line[:80]}")
        close()
        preamble = _trim(lines[:start])
    else:
        opening = "|" if kind == TABLE else "#### "
        start = next((i for i, line in enumerate(lines) if not fenced[i] and line.startswith(opening)), len(lines))
        preamble = _trim(lines[:start])
        i = start
        if kind == TABLE:
            header_seen = False
            while i < len(lines) and lines[i].startswith("|"):
                row = cells(lines[i])
                if header_seen and not (len(row) >= 1 and set(row[0]) <= _SEPARATOR):
                    entries.append(Entry(row[0].strip("`") if row else "", lines[i], first + i))
                elif not entries:
                    head.append(lines[i])
                header_seen = True
                i += 1
        heads = [j for j in blocks if j >= i]
        for j in range(i, heads[0] if heads else len(lines)):
            if lines[j].strip() and not lines[j - 1].strip():   # one finding a paragraph, at its first line
                findings.append(f"{where} line {first + j}: text after the section's entries, outside a row body and the "
                                f"epilogue: {lines[j][:80]}")
        for n, j in enumerate(heads):
            end = heads[n + 1] if n + 1 < len(heads) else len(lines)
            found = _BLOCK.match(lines[j])
            if found is not None:
                (bodies if kind == TABLE else entries).append(Entry(found.group(1), _trim(lines[j + 1:end]), first + j))
        findings += [f"{where} line {first + j}: a `#### ` heading before the section's entries" for j in blocks if j < i]
    if not entries:
        findings.append(f"{where}: a {kind} section with no entry (`KINDS` gives it entries)")
    if kind != BLOCKS:
        keys = [e.key for e in entries]
        if keys != sorted(keys, key=natural_key):
            out = next(e for e, k in zip(entries, sorted(keys, key=natural_key)) if e.key != k)
            findings.append(f"{where} line {out.line}: entries out of natural key order from `{out.key}` "
                            f"(the order the renderer writes, `natural_key`)")
    if kind == TABLE:
        rows = {e.key for e in entries}
        findings += [f"{where} line {b.line}: `#### {b.key}` is the body of no row of the table" for b in bodies if b.key not in rows]
        body_keys = [b.key for b in bodies]
        if body_keys != sorted(body_keys, key=natural_key):
            findings.append(f"{where}: the row bodies are out of natural key order")
        findings += [f"{where}: two bodies of the row `{k}`" for k in sorted({k for k in body_keys if body_keys.count(k) > 1})]
        width = len(cells(head[0])) if head else 0
        findings += [f"{where} line {e.line}: the row `{e.key}` has {len(cells(e.text))} cells and the table {width} columns "
                     f"(a stray `|`, or an unescaped one inside a cell: write it `\\|`)"
                     for e in entries if len(cells(e.text)) != width]
        findings += [f"{where} line {e.line}: the row `{e.key}` is not spelled `| cell | cell |` (one space each side of "
                     f"every separator), the one spelling a row renders back to" for e in entries
                     if len(cells(e.text)) == width and e.text != row_line(cells(e.text))]
    return Shape(kind, preamble, "\n".join(head), entries, bodies, epilogue, findings)


def render_section(heading: str, shape: Shape) -> str:
    """One section as the renderer writes it: the heading, the preamble, the entries in the order the shape holds them (a
    table under its header, a block under `#### <key>`), the row bodies under `#### <key>`, the marker and the epilogue,
    every part separated from the next by one blank line. The layout is this function's alone: `check` holds the document
    to it, and the cut renders the whole through it (DESIGN-orchestrator-loop.md section 16.8)."""
    parts = [heading] + ([shape.preamble] if shape.preamble else [])
    if shape.kind == TABLE:
        parts.append("\n".join([shape.table_head] + [e.text for e in shape.entries]))
        parts += [f"#### {b.key}\n\n{b.text}" for b in shape.bodies]
    elif shape.kind == BLOCKS:
        parts += [f"#### {e.key}\n\n{e.text}" for e in shape.entries]
    else:
        parts += [e.text for e in shape.entries]
    if shape.epilogue is not None:
        parts += [EPILOGUE_MARKER] + ([shape.epilogue] if shape.epilogue else [])
    return "\n\n".join(parts)


def render(conformance: Conformance, shapes: dict[str, Shape] | None = None) -> str:
    """The whole document as the renderer writes it, from each section's shape (`shapes` replaces the parsed shape of the
    sections it names): the text before the first section, then every section in the order written, then one newline."""
    shapes = shapes or {}
    first = conformance._headings[0][0] if conformance._headings else len(conformance.text)
    out = [conformance.text[:first].strip("\n")]
    for number in conformance.numbers():
        own = conformance.own_section(number)
        out.append(render_section(own.heading, shapes.get(number) or conformance.shape(number)))
    return "\n\n".join(out) + "\n"


def check(conformance: Conformance) -> list[str]:
    """Every way the document departs from the regular shape the renderer writes (kb/Work PB5429): a heading the reader
    does not list, an unkeyed entry, a key used twice, entries out of order, text after a section's entries outside its
    row bodies and its epilogue. [] when it is regular."""
    findings: list[str] = []
    lines = conformance.lines
    fenced = _fenced(lines)
    titles = 0
    parent = ""
    seen: list[str] = []
    for i, line in enumerate(lines):
        found = _ANY_HEADING.match(line)
        if found is None or fenced[i]:
            continue
        level = len(found.group(1))
        if level == 1:
            titles += 1
            if titles > 1:
                findings.append(f"{PATH_REL} line {i + 1}: a second title heading: {line[:80]}")
            continue
        if level == 4 and _BLOCK.match(line):
            continue   # a keyed block or a row body: its section's shape places it
        numbered = _NUMBERED.get(level)
        m = None if numbered is None else numbered.match(line)
        if m is None:
            findings.append(f"{PATH_REL} line {i + 1}: a heading the reader does not list (`## <n>. `, `### <n>.<m> ` or "
                            f"`#### <key>`): {line[:80]}")
            continue
        number = m.group(1)
        if level == 2:
            parent = number
        elif number.split(".")[0] != parent:
            findings.append(f"{PATH_REL} line {i + 1}: §{number} stands under §{parent}")
        if number in seen:
            findings.append(f"{PATH_REL} line {i + 1}: §{number} is numbered twice")
        elif seen and natural_key(number) < natural_key(seen[-1]):
            findings.append(f"{PATH_REL} line {i + 1}: §{number} follows §{seen[-1]}")
        seen.append(number)
    findings += [f"{PATH_REL} lost §{n}, a section with entries ({KINDS[n]})" for n in KINDS if n not in seen]
    keys: dict[str, int] = {}
    for number in dict.fromkeys(seen):
        shape = conformance.shape(number)
        findings += shape.findings
        for entry in shape.entries:
            if entry.key in keys:
                findings.append(f"{PATH_REL} line {entry.line}: the key `{entry.key}` is already the entry at line {keys[entry.key]} "
                                f"(a key is an anchor, unique in the document)")
            else:
                keys[entry.key] = entry.line
    if not findings:   # the parts are regular; their layout must be the renderer's too, blank lines included
        rendered = render(conformance).split("\n")
        if rendered != lines:
            at = next((i for i, (a, b) in enumerate(zip(rendered, lines)) if a != b), min(len(rendered), len(lines)))
            findings.append(f"{PATH_REL} line {at + 1}: the layout is not the renderer's from here (one blank line "
                            f"between parts, none at the end but the final newline): `render_section`")
    return findings


def parse(text: str) -> Conformance:
    """The conformance statement of a TEXT: a fabricated body of a self-test, or one a caller has read."""
    return Conformance(text)


# ── the sources: one directory per section, one file per entry (step B2, kb/Work PB5430) ───────────────────────────

def frontmatter(text: str, where: str) -> tuple[dict, str]:
    """A source file's frontmatter and body. The frontmatter is the lines between a first line `---` and the next `---`,
    each `<name>: <JSON value>` (JSON, so a heading's quotes and a cell's pipes need no escaping of their own, and every
    such value is YAML too); a malformed line is a ValueError naming the file, never a field quietly dropped."""
    lines = text.split("\n")
    if not lines or lines[0] != "---" or "---" not in lines[1:]:
        raise ValueError(f"{where}: no frontmatter (a first line `---`, `<name>: <JSON>` lines, a closing `---`)")
    end = lines.index("---", 1)
    fields: dict = {}
    for line in lines[1:end]:
        name, sep, value = line.partition(": ")
        if not sep or re.fullmatch(r"[a-z]+", name) is None:
            raise ValueError(f"{where}: frontmatter line {line[:60]!r} is not `<name>: <JSON value>`")
        try:
            fields[name] = json.loads(value)
        except json.JSONDecodeError as e:
            raise ValueError(f"{where}: frontmatter line {line[:60]!r} holds no JSON value ({e.msg})") from None
    return fields, "\n".join(lines[end + 1:]).strip("\n")


def with_frontmatter(fields: dict, body: str) -> str:
    """A source file's text: the frontmatter of `fields` (one JSON value a line), then the body, then one newline."""
    head = "".join(f"{name}: {json.dumps(value, ensure_ascii=False)}\n" for name, value in fields.items())
    return f"---\n{head}---\n" + (body + "\n" if body else "")


def file_name(key: str) -> str:
    """The name (without `.md`) of the file an entry keyed `key` is stored in: the key itself, its en dash (an A.3 item
    range, `28–30`) written as a hyphen so every tracked path stays ASCII."""
    return key.replace("–", "-")


def _read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def _heading_number(heading: str) -> str | None:
    found = HEADING.match(heading.strip())
    return None if found is None else found.group(2)


def _section_from(directory: pathlib.Path, rel: str, findings: list[str]) -> str | None:
    """One section rendered from its directory (`rel` its repository-relative name), each file out of shape a finding;
    None when the section cannot be rendered at all (no readable index)."""
    number = directory.name
    kind = KINDS.get(number, PROSE)
    try:
        fields, preamble = frontmatter(_read(directory / INDEX_FILE), f"{rel}/{INDEX_FILE}")
    except (OSError, ValueError) as e:
        findings.append(f"{rel}: its {INDEX_FILE} does not read: {e}")
        return None
    heading = fields.get("heading")
    if not isinstance(heading, str) or _heading_number(heading) != number:
        findings.append(f"{rel}/{INDEX_FILE}: its heading {heading!r} does not name §{number}, the directory's number")
        return None
    columns = fields.get("columns", [])
    if (kind == TABLE) != bool(columns):
        findings.append(f"{rel}/{INDEX_FILE}: `columns:` belongs to a table section's index, and §{number} is a {kind} section")
    epilogue_path = directory / EPILOGUE_FILE
    epilogue = _read(epilogue_path).strip("\n") if epilogue_path.is_file() else None
    entries: list[tuple[tuple, Entry]] = []
    bodies: list[Entry] = []
    for path in sorted(directory.iterdir(), key=lambda p: p.name):
        if path.name in (INDEX_FILE, EPILOGUE_FILE):
            continue
        where = f"{rel}/{path.name}"
        if path.suffix != ".md" or not path.is_file():
            findings.append(f"{where}: not an entry file (`<key>.md`)")
            continue
        key = path.stem
        if kind == PROSE:
            findings.append(f"{where}: §{number} is a prose section, whose whole text is its {INDEX_FILE}")
            continue
        if kind == BULLETS:
            body = _read(path).strip("\n")
            found = _BULLET.match(body)
            if found is None or found.group(1) != key:
                findings.append(f"{where}: the determination does not open `- **{key}` (a key is its file's name)")
            entries.append((natural_key(key), Entry(key, body, 0)))
            continue
        try:
            meta, body = frontmatter(_read(path), where)
        except ValueError as e:
            findings.append(str(e))
            continue
        if kind == TABLE:
            row = meta.get("cells")
            if not isinstance(row, list) or not row or not all(isinstance(c, str) for c in row):
                findings.append(f"{where}: `cells:` is not the row's list of cells")
                continue
            if len(row) != len(columns):
                findings.append(f"{where}: {len(row)} cells, and the table has {len(columns)} columns")
            row_key = row[0].strip("`")
            if file_name(row_key) != key:
                findings.append(f"{where}: the row's key `{row[0]}` is not its file's name ({file_name(row_key)}.md)")
            entries.append((natural_key(row_key), Entry(row_key, row_line(row), 0)))
            if body:
                bodies.append(Entry(row_key, body, 0))
        else:
            order = meta.get("order")
            if not isinstance(order, int) or isinstance(order, bool):
                findings.append(f"{where}: `order:` is not the block's place in the section (an integer)")
                continue
            entries.append(((order,), Entry(key, body, 0)))
    if kind == BLOCKS and len({o for o, _ in entries}) != len(entries):
        findings.append(f"{rel}: two blocks share an `order:`")
    head = row_line(columns) + "\n|" + "---|" * len(columns) if kind == TABLE else ""
    shape = Shape(kind, preamble, head, [e for _, e in sorted(entries, key=lambda oe: oe[0])],
                  sorted(bodies, key=lambda b: natural_key(b.key)), epilogue, [])
    return render_section(heading, shape)


def read_sources(root: pathlib.Path = REPO) -> tuple[str, list[str]]:
    """The whole document rendered from its sources at `root` (`render_section` per section, in section-number order),
    and a finding for every source out of shape: a missing or malformed frontmatter, a key that is not its file's name, a
    row whose cells are not its columns, a stray file or directory."""
    base = root / SOURCE_REL
    if not (base / HEAD_FILE).is_file():
        return "", [f"{SOURCE_REL}: no {HEAD_FILE} (the document's title and status, before section 1)"]
    findings: list[str] = []
    parts = [_read(base / HEAD_FILE).strip("\n")]
    for path in sorted(base.iterdir(), key=lambda p: natural_key(p.name)):
        rel = f"{SOURCE_REL}/{path.name}"
        if not path.is_dir():
            if path.name not in (HEAD_FILE, README_FILE):
                findings.append(f"{rel}: a file outside every section")
        elif re.fullmatch(r"[1-9][0-9]*(?:\.[1-9][0-9]*)*", path.name) is None:
            findings.append(f"{rel}: a directory that is no section number")
        elif (rendered := _section_from(path, rel, findings)) is not None:
            parts.append(rendered)
    return "\n\n".join(parts) + "\n", findings


def problems(root: pathlib.Path = REPO) -> list[str]:
    """Every way the document's sources at `root` are out of shape: the source files' own, then the rendered whole's
    (`check`). [] when the sources render a regular document; the DOC AUDITS and `conformance_doc.py --check` ask it."""
    text, findings = read_sources(root)
    return findings + (check(parse(text)) if text else [])


def load(root: pathlib.Path = REPO) -> Conformance:
    """The conformance statement of the repository at `root`, rendered from its sources once. A source out of shape is a
    SystemExit naming it: no reader works from a document it could only partly read."""
    text, findings = read_sources(root)
    if findings:
        raise SystemExit(f"⛔ {SOURCE_REL} does not read: " + "; ".join(findings[:5])
                         + (f" (+{len(findings) - 5} more)" if len(findings) > 5 else "")
                         + " — python scripts/spec/conformance_doc.py --check names them all")
    return Conformance(text)
