"""conformance_source.py — the ONE reader of docs/CONFORMANCE.md (kb/Work PB5395; DESIGN-orchestrator-loop.md §16.8, step A).

Every script that needs a register, a section, a row or a determination of the conformance statement reads it HERE, and no
other module opens the document or keeps a parser of its tables: `load(root)` reads the file once into a `Conformance`, and
`parse(text)` builds the same from a text (a self-test's fabricated body). The storage is one Markdown file today and
`Conformance` is what the rest of §16.8's steps keep stable while it is cut into one file per entry, so a reader written
against this module does not change when the storage does.

THE READER KNOWS THE DOCUMENT'S SECTIONS BY NUMBER (`## 7. …`, `### 3.2 …`), never by their title: a title is prose and is
reworded, and a number is the key every citation of the document already uses. A section runs from its heading to the next
heading of the same or a shallower level, so section 3 holds its subsections 3.1 to 3.10 and section 3.2 does not.

NOTHING IN HERE MAY BE IMPORTED BY A MODULE THE COMPILER'S TESTS RUN. `inventory_schema.py` and `generate.py` are in a compiler
test's import closure (kb/Work PB4992) and read the registers' EXTRACT (`tests/version-matrix/conformance-register.json`,
`inventory_schema.register_extract`), which `docchecks/register.py` writes from this reader; a Markdown file is inert
(kb/Work PB4453), so a reader of the document must not reach a compiler test.
"""
from __future__ import annotations

import pathlib
import re
import sys
from typing import NamedTuple

sys.path.append(str(pathlib.Path(__file__).resolve().parent))  # scripts/spec, for inventory_schema
from inventory_schema import DerivationRow  # noqa: E402  (the §8 row's shape, which the registers' extract shares)

REPO = pathlib.Path(__file__).resolve().parents[2]
#: THE document's path, relative to a repository root. The only place a script names it.
PATH_REL = "docs/CONFORMANCE.md"
PATH = REPO / "docs" / "CONFORMANCE.md"

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


def parse(text: str) -> Conformance:
    """The conformance statement of a TEXT: a fabricated body of a self-test, or one a caller has read."""
    return Conformance(text)


def load(root: pathlib.Path = REPO) -> Conformance:
    """The conformance statement of the repository at `root`, read once (UTF-8, universal newlines)."""
    return Conformance((root / PATH_REL).read_text(encoding="utf-8"))
