#!/usr/bin/env python3
"""rule_catalog_1985.py - the 1985 arm of the rule catalog (`extract_rule_catalog.py --edition 1985`, kb/Work PB4886).

The 1985 audit is the 2023 audit run with an edition parameter (docs/rearchitecture/DESIGN-spec-conformance-review.md
§10), and its catalog is the same denominator read from `specs/ANSI_COBOL_1985.md` in that document's OWN structure, which
is not ISO's:

  - the document is eighteen Roman SECTIONS, and paragraph numbers restart in each one, so a rule's locator always carries
    its section (`VI.5.7.3`, the citation format of scripts/spec/citation.py); Amendment 1 (ANSI X3.23a-1989) adds
    `SECTION A: INTRINSIC FUNCTION MODULE`, cited `§1989:A.<paragraph>`;
  - Sections VI to XVI are the eleven MODULES (Nucleus ... Segmentation) and Section A the twelfth; every module but
    Sort-Merge, Report Writer and Intrinsic Function has two LEVELS, and the standard draws a BOX around every
    specification unique to level 2 (§1985:I.1.3; FIGURE-STYLE.md rule 9 for a general format);
  - a rule block is a paragraph headed `Syntax Rules`, `General Rules`, `Arguments` or `Returned Values`, or another title
    THE kind table types (`Editing Rules`), or one the content-keyed scan's manifest admits (headed with `#` or, where the
    transcription did not head it, a plain line opening with its number, as citation.PARAGRAPH_85 reads both), and a
    rule is a column-0 `(n)`; its lettered `a.` items and their `1)` items are its text (THE RULE IS THE
    UNIT, design §3);
  - Amendment 1's Addendum 1 and Amendment 2 (ANSI X3.23b-1993) are TABLES OF CHANGES to the base text, a row per change
    keyed by the printed page it begins on (`VI-104`, `(A-12)` for an Amendment 1 page) and a paragraph and rule; a rule a
    change names carries that amendment in `amended_by`, because the amended 1985 is a different language level for
    edition gating. The transcription applies every change in place (kb/Work PB5045), so a rule a change ADDS is a row
    of the body, at the number the amended text gives it, and the change only records `amended_by` on it: an amended
    rule has one home (docs/rearchitecture/DESIGN-spec-conformance-review.md section 10.2).

THE ROW. `id` `<year>-<kind>-<locator>-<n>` (a sub-list restarted inside a block: `-L<sublist>.<n>`, as 2023's), its
citation (`§1985:VI.5.7.3 SR1`), section, paragraph, module, subject (the enclosing paragraph's title), kind (SR, GR, AR,
RV, FMT for a general format), ordinal, `box` (none, part, whole: how much of the rule the level-2 box covers), `level`
(1 or 2: 2 only when the WHOLE rule is boxed; None outside a module), `document` (the base standard or Amendment 1),
`amended_by`, its `status` (normal, or obsolete naming its §1985:C.1 entry: element_standing.py, kb/Work PB4993), the
PDF page and printed folio it starts on, the printed band in effect, its text, and `provisional` (none, part, whole:
how much of its text is a RECONSTRUCTION of a printed page the scan lacks, cite.RECONSTRUCTED, kb/Work PB4952; a
provisional rule's text is the transcription's, not the standard's, and the inventory row and every verdict built on it
carry the mark until the printed page's verbatim text replaces the reconstruction). A UNIT OF UNNUMBERED TEXT disposed
a row kind in the content-keyed manifest is a row too (`mint`, kb/Work PB5452): `-p<n>`, `-T<t>.<r>`, `-T<t>`, an item's
`-<n>`, with its `shape`, and `definitional` where its disposition says so; every row of a list a colon unit leads
carries that unit's words as `lead`.

COMPLETENESS (design §7) is held the 2023 way: a rule-shaped heading no kind resolves, a recognised block that yields no
rule, an ordinal that neither continues its list nor restarts it, a box inside a one-level module, an amendment change
that names a rule this catalog cannot find, an adjudication
(EXCLUDED_BLOCKS, KNOWN_EMPTY_BLOCKS) the transcription no longer has, a paragraph carrying a numbered list
that the content-keyed scan's manifest does not record as measured (the 2023 arm's PB29 check), a rule of an admitted
paragraph its kind map does not name, a row whose citation names no region of the transcription, and a disagreement
between the standing lists' data and the text (`element_standing.findings`) are each REPORTED,
and `--check` fails on them. A page the scan prints twice (citation.SCAN_REPEAT_85) is read once.

    python scripts/spec/extract_rule_catalog.py --edition 1985 --check   # counts and findings; writes nothing
    python scripts/spec/rule_catalog_1985.py --self-test                 # planted text, no transcription read
"""
from __future__ import annotations

import collections
import dataclasses
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from citation import (AMENDMENT_85, APPENDIX_85, FOLIO_85, MARK_85, PARAGRAPH_85,  # noqa: E402
                      SCAN_REPEAT_85, SECTION_85, SECTIONS_85)  # (THE readers of the 1985 structure, kb/Work PB4887)
from spec_pdf import BOX_SIDE, PAGE_HEADING  # noqa: E402  (THE side of a level-2 box, kb/Work PB4850; THE page heading)
# THE shapes of an editorial note (a correction, R66; a reconstruction's opening and closing, kb/Work PB4952): never rule
# text. A reconstruction's own text IS read, its rows marked provisional (`reconstructed`).
from cite import EDITORIAL_NOTE, RECONSTRUCTED, RECONSTRUCTED_END, reconstructed  # noqa: E402
from spec_headings import GENERAL_FORMAT_NOUN, GENERAL_FORMAT_TITLE  # noqa: E402  (THE spelling of a general-format heading, kb/Work PB284)
# THE singular form of a block title, THE kind table, any Markdown heading, and a printed band inside a rule list
# ("FORMAT 2", "SEQUENTIAL FILES"): the 2023 arm's, one definition each.
from extract_rule_catalog import ANY_HEADING, GROUP_LABEL, KINDS, canonical_title  # noqa: E402
# THE content-keyed scan (kb/Work PB29), its manifest and its report: the 2023 arm's, with the edition as a parameter.
from extract_rule_catalog import (RULE_SHAPED, UNHARVESTED_OF, admitted_kinds, kinds_of, ordinal_shape, rule_kind,  # noqa: E402
                                  unharvested_report)
import citation  # noqa: E402  (THE validator of a citation against the transcription: every row's must name a region)
import element_standing  # noqa: E402  (THE standing of a rule, from the edition's lists; kb/Work PB4993)
import text_units  # noqa: E402  (THE units of unnumbered text and their dispositions, every edition; kb/Work PB5449)
sys.path.append(str(Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The MODULES and their levels, by section (§1985:I.1.2: eleven modules, "Two of the modules contain only level 1
#: elements", Sort-Merge and Report Writer; Amendment 1 adds the Intrinsic Function module, "only level 1 elements", and
#: changes that sentence to "Three of the modules"). A section absent here (I-V, XVII) is no module: its rows carry no level.
MODULES: dict[str, tuple[str, int]] = {
    "VI": ("Nucleus", 2), "VII": ("Sequential I-O", 2), "VIII": ("Relative I-O", 2), "IX": ("Indexed I-O", 2),
    "X": ("Inter-Program Communication", 2), "XI": ("Sort-Merge", 1), "XII": ("Source Text Manipulation", 2),
    "XIII": ("Report Writer", 1), "XIV": ("Communication", 2), "XV": ("Debug", 2), "XVI": ("Segmentation", 2),
    "A": ("Intrinsic Function", 1),
}
#: The documents a row comes from, and the citation year of each (citation.EDITIONS).
BASE, AMENDMENT_1, AMENDMENT_2 = "X3.23-1985", "X3.23a-1989", "X3.23b-1993"
YEAR_OF = {BASE: 1985, AMENDMENT_1: 1989, AMENDMENT_2: 1993}
#: The section that carries no rule: the Index.
NO_RULES = {"XVIII"}
#: Rule-shaped paragraphs deliberately outside the denominator, each with its reason (the ISO arm's
#: `IsoEdition.excluded_blocks`).
EXCLUDED_BLOCKS = {
    "IV.3": "'RULES' defines the rule taxonomy itself (§1985:IV.3.1 and IV.3.2 are prose about what syntax rules and "
            "general rules ARE) and carries no numbered rule, as §2023:5.3 does for 2023",
    "IV.3.1": "the definition of a syntax rule (see IV.3)",
    "IV.3.2": "the definition of a general rule (see IV.3)",
    "VI.6.4": "'COMMON OPTIONS AND RULES FOR STATEMENTS' is the parent of the ROUNDED, SIZE ERROR, CORRESPONDING ... "
              "paragraphs: its own text is one sentence about them (§1985:VI.6.4 'provide a description of the common "
              "options') and no numbered rule",
    "XIII.3.10": "'PRESENTATION RULES TABLES' is the parent of the presentation rules paragraphs XIII.3.10.1 to "
                 "XIII.3.10.10, and carries no text of its own; the four tables' rules are admitted in the manifest",
}

#: THE rule-block kinds are extract_rule_catalog.KINDS, by the canonical title: one table for both editions. The 1985
#: document heads them "Syntax Rules", "General Rules" and, in Amendment 1's Section A, "Arguments" and "Returned
#: Values"; its PICTURE clause's "Editing Rules" and "Precedence Rules" are the same rules §2023:5.3.3 and §2023:5.3.2
#: type GR and SR, and the table types them so in both editions.
#: A heading that names rules in some spelling (extract_rule_catalog.RULE_SHAPED) and that neither KINDS nor the manifest
#: types holds numbered rules this catalog does not harvest, and is reported: which kind each is is an adjudication,
#: never a guess here.
#: THE ADJUDICATED PARAGRAPHS are the content-keyed scan's manifest, the 2023 arm's mechanism with the edition as its
#: parameter (extract_rule_catalog.UNHARVESTED_OF[1985], kb/Work PB29 and PB4886): every paragraph whose heading names no
#: rule block and whose body carries a column-0 `(n)` list is MEASURED (ordinal_shape) and recorded there, keyed by its
#: citation, with its disposition and reason: `rules` and its kind (admitted_kinds' three shapes), or `not-rules`. The
#: test is §1985:IV.3.1 (a syntax rule defines "the order in which words or elements are arranged", and may "impose
#: restrictions on individual words or elements or relax restrictions implied by words or elements") and §1985:IV.3.2 (a
#: general rule defines "the meaning or relationship of meanings" and "the effect that it has on either execution or
#: compilation"). Admitting a paragraph is a data edit with its reason, never a code edit.
#: The block kind of an admitted paragraph whose rules are not all one kind (its rows carry each rule's own kind).
MIXED = "MIXED"
#: Recognised blocks that rightly yield no numbered rule (the ISO arm's `IsoEdition.known_empty_blocks`).
KNOWN_EMPTY_BLOCKS = {
    "VI.5.9.6": "the PICTURE precedence rules are unnumbered prose and the chart on page VI-37 (§1985:VI.5.9.6 'The "
                "chart on page VI-37 shows the order of precedence'): typed SR by KINDS, as 2023's, with no row",
}
#: How much of a rule's text a reconstruction holds (`provisional`), by whether its lines came from inside one.
PROVISIONAL = {frozenset(): "none", frozenset({False}): "none", frozenset({True}): "whole",
               frozenset({False, True}): "part"}
#: A top-level rule: `(1)` at column 0 (the transcription may escape the parentheses).
#: A boxed rule opens with the box's side (`║(1) …║`), which stays with the rule's text so `box_of` sees the whole box.
ORDINAL = re.compile(rf"^(?P<box>{BOX_SIDE}?)\\?\((?P<n>\d+)\\?\)\s+(?P<text>.*)$")
#: THE LEVEL-2 BOX of a prose span: a `║` at each end of each boxed block or span (spec_pdf.BOX_SIDE; the markup is
#: TRANSCRIPTION-STATE.md "The 1985 transcription", kb/Work PB4850). A box never nests, so its sides pair in order.
BOX_SPAN = re.compile(rf"{BOX_SIDE}(?P<boxed>[^{BOX_SIDE}]*){BOX_SIDE}")
#: The level-2 box of a general format: FIGURE-STYLE.md rule 9 draws it in DOUBLE lines.
BOX_GLYPHS = re.compile(r"[╔╗╚╝║═]")
#: A change row of an amendment's table: `| VI-104 | Paragraph 5.8 entitled ..., general rule 4, ... |`, the page cell a
#: base folio, an Amendment 1 folio in parentheses, or both (`VI-104<br>(A-12)`).
CHANGE_ROW = re.compile(r"^\|\s*(?P<page>[^|]*?)\s*\|\s*(?P<change>[^|].*?)\s*\|\s*$")
#: A change row of a table page the transcription prints without the table's pipes (Addendum 1, PDF page 839: "**IV-4**
#: Paragraph 4.2.1, rule (4), change ..."): the page cell in bold, then the change; a cell may span pages ("**IV-22 thru
#: IV-24** Renumber paragraphs ...", page 841).
CHANGE_PROSE_ROW = re.compile(r"^\*\*(?P<page>\(?(?:[IVX]+|[A-F])-\d+\)?(?: thru \(?(?:[IVX]+|[A-F])-\d+\)?)?(?:\s*\(T\))?)"
                              r"\*\*\s+(?P<change>\S.*?)\s*$")
#: The table's column heads repeated at a page top without the table's pipes ("**Page No.** **Description of Change**").
CHANGE_HEADER = re.compile(r"^\**Page No\.")
#: A line of a change table that OPENS WITH A PAGE CELL but is in neither row shape ("IX-30 — Paragraph 4.5.4 ...",
#: "IV-21 Paragraph 4.3.8.2.3, ..."): a change row transcribed as prose, which no reader would count and the open row
#: would swallow as its own new text, so a change the transcription applied in place would record no amendment
#: (kb/Work PB5045). It is a finding, never read.
CHANGE_UNREAD = re.compile(r"^\(?(?:[IVX]+|[A-F])-\d+\)?(?:\s+thru\s+\(?(?:[IVX]+|[A-F])-\d+\)?)?\s+(?:[—–-]\s+)?[A-Z]")
CHANGE_PAGE = re.compile(r"(?P<amd>\()?(?P<folio>(?:[IVX]+|[A-F])-\d+)\)?")
#: The paragraph a change names: "Paragraph 4.5.4 ...", or "New paragraph 4.3.8.4.4, ..." (X3.23a-1989 IV-22).
CHANGE_PARAGRAPH = re.compile(r"\b[Pp]aragraphs? (?P<num>\d+(?:\.\d+)*)")
CHANGE_RULE = re.compile(r"\b(?:(?P<kind>syntax|general|arguments?|returned values?) )?rules? \(?(?P<n>\d+)\)?(?![\d.])",
                         re.IGNORECASE)
#: A change to a GENERAL FORMAT, read from a directive with its quoted text dropped: "replace the general format", "delete
#: the box around the general format", "general format of the IF statement, delete ...", "in format 3" — never "the first
#: paragraph after the general format beginning with ...", which changes the prose below the format.
CHANGE_FORMAT = re.compile(rf"(?<!after )\bthe {GENERAL_FORMAT_NOUN}\b|\b{GENERAL_FORMAT_NOUN} of\b|\bin format \d+\b",
                           re.IGNORECASE)
#: A change that ADDS a rule: "add the following as the new general rule 4", "add new syntax rule 4 as follows" (X3.23a-1989
#: X-27, XIV-26), "as new syntax rule 5", "as a new general rule 12" (never "as a new last paragraph for general rule 7", which amends rule 7).
CHANGE_ADDS = re.compile(r"\b(?:as (?:the |a )?|add )new (?P<kind>syntax|general|arguments?|returned values?) rule (?P<n>\d+)",
                         re.IGNORECASE)


def kind_of(title: str) -> str | None:
    """The kind of the block a paragraph title (its markup already dropped) heads: FMT for a general format."""
    if GENERAL_FORMAT_TITLE.fullmatch(title.strip()):
        return "FMT"
    return KINDS.get(canonical_title(title))


def running_header(line: str) -> bool:
    """The page's running header, the first line of a printed page ("Nucleus - Level-Number", "Introduction"): short, no
    closing punctuation, and none of the shapes a rule, a heading, a figure or a table opens with."""
    s = line.strip()
    return (0 < len(s) <= 70 and not re.search(r"[.:;,]$", s) and not re.match(r"[(#<|>\-*a-z0-9]", s)
            and ORDINAL.match(s) is None)


@dataclasses.dataclass
class Paragraph:
    """One paragraph of the transcription as `parse` walks it: its locator (`<section or appendix>.<number>`, or the bare
    section or appendix for the text between its heading and its first paragraph), its title, the kind its title types
    (`kind_of`; None for prose), and every body line the walk reads under it, skips applied (a repeated page, an editorial
    note, a folio, a running header), each as (text, PDF page, printed folio, 1-based line: where a unit holds it,
    `text_units.Unit.lines`). THE ONE WALK of the 1985 transcription:
    `catalog_prose_holes.py` reads these, never a copy of the walk (kb/Work PB5924 review R1-3)."""
    locator: str
    title: str
    kind: str | None
    section: str                     # the Roman section, or "A" (Amendment 1's Section A)
    appendix: str | None             # the appendix of Section XVII, whose letter leads the locator (`C.2`), else None
    lines: list[tuple[str, int, str, int]] = dataclasses.field(default_factory=list)


@dataclasses.dataclass
class Catalog:
    """The rows, every paragraph the walk read, and every finding `--check` fails on."""
    rules: list[dict] = dataclasses.field(default_factory=list)
    blocks: list[tuple[str, str]] = dataclasses.field(default_factory=list)   # (locator, kind) of every block seen
    #: every paragraph of a section or an appendix, by locator, in reading order (`Paragraph`)
    paragraphs: dict[str, Paragraph] = dataclasses.field(default_factory=dict)
    empty: list[str] = dataclasses.field(default_factory=list)               # a recognised block with no rule
    unrecognised: list[tuple[str, str]] = dataclasses.field(default_factory=list)  # (locator, title)
    unexplained: list[tuple[str, int, int]] = dataclasses.field(default_factory=list)  # (locator, ordinal, after)
    one_level_boxes: list[str] = dataclasses.field(default_factory=list)     # a box in a module with one level
    changes: int = 0                                                          # amendment change rows read
    unresolved: list[tuple[str, str, str]] = dataclasses.field(default_factory=list)  # (document, page, change)
    unread: list[str] = dataclasses.field(default_factory=list)   # a change row in neither row shape (CHANGE_UNREAD)
    stale: list[str] = dataclasses.field(default_factory=list)   # an adjudication the transcription no longer needs
    #: THE CONTENT-KEYED SCAN (kb/Work PB29): every paragraph whose heading names no rule block and whose body carries a
    #: numbered list, by citation ⇒ {title, ordinals, lists, longest}; and the manifest's record of them (`declared`).
    unharvested: dict[str, dict] = dataclasses.field(default_factory=dict)
    declared: dict[str, dict] = dataclasses.field(default_factory=dict)
    untyped: list[tuple[str, int, int]] = dataclasses.field(default_factory=list)  # (citation, sublist, ordinal)
    uncitable: list[str] = dataclasses.field(default_factory=list)  # a row whose citation names no region (cite_check)
    standing: list[str] = dataclasses.field(default_factory=list)   # the standing lists' data against the text
    #: The dispositions audit of the units of unnumbered text (text_units.audit, kb/Work PB5449), and the units' count.
    dispositions: list[str] = dataclasses.field(default_factory=list)
    pending_units: int = 0
    #: How many rows the units disposed a row kind made (`mint`, kb/Work PB5452).
    minted: int = 0
    #: The paragraphs after a rule held to the margin-level data (measure_margin_levels.refusals, kb/Work PB5449 item 3),
    #: and the recorded paragraphs by what their level makes them (measure_margin_levels.tally).
    margin_levels: list[str] = dataclasses.field(default_factory=list)
    margin_tally: dict[str, int] = dataclasses.field(default_factory=dict)
    #: The 1-based transcription line each numbered rule opens on, by row id: where `mint` places a unit's row among
    #: its clause's rules, and where a unit's text ends (kb/Work PB5452).
    row_line: dict[str, int] = dataclasses.field(default_factory=dict)

    def drift(self) -> tuple[list[str], int]:
        """The content-keyed check's report and its number of drifts: THE 2023 arm's, on this edition's manifest."""
        return unharvested_report(self.unharvested, self.declared, UNHARVESTED_OF[1985], label="")

    def duplicates(self) -> list[str]:
        """Every id more than one row carries: the inventory is keyed on it, so a duplicate is a lost row."""
        counts = collections.Counter(r["id"] for r in self.rules)
        return sorted(i for i, n in counts.items() if n > 1)

    def shared_citations(self) -> list[str]:
        """Every citation more than one row carries: a citation names ONE rule (kb/Work PB4969), so a rule of a restarted
        list is cited by its list (citation.rule_designator)."""
        counts = collections.Counter(r["citation"] for r in self.rules)
        return sorted(c for c, n in counts.items() if n > 1)

    def findings(self) -> int:
        return (len(self.empty) + len(self.unrecognised) + len(self.unexplained) + len(self.one_level_boxes)
                + len(self.unresolved) + len(self.unread) + len(self.stale) + len(self.duplicates())
                + len(self.shared_citations())
                + self.drift()[1] + len(self.untyped) + len(self.uncitable) + len(self.standing)
                + len(self.dispositions) + len(self.margin_levels))


def box_of(text: str, kind: str) -> str:
    """How much of the rule the level-2 box covers: none, part or whole."""
    if kind == "FMT":
        lines = [ln for ln in text.splitlines() if ln.strip() and not ln.lstrip().startswith("<")]
        if not any(BOX_GLYPHS.search(ln) for ln in lines):
            return "none"
        return "whole" if all(re.match(r"\s*[╔║╚]", ln) for ln in lines) else "part"
    if not BOX_SPAN.search(text):
        return "none"
    rest = BOX_SPAN.sub("", text)
    return "whole" if not re.sub(r"[\s.,;:]", "", rest) else "part"


#: What `furniture` says a line of the transcription is when it is no text: a page heading (`## Page 599`), a line of a
#: page the scan prints twice (SCAN_REPEAT_85, its marker included), an editorial note, a folio, a running header.
PAGE, REPEAT, NOTE, FOLIO, HEADER = "page", "repeat", "note", "folio", "header"


def furniture(lines: list[str]) -> list[str | None]:
    """THE ONE READING OF THE 1985 PAGE FURNITURE (kb/Work PB5998): for each line of the transcription, what it is when it
    is no text (`PAGE`, `REPEAT`, `NOTE`, `FOLIO`, `HEADER`), else None; a blank line is None (it is the segmentation's
    separator). Read ONCE over the whole transcription, in order, so every reader of a region (`parse`, `_bodies`) agrees
    on it however the region starts: a region that opens on a `## Page` line or after a page the scan repeats still
    sees that page's running header as furniture.
      - ⛔ AN EDITORIAL NOTE IS NOT RULE TEXT (R66, kb/Work PB4972, PB4952): `> ⚠ **CORRECTED — see the Errata (Cn).**`
        flags a correction where it was made and quotes the printed form, the rule's text being the corrected line above
        it; a reconstruction's opening and closing notes name its folios, sources and confidence. The reconstruction is
        no printed page, so it opens with no running header; the printed page after it resumes with one.
      - A folio ends a printed page. It is the last line of a `## Page` but where printed pages from another scan stand
        inside one (cite.SOURCE, kb/Work PB5789), and the next printed page opens with its running header."""
    out: list[str | None] = [None] * len(lines)
    page_top = repeat = in_note = False
    for at, line in enumerate(lines):
        if PAGE_HEADING.match(line):
            out[at], page_top, repeat = PAGE, True, False
            continue
        if repeat:
            out[at] = REPEAT
            continue
        if not line.strip():
            in_note = False
            continue
        if EDITORIAL_NOTE.match(line) or (in_note and line.startswith(">")):
            out[at], in_note = NOTE, True
            if RECONSTRUCTED.match(line):
                page_top = False
            elif RECONSTRUCTED_END.match(line):
                page_top = True
            continue
        in_note = False
        if SCAN_REPEAT_85.match(line):
            out[at], repeat = REPEAT, True
            continue
        if FOLIO_85.match(line):
            out[at], page_top = FOLIO, True
            continue
        if page_top:
            page_top = False
            if running_header(line):
                out[at] = HEADER
    return out


def folios_below(lines: list[str]) -> list[str]:
    """For each line (0-based), the printed folio of the page it is on: a rule is on the printed page whose folio is the
    first one below it on its `## Page`, one per page, but where printed pages from another scan stand inside a page
    (cite.SOURCE, kb/Work PB5789) each closes with its own."""
    folio_below: list[str] = [""] * len(lines)
    below = ""
    for at in range(len(lines) - 1, -1, -1):
        if PAGE_HEADING.match(lines[at]):
            below = ""
        elif (f := FOLIO_85.match(lines[at])) is not None:
            below = f.group(1)
        folio_below[at] = below
    return folio_below


def pages_of(lines: list[str]) -> list[int]:
    """For each 1-based line number (index 0 unused), the PDF page its `## Page` heading names."""
    page_of, page = [0] * (len(lines) + 1), 0
    for at, line in enumerate(lines, start=1):
        if PAGE_HEADING.match(line):
            page = int(line.split()[-1])
        page_of[at] = page
    return page_of


def parse(lines: list[str], manifest: dict[str, dict] | None = None, carved: frozenset[int] = frozenset()) -> Catalog:
    """The 1985 catalog of a transcription's lines (the module docstring states the structure it reads), with the
    content-keyed scan measured against `manifest`, the `blocks` of extract_rule_catalog.UNHARVESTED_OF[1985]: the
    paragraphs it admits are rule blocks of their declared kind. `carved` holds the 1-based lines of the decided margin
    paragraphs (`carved_lines`): they leave the text of the rule they follow, each a unit of its own (decision PB5448
    point 4; kb/Work PB5452)."""
    cat = Catalog(declared=dict(manifest or {}))
    admitted = admitted_kinds(cat.declared)
    folio_below = folios_below(lines)

    section: str | None = None        # the Roman section, or "A"; None outside every section
    appendix: str | None = None       # the appendix of Section XVII being read, its letter the locator's (`§1985:C.2`)
    document = BASE
    titles: dict[str, str] = {}        # locator -> title of every paragraph of the section, for the subject
    block: tuple[str, str, str] | None = None   # (locator, kind, paragraph) of the open rule block
    block_rules = 0
    buf: list[str] = []
    start: dict = {}
    sublist, last = 1, 0
    band = ""
    page = 0
    amendment: str | None = None       # the amendment whose part is being read
    change_rows: list[tuple[str, str, str]] = []   # (document, page cell, change)
    open_change = False                # the last change row's new text follows the table row as prose

    def flush() -> None:
        nonlocal buf, drawn_from
        if not buf or block is None:
            buf, drawn_from = [], set()
            return
        loc, kind, para = block
        kind = start.get("kind", kind)   # a rule an admitted paragraph types by its ordinal (rule_kind)
        text = re.sub(r"[ \t]+", " ", "\n".join(buf)).strip() if kind != "FMT" else "\n".join(buf).strip()
        year, folio = YEAR_OF[document], start["folio"]
        cat.row_line[add_row(cat, document=document, section=section, paragraph=para, kind=kind, n=start["n"],
                sublist=start["sublist"], text=text, page=start["page"], folio=folio,
                band=start["band"], subject=titles.get(para.rsplit(".", 1)[0], ""),
                citation=f"§{year}:{loc}" + ("" if kind == "FMT" else
                                             f" {citation.rule_designator(kind, start['n'], start['sublist'])}"),
                provisional=PROVISIONAL[frozenset(drawn_from)])["id"]] = start["line"]
        buf, drawn_from = [], set()

    def close_block() -> None:
        nonlocal block, block_rules
        flush()
        if block is not None and block_rules == 0 and block[0] not in KNOWN_EMPTY_BLOCKS:
            cat.empty.append(f"{block[1]} {block[0]}")
        block, block_rules = None, 0

    def open_block(loc: str, kind: str, num: str, admitted_as: str | dict | None = None) -> None:
        nonlocal block, block_rules, sublist, last, band, spec
        block, block_rules, sublist, last, band = (loc, kind, num), 0, 1, 0, ""
        spec = admitted_as
        cat.blocks.append((loc, kind))

    def close_scan() -> None:
        """Record the paragraph the content-keyed scan is reading, if its body carries a numbered list."""
        nonlocal scan, scan_body
        if scan is not None and (shape := ordinal_shape(scan_body, ORDINAL)) is not None:
            # Its `digest` keys the refuter record a `not-rules` disposition names (text_units.block_refusals): the whole
            # body's, as a unit's is its whole text's, so a changed text matches no record (kb/Work PB5452).
            cat.unharvested[scan[0]] = {"title": scan[1], **shape, "digest": text_units.digest(" ".join(scan_body))}
        scan, scan_body = None, []

    spec: str | dict | None = None  # the open block's kind as the manifest admits it (rule_kind), or None
    scan: tuple[str, str] | None = None   # (citation, title) of the paragraph the content-keyed scan reads
    scan_body: list[str] = []
    met: set[str] = set()          # every adjudicated locator the transcription still has
    # ⛔ A RECONSTRUCTION IS READ AS TEXT, ITS ROWS MARKED (kb/Work PB4952): it stands where the scan lacks printed pages
    # and carries their headings and rules, so a rule continues across it as across a page break; `drawn_from` records
    # whether the open rule's lines came from inside one (True), from the printed text (False), or both, and a rule
    # that opens inside one is on the folio it reconstructs.
    recon_folio = reconstructed(lines)
    inside = [f is not None for f in recon_folio]
    drawn_from: set[bool] = set()
    para: Paragraph | None = None   # the paragraph the walk is in (Catalog.paragraphs), None outside every section
    furn = furniture(lines)
    for at, line in enumerate(lines):
        if furn[at] == PAGE:
            page = int(line.split()[-1])
            continue
        if furn[at] is not None or not line.strip():
            continue
        if AMENDMENT_85.match(line):
            close_block()
            close_scan()
            amendment = AMENDMENT_1 if line.split()[2].rstrip(":") == "1" else AMENDMENT_2
            section, document, appendix, para = None, BASE, None, None
            continue
        if (head := SECTION_85.match(line) or APPENDIX_85.match(line)) is not None:
            close_block()
            close_scan()
            name = head.group(1)
            if SECTION_85.match(line):
                appendix = None
                if name in SECTIONS_85:
                    section, document, amendment = name, BASE, None
                elif name == "A" and amendment == AMENDMENT_1:
                    section, document = "A", AMENDMENT_1
                else:
                    section = None
            else:
                appendix = name
            titles = {}
            para = None if section is None else cat.paragraphs.setdefault(
                appendix or section, Paragraph(appendix or section, "(section preamble)", None, section, appendix))
            continue
        if section is None:
            if amendment is None:
                continue
            if (row := CHANGE_ROW.match(line) or CHANGE_PROSE_ROW.match(line)) is not None:
                open_change = CHANGE_PAGE.search(row["page"]) is not None
                if open_change:
                    change_rows.append((amendment, row["page"], row["change"]))
                open_change = open_change and row["change"].endswith(":") and "<br>" not in row["change"]
            elif CHANGE_UNREAD.match(line):
                cat.unread.append(f"page {page}: {line.strip()[:90]}")
                open_change = False
            elif open_change and not ANY_HEADING.match(line) and not CHANGE_HEADER.match(line):
                # A change whose new text the table prints BELOW its row ("add the following as the new general
                # rule 19:", then the rule as prose): the text is the row's, as a `<br><br>` cell would carry it.
                doc_, cell, change = change_rows[-1]
                change_rows[-1] = (doc_, cell, f"{change}<br><br>{line.strip()}")
            continue
        p = PARAGRAPH_85.match(line)
        if p is not None:
            close_block()
            close_scan()
            num = (p.group(1) or p.group(2)).rstrip(".")
            title = re.sub(r"^#*\s*" + MARK_85 + re.escape(num) + r"\.?\s*", "", line).strip()
            title = re.sub(rf"</?u>|\*\*|{BOX_SIDE}", "", title).strip()
            loc = f"{appendix or section}.{num}"
            titles[num] = title
            kind = kind_of(title)
            para = cat.paragraphs.setdefault(loc, Paragraph(loc, title, kind, section, appendix))
            para.kind = kind
            if kind is None:
                # THE CONTENT-KEYED SCAN reads every paragraph a heading does not type, in every section and appendix:
                # a guard keyed on what a block is CALLED cannot find one called something else (kb/Work PB29).
                scan = (f"§{YEAR_OF[document]}:{loc}", title)
            if section in NO_RULES or appendix is not None:
                # The Index and the appendices carry no rule block; one headed as such here is reported, not read.
                if kind is not None or RULE_SHAPED.search(title):
                    cat.unrecognised.append((loc, title))
                continue
            if loc in EXCLUDED_BLOCKS:
                met.add(loc)
                continue
            admitted_as = admitted.get(f"§{YEAR_OF[document]}:{loc}") if kind is None else None
            if admitted_as is not None:
                kinds = kinds_of(admitted_as)
                kind = kinds.pop() if len(kinds) == 1 else MIXED
            if loc in KNOWN_EMPTY_BLOCKS:
                met.add(loc)
            if kind is not None:
                open_block(loc, kind, num, admitted_as)
                if kind == "FMT":
                    start = {"n": 0, "sublist": 1, "page": page, "band": "", "folio": recon_folio[at] or folio_below[at],
                             "line": at + 1}
                    buf, drawn_from = [], set()
                    block_rules = 1
            elif RULE_SHAPED.search(title):
                cat.unrecognised.append((loc, title))
            continue
        if para is not None:
            para.lines.append((line.strip(), page, recon_folio[at] or folio_below[at], at + 1))
        if scan is not None:
            if ANY_HEADING.match(line) and GROUP_LABEL.match(line) is None:
                close_scan()
            else:
                scan_body.append(line)
        if section in NO_RULES or appendix is not None:
            continue
        if block is None:
            continue
        if block[1] == "FMT":
            buf.append(line)
            drawn_from.add(inside[at])
            continue
        if ANY_HEADING.match(line) and GROUP_LABEL.match(line) is None:
            close_block()
            continue
        if (b := GROUP_LABEL.match(line)) is not None and not ORDINAL.match(line):
            band = b.group("label").strip()
            continue
        if (m := ORDINAL.match(line)) is not None:
            n = int(m.group("n"))
            flush()
            if n == last + 1 or last < 0:
                pass
            elif n == 1 and last:
                sublist += 1
            else:
                cat.unexplained.append((block[0], n, last))
            last = n
            block_rules += 1
            kind = block[1] if spec is None else rule_kind(spec, sublist, n)
            if kind is None:   # the manifest's map does not name this rule: reported, never guessed
                cat.untyped.append((f"§{YEAR_OF[document]}:{block[0]}", sublist, n))
                kind = block[1]
            start = {"n": n, "sublist": sublist, "page": page, "band": band, "kind": kind,
                     "folio": recon_folio[at] or folio_below[at], "line": at + 1}
            buf, drawn_from = [m.group("box") + m.group("text")], {inside[at]}
        elif buf and at + 1 not in carved:
            buf.append(line.strip())
            drawn_from.add(inside[at])
    close_block()
    close_scan()
    for declared in (*EXCLUDED_BLOCKS, *KNOWN_EMPTY_BLOCKS):
        if declared not in met:
            cat.stale.append(f"{declared}: adjudicated here, but the transcription no longer has it")
    cat.changes = len(change_rows)
    for doc, cell, change in change_rows:
        resolve_change(cat, doc, cell, change)
    return cat


def add_row(cat: Catalog, *, document: str, section: str | None, paragraph: str, kind: str, n: int, sublist: int,
            text: str, page: int, folio: str, band: str, subject: str, citation: str, provisional: str = "none") -> dict:
    """THE one constructor of a catalog row (module docstring: THE ROW), for a rule of the base text, of Section A, or one
    an amendment adds (an amendment's change table is printed whole: its rows are never provisional)."""
    loc = f"{section}.{paragraph}"
    rid = f"{YEAR_OF[document]}-{kind}-{loc}" + ("" if kind == "FMT" else f"-{n}" if sublist == 1 else f"-L{sublist}.{n}")
    module, levels = MODULES.get(section or "", (None, 0))
    box = box_of(text, kind)
    if box != "none" and levels == 1:
        cat.one_level_boxes.append(rid)
    row = {"id": rid, "citation": citation, "document": document, "section": section, "paragraph": paragraph,
           "locator": loc, "module": module, "subject": subject, "kind": kind, "ordinal": None if kind == "FMT" else n,
           "sublist": sublist, "box": box, "level": None if module is None else (2 if box == "whole" and levels == 2 else 1),
           "amended_by": [], "status": element_standing.NORMAL, "page": page, "folio": folio, "band": band, "text": text,
           "provisional": provisional}
    cat.rules.append(row)
    return row


def resolve_change(cat: Catalog, doc: str, cell: str, change: str) -> None:
    """Record amendment `doc` on the rule one change row names, a rule it changes or one it adds ("add the following as
    the new general rule 4"); a row naming a rule the catalog cannot find is reported. A row that names no rule (a
    heading, a figure, a table of contents) changes no row and is left alone.
    The transcription applies every change in place (kb/Work PB5045), so a rule a change adds is already a row of the
    body (of Section A for an Amendment 1 page), at the number the amended text gives it: the change records itself on
    that row and adds none, an amended rule having one home (DESIGN-spec-conformance-review.md section 10.2).
    The paragraph and the rule a change names are read from its DIRECTIVE, the text before the new text it carries
    (`<br><br>`): the new text is the standard's own words, and a "(See 2.9.4, rule 2, ...)" inside an item it adds to an
    appendix list (X3.23a-1989 XVII-93) names no rule the change touches."""
    pages = [(m["folio"], bool(m["amd"])) for m in CHANGE_PAGE.finditer(cell)]
    directive = change.split("<br><br>", 1)[0]
    para = CHANGE_PARAGRAPH.search(directive)
    if (added := CHANGE_ADDS.search(directive)) is not None:
        if para is None:   # an added rule with no paragraph has no block to find it in: never read as a change to rule n
            cat.unresolved.append((doc, cell, change[:100]))
            return
        kind = KINDS.get(canonical_title(f"{added['kind']} rule"))
        folio, in_amendment = pages[0]
        sec = "A" if in_amendment else folio.split("-")[0]
        home = AMENDMENT_1 if in_amendment else BASE
        found = [r for r in cat.rules if r["document"] == home and r["section"] == sec and r["kind"] == kind
                 and r["ordinal"] == int(added["n"]) and r["sublist"] == 1
                 and para["num"] in (r["paragraph"], r["paragraph"].rsplit(".", 1)[0])]
        if len(found) == 1:
            if doc not in found[0]["amended_by"]:
                found[0]["amended_by"].append(doc)
        else:
            cat.unresolved.append((doc, cell, change[:100]))
        return
    rule = CHANGE_RULE.search(directive)
    if rule is None:
        if CHANGE_FORMAT.search(re.sub(r'"[^"]*"', "", directive)) is not None:
            record_format_change(cat, doc, cell, change, pages, para)
        return
    kind = KINDS.get(canonical_title(f"{rule['kind']} rule")) if rule["kind"] else None
    n = int(rule["n"])
    found = []
    for folio, in_amendment in pages:
        sec = "A" if in_amendment else folio.split("-")[0]
        for r in cat.rules:
            if r["document"] not in (BASE, AMENDMENT_1):
                continue   # a rule an amendment added is not the base rule a later change names
            if r["section"] != sec or r["ordinal"] != n or (kind and r["kind"] != kind):
                continue
            if para is not None:
                if para["num"] not in (r["paragraph"], r["paragraph"].rsplit(".", 1)[0]):
                    continue   # a change names the rule's block ("Paragraph 2.23.3") or the paragraph above it ("5.8")
            elif r["folio"] != folio:
                continue
            found.append(r)
    if len(found) == 1:
        if doc not in found[0]["amended_by"]:
            found[0]["amended_by"].append(doc)
    else:
        cat.unresolved.append((doc, cell, change[:100]))


def record_format_change(cat: Catalog, doc: str, cell: str, change: str, pages: list[tuple[str, bool]],
                         para: re.Match | None) -> None:
    """Record amendment `doc` on the general format (the FMT row) one change row names ("Paragraph 4.3.8.2.2, replace the
    general format ...", "Paragraph 6.16.2, general format of the IF statement, delete ...", "... in format 3." on a
    page cell's folio): a format is a row of the catalog as a rule is, so a change to it is recorded the same way, and
    a change that could name more than one row is reported (kb/Work PB5045). A change to a format that is no row names
    nothing the catalog holds and is left alone, as a heading is: the catalog harvests a format from its "General
    Format" paragraph only, so Section V's skeleton (V-15, V-24) and the unheaded format of a section's paragraph 2.1
    (X3.23a-1989 VII-6) have none."""
    found = []
    for folio, in_amendment in pages:
        sec = "A" if in_amendment else folio.split("-")[0]
        for r in cat.rules:
            if r["kind"] != "FMT" or r["document"] not in (BASE, AMENDMENT_1) or r["section"] != sec:
                continue
            if para is not None:
                if para["num"] not in (r["paragraph"], r["paragraph"].rsplit(".", 1)[0]):
                    continue
            elif r["folio"] != folio:
                continue
            found.append(r)
    if len(found) == 1:
        if doc not in found[0]["amended_by"]:
            found[0]["amended_by"].append(doc)
    elif found:
        cat.unresolved.append((doc, cell, change[:100]))


def summary(cat: Catalog) -> list[str]:
    """The counts the audit plans from: rules per module and level, and per kind; then every finding."""
    out = [f"RULES EXTRACTED: {len(cat.rules)} from {len(cat.blocks)} block(s)"]
    by_kind = collections.Counter(r["kind"] for r in cat.rules)
    out.append("   by kind: " + ", ".join(f"{k} {by_kind[k]}" for k in sorted(by_kind)))
    per: dict[str, collections.Counter] = collections.defaultdict(collections.Counter)
    for r in cat.rules:
        per[r["module"] or f"(Section {r['section']}, no module)"][r["level"]] += 1
    out.append("   by module and level (level 2 = the whole rule boxed; 'part' rows are level 1 with a boxed span):")
    for module in sorted(per, key=lambda m: (m.startswith("("), m)):
        c = per[module]
        parts = sum(1 for r in cat.rules if (r["module"] or f"(Section {r['section']}, no module)") == module
                    and r["box"] == "part")
        out.append(f"      {module:<34s} level 1 {c[1]:5d}   level 2 {c[2]:5d}   no level {c[None]:5d}   part-boxed {parts:4d}")
    amended = collections.Counter(d for r in cat.rules for d in r["amended_by"])
    out.append(f"   amendment change rows read {cat.changes}; rules amended: "
               + (", ".join(f"{d} {amended[d]}" for d in sorted(amended)) or "none"))
    for label, items in (("recognised block(s) with no rule", cat.empty),
                         ("rule-shaped heading(s) no kind resolves (their rules are not harvested)", cat.unrecognised),
                         ("ordinal(s) that neither continue nor restart their list", cat.unexplained),
                         ("box(es) inside a module that has one level", cat.one_level_boxes),
                         ("amendment change(s) naming a rule this catalog cannot find", cat.unresolved),
                         ("amendment change row(s) transcribed in neither row shape (CHANGE_UNREAD): no amendment "
                          "is recorded for them", cat.unread),
                         ("adjudication(s) the transcription no longer supports", cat.stale),
                         ("DUPLICATE id(s): the inventory key is not unique (a paragraph transcribed twice, or a "
                          "list the ordinals above broke)", cat.duplicates()),
                         ("CITATION(S) more than one row carries (a citation must name one rule: "
                          "citation.rule_designator)", cat.shared_citations()),
                         ("rule(s) of an admitted paragraph its manifest kind map does not name (citation, sub-list, "
                          "ordinal)", cat.untyped),
                         ("row(s) whose citation names no region of the transcription", cat.uncitable),
                         ("disagreement(s) of the standing lists' data with the text (element_standing.findings)",
                          cat.standing),
                         ("refusal(s) of the dispositions of the units of unnumbered text (text_units.audit)",
                          cat.dispositions),
                         ("paragraph(s) after a rule the margin-level data does not hold (measure_margin_levels)",
                          cat.margin_levels)):
        if items:
            out.append(f"⛔ {len(items)} {label}:")
            out.extend(f"      {i}" for i in items[:12])
            if len(items) > 12:
                out.append(f"      ... and {len(items) - 12} more")
    out.extend(element_standing.tally(cat.rules))
    provisional = collections.Counter(r["provisional"] for r in cat.rules if r["provisional"] != "none")
    if provisional:
        out.append(f"   PROVISIONAL rules (text from a reconstruction of a printed page the scan lacks, kb/Work PB4952): "
                   + ", ".join(f"{k} {provisional[k]}" for k in sorted(provisional)) + "; "
                   + ", ".join(r["id"] for r in cat.rules if r["provisional"] != "none"))
    dispositions = collections.Counter(v.get("disposition") for v in cat.declared.values())
    admitted = {k for k, v in cat.declared.items() if v.get("disposition") == "rules"}
    out.append(f"   content-keyed scan manifest: {dispositions['rules']} paragraph(s) admitted as rules "
               f"({sum(1 for r in cat.rules if r['citation'].split(' ')[0] in admitted)} rows), "
               f"{dispositions['not-rules']} recorded as not rules")
    out.extend(cat.drift()[0])
    out.append(f"   units of unnumbered text awaiting a disposition (kb/Work PB5449): {cat.pending_units}; "
               f"rows the units disposed a row kind make (kb/Work PB5452): {cat.minted}")
    out.append("   paragraphs after a rule, by printed level (scripts/spec/data/margin-levels-1985.json): "
               + ", ".join(f"{k} {v}" for k, v in cat.margin_tally.items()))
    out.append("✓ no findings" if not cat.findings() else f"FINDINGS: {cat.findings()}")
    return out


#: The 1985 normative region whose unnumbered text holds units (decision PB5448): Sections IV and VI to XVI, and
#: Amendment 1's Section A. Section II is explanatory, the counterpart of 2023's informative Annex D ("In this section
#: each of these features is discussed", §1985:II.1); I, III and V are the introduction, the glossary and the
#: composite language skeleton, and XVII the appendices.
NORMATIVE_SECTIONS = ("IV", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI")


def unit_kinds(clause: str) -> set[str]:
    """The row kinds a unit of the 1985 paragraph `clause` (its citation) may take: SR and GR (§1985:IV.3.1, IV.3.2), and
    AR and RV in Amendment 1's Intrinsic Function module, whose rule blocks are "Arguments" and "Returned Values"."""
    return {"SR", "GR"} | ({"AR", "RV"} if clause.startswith("§1989:A.") else set())


def unit_rule_id(kind: str, clause: str, label: str) -> str:
    """The row id of the unit `label` of the 1985 paragraph `clause` (its citation, `§1985:VI.5.13.1`) disposed as `kind`:
    `<year>-<kind>-<locator>-<label>` (decision PB5448 point 2: `1985-GR-VI.5.13.1-p3`), `add_row`'s form."""
    year, _, loc = clause.removeprefix("§").partition(":")
    return f"{year}-{kind}-{loc}-{label}"


#: The sides of a level-2 box at either end of a line (`unboxed`).
BOX_EDGE = re.compile(rf"^\s*{BOX_SIDE}\s*|\s*{BOX_SIDE}\s*$")
#: A label inside a rule's text: a band naming formats ("<u>FORMATS 1 AND 3</u>:") or a figure's or table's caption
#: ("Figure 4: ...", "Table 1. ..."). It is no paragraph of the clause: it is the rule's (`_rules_of`), as a table the
#: rule's text holds is, in either markup (text_units.HTML_TABLE_OPEN for the sixteen the 1985 text holds in HTML).
FORMAT_BAND = r"^\s*(?:<u>)?FORMATS?\s+\d+(?:\s*(?:,|AND)\s*\d+)*(?:</u>)?:?(?:</u>)?\s*$"
RULE_LABEL = re.compile(rf"{FORMAT_BAND}|^\s*(?:<u>)?(?:Figure|Table)\s+\d+[.:]", re.I)
#: A band naming a format on a line of its own ("<u>Format 1</u>:", page 182) is part of the general-format display it
#: labels, in prose as in a rule's text: no unit (kb/Work PB5452, reader 85u-05's repair records).
FORMAT_LABEL = re.compile(FORMAT_BAND, re.I)


@dataclasses.dataclass(frozen=True)
class BlockParagraph:
    """A paragraph of a 1985 rule block that is no numbered rule (kb/Work PB5449 item 3): a LEAD-IN, before the block's
    first `(n)` (`follows` empty), or a paragraph after a rule, inside the text the catalog gives that rule (`follows`
    names it: `(9)`, or `L2.(3)` in a restarted list). `order` keys it among the clause's paragraphs after a rule whose
    digest is equal, as a unit is keyed (text_units.keyed); `page` is the PDF page its first line is on."""
    clause: str
    follows: str
    unit: text_units.Unit
    order: int
    page: int

    @property
    def key(self) -> tuple[str, str, int]:
        """The key the margin-level data records it under: its clause, its text's digest and its order."""
        return self.clause, self.unit.digest, self.order


def _bodies(lines: list[str], rows: list[dict], whole: frozenset[str] = frozenset()
            ) -> dict[str, list[tuple[int, str]]]:
    """The body of every paragraph region of the normative region but a general format's, and of every clause of `whole`
    wherever it lies (a clause its content-keyed block decides unit by unit, `text_units.UNITS`; kb/Work PB5452), by
    citation, in the order of the text, with the 1985 page furniture removed (`furniture`, the reading `parse` makes: a page heading, a page the scan prints
    twice, an editorial note, a folio and the running header that opens a printed page), every heading and every format
    band (`FORMAT_LABEL`). A paragraph headed "Examples" is text like any other (§1985:VI.6.18.5: its prose and its
    result tables are units, its code is a display; kb/Work PB6001): an example is disposed on its own words, never
    kept out of the units unrecorded. A blank line stays, as the segmentation's separator."""
    formats = {r["citation"].split(" ")[0] for r in rows if r["kind"] == "FMT"}
    regions = [(f"§1985:{loc}", s, e) for loc, _t, s, e in citation.regions_1985(lines)
               if (loc.split(".", 1)[0] in NORMATIVE_SECTIONS and "." in loc) or f"§1985:{loc}" in whole]
    regions += [(f"§1989:{loc}", s, e) for loc, _t, s, e in citation.amendment_regions(lines, 1989)
                if loc.startswith("A.")]
    bodies: dict[str, list[tuple[int, str]]] = {}
    furn = furniture(lines)
    for clause, start, end in regions:
        if clause in formats:
            continue
        body = bodies.setdefault(clause, [])
        for at in range(start + 1, end):
            line = lines[at]
            if furn[at] is not None or ANY_HEADING.match(line) or FORMAT_LABEL.match(line):
                continue
            body.append((at + 1, unboxed(line) if line.strip() else line))
    return bodies


def unboxed(line: str) -> str:
    """`line` without the sides of a level-2 box (BOX_SIDE): a boxed line is segmented as the line it boxes, so a boxed
    lettered item is a lettered item. A unit's box is read from the transcription's lines, never from its text."""
    return BOX_EDGE.sub("", line).replace(BOX_SIDE, " ")


def _rules_of(body: list[tuple[int, str]]) -> tuple[list[tuple[int, str]], list[tuple[str, list[tuple[int, str]]]]]:
    """A rule block's body cut at its `(n)` lines: the lead-in's lines, and each rule's (`follows`, its lines, the
    first with its ordinal removed). A printed band (GROUP_LABEL, "FORMAT 2") between rules is no text, as `parse`
    reads it."""
    lead: list[tuple[int, str]] = []
    rules: list[tuple[str, list[tuple[int, str]]]] = []
    sublist, last = 1, 0
    in_table = False
    for no, line in body:
        if (m := ORDINAL.match(line)) is not None:
            n = int(m.group("n"))
            if n == 1 and last:
                sublist += 1
            last = n
            rules.append((f"({n})" if sublist == 1 else f"L{sublist}.({n})", [(no, m.group("box") + m.group("text"))]))
            in_table = False
        elif not rules:
            lead.append((no, line))
        elif text_units.HTML_TABLE_OPEN.match(line) or in_table:
            # A table a rule's text holds is the rule's (text_units.after_rule), whatever its markup: no candidate.
            in_table = not text_units.HTML_TABLE_CLOSE.search(line)
        elif GROUP_LABEL.match(line) is None and RULE_LABEL.match(line) is None:
            rules[-1][1].append((no, line))
    return lead, rules


def block_paragraphs(lines: list[str], rows: list[dict]) -> tuple[list[BlockParagraph], list[BlockParagraph]]:
    """THE PARAGRAPHS OF THE 1985 RULE BLOCKS THAT ARE NO NUMBERED RULE (kb/Work PB5449 item 3, decision PB5448 point 4):
    every lead-in paragraph of a block (the margin-level measurement's calibration: a lead-in prints at the block's
    margin by definition) and every paragraph the text of a numbered rule holds after the rule's own
    (text_units.after_rule), each with its key and page. The second are the candidates whose printed level the 1985
    margin-level data records (`measure_margin_levels.py`): level 0 is a margin paragraph of the clause, deeper a
    continuation of the rule."""
    numbered = {r["citation"].split(" ")[0] for r in rows if r["kind"] != "FMT" and "shape" not in r}
    page_of = pages_of(lines)
    leads: list[BlockParagraph] = []
    after: list[BlockParagraph] = []
    for clause, body in _bodies(lines, rows).items():
        if clause not in numbered:
            continue
        lead, rules = _rules_of(body)
        leads += [BlockParagraph(clause, "", u, order, page_of[u.line])
                  for u, order in text_units.keyed([u for u in text_units.segment(lead, held=ORDINAL)
                                                    if u.shape == "paragraph"])]
        found = [(follows, u) for follows, rule in rules for u in text_units.after_rule(rule)]
        after += [BlockParagraph(clause, follows, u, order, page_of[u.line])
                  for (follows, _), (u, order) in zip(found, text_units.keyed([u for _, u in found]))]
    return leads, after


def units(lines: list[str], rows: list[dict], declared: dict[str, dict], margin: list[BlockParagraph]) -> dict[str, list]:
    """THE UNITS OF 1985'S UNNUMBERED TEXT (kb/Work PB5449, decision PB5448 point 6), by paragraph citation
    (`§1985:VI.5.13.1`, `§1989:A.2.1`: the key the content-keyed manifest's `blocks` already use), in the order of the
    text: the ISO arm's segmentation (text_units.segment) over every paragraph region of the normative region
    (citation.regions_1985, the reader cite.py searches), its furniture removed (`_bodies`).

    ⛔ WHAT LIES IN A ROW'S TEXT IS NO UNIT, as in the ISO arm, with ONE exception: a general format's paragraph is its FMT
    row; a paragraph whose numbered list the manifest records `not-rules` contributes its text but not the list's items;
    a table a row of the paragraph names is that row's. A rule block contributes its LEAD-IN, the text before its first
    `(n)`, and every paragraph after a rule that the 1985 PDF prints at the block's margin: `margin` holds those (the
    paragraphs of `block_paragraphs` the margin-level data measures at level 0, `measure_margin_levels.at_margin`), each
    a unit numbered on after the lead-in's paragraphs (decision PB5448 point 2: `n` counts every margin paragraph of the clause). While its
    disposition is pending it ALSO stays in the rule's text, so the rule's words never leave the denominator undecided
    (design point 4); a decided one leaves the rule when the 1985 rows are minted (kb/Work PB5452)."""
    numbered = {r["citation"].split(" ")[0] for r in rows if r["kind"] != "FMT" and "shape" not in r}
    named: dict[str, set[int]] = collections.defaultdict(set)
    for r in (r for r in rows if "shape" not in r):
        named[r["citation"].split(" ")[0]].update(text_units.tables_named(r["text"]))
    glued: dict[str, list[text_units.Unit]] = collections.defaultdict(list)
    for p in margin:
        glued[p.clause].append(p.unit)
    out: dict[str, list] = {}
    whole = frozenset(c for c, info in declared.items() if info.get("disposition") == text_units.UNITS)
    for clause, body in _bodies(lines, rows, whole).items():
        lead = _rules_of(body)[0] if clause in numbered else body
        # A list is held by its marker (kb/Work PB6001): a rule block's and a declared block's lists are the `(n)` lists
        # the content-keyed scan measures (ORDINAL); a clause decided unit by unit holds none, nor does a prose clause.
        prose = clause in whole or (clause not in numbered and clause not in declared)
        found = [u for u in text_units.segment(lead, held=None if prose else ORDINAL)
                 if not (u.table and u.table in named[clause])]
        p = sum(u.shape == "paragraph" for u in found)
        found += [dataclasses.replace(u, label=f"p{p + i}") for i, u in enumerate(glued[clause], start=1)]
        if found:
            out[clause] = found
    return out


# ── THE MINT (kb/Work PB5452, decision PB5448 points 2 and 4): a unit disposed a row kind is a row of the catalog ───────


def _decided(blocks: dict[str, dict]) -> dict[str, dict[tuple[str, int], dict]]:
    """The disposition entries of a `paragraph-blocks` map by clause and key (digest, order)."""
    return {clause: {(e.get("digest"), e.get("order", 0)): e for e in es}
            for clause, es in text_units.entries_of(blocks).items()}


def _spans(lines: list[str], cat: Catalog, units: dict[str, list], after: list[BlockParagraph]
           ) -> dict[tuple[str, int], tuple[int, ...]]:
    """Where each unit's text lies in the transcription, by (clause, its first line): the 1-based lines from its first
    up to the next thing the clause prints that is no part of it (another unit, a numbered rule, another paragraph after
    a rule) or the clause's end, less every display among them (a table unit's are its own lines). A margin paragraph's span is what leaves its rule's text (`carved_lines`); a unit's span is
    where its `box` is read (its text is segmented without the box sides, `unboxed`)."""
    ends = {f"§1985:{loc}": e for loc, _t, _s, e in citation.regions_1985(lines)}
    ends |= {f"§1989:{loc}": e for loc, _t, _s, e in citation.amendment_regions(lines, 1989)}
    starts: dict[str, set[int]] = collections.defaultdict(set)
    for r in cat.rules:
        if r["id"] in cat.row_line:
            starts[r["citation"].split(" ")[0]].add(cat.row_line[r["id"]])
    for p in after:
        starts[p.clause].add(p.unit.line)
    for clause, us in units.items():
        starts[clause].update(u.line for u in us)
    out = {}
    for clause, us in units.items():
        marks = sorted(starts[clause])
        for u in us:
            end = min(next((m for m in marks if m > u.line), ends[clause] + 1), ends[clause] + 1)
            if u.shape in ("table", "table-row"):
                out[clause, u.line] = tuple(range(u.line, end))
                continue
            # A display among a paragraph's lines (a <pre> figure or code, a fence, a table) is no unit text
            # (text_units.segment): it stays where it was, so §1985:VI.6.3.4's figures stay in rule (2)'s text when
            # the paragraph naming them leaves it, and §1985:VI.6.4.6's code in rule (2)'s.
            own = []
            shown = text_units.display_mask(lines[u.line - 1:end - 1])
            for i, display in zip(range(u.line, end), shown):
                line = lines[i - 1]
                if not display and not text_units.TABLE_LINE.match(line) and not FORMAT_LABEL.match(line):
                    own.append(i)   # (a format band labels the display after it, and stays with it)
            out[clause, u.line] = tuple(own)
    return out


def carved_lines(lines: list[str], cat: Catalog, units: dict[str, list], blocks: dict[str, dict],
                 after: list[BlockParagraph], margin: list[BlockParagraph]) -> frozenset[int]:
    """The 1-based lines of every DECIDED margin paragraph (a unit of `margin`, the paragraphs after a rule printed at
    the block's margin, whose disposition is not pending): they leave the text of the rule they follow (decision PB5448
    point 4; kb/Work PB5452), so `parse` reads the rule without them. A pending one stays in its rule's text."""
    decided = _decided(blocks)
    spans = _spans(lines, cat, units, after)
    glued = {(p.clause, p.unit.line) for p in margin}
    out: set[int] = set()
    for clause, us in units.items():
        for u, order in text_units.keyed(us):
            e = decided.get(clause, {}).get((u.digest, order))
            if (clause, u.line) in glued and e is not None and e.get("disposition") != text_units.PENDING:
                out.update(spans[clause, u.line])
    return frozenset(out)


def _box(lines: list[str], span: tuple[int, ...], furn: list[str | None]) -> str:
    """How much of a unit the level-2 box covers (none, part, whole), read on its printed lines (`_spans`)."""
    printed = [lines[i - 1] for i in span if lines[i - 1].strip() and furn[i - 1] is None]
    boxed = sum(BOX_SIDE in ln for ln in printed)
    return "none" if not boxed else "whole" if boxed == len(printed) else "part"


def mint(cat: Catalog, lines: list[str], units: dict[str, list], blocks: dict[str, dict],
         after: list[BlockParagraph]) -> int:
    """THE ROWS OF THE UNITS OF UNNUMBERED TEXT (kb/Work PB5452): every unit of `units` whose disposition in `blocks` is
    a row kind becomes a row of `cat`, placed among its clause's rows in the order of the text (cite.py anchors a
    clause's rows in that order), and returns how many. Its id is `<year>-<kind>-<locator>-<label>` (`unit_rule_id`:
    `-p<n>`, `-T<t>.<r>`, `-T<t>`, an item `-<n>` or `-L<m>.<n>`), its citation the unit's designator (`p3`, `Table 1
    row 2`), an item's the numbered rule's (`GR1`, `GRL2.1`: the id an item mints is the id a numbered rule of that
    designator has, `citation.rule_designator`); `shape` the unit's; `box` read on its printed lines and `level` as a
    numbered rule's (`add_row`); `definitional` when the disposition says the row obliges nothing (closed through the
    `definitional` arm, kb/Work PB5453). Every row a list holds, numbered rule or item, then takes the words of the
    colon unit that leads its list as its `lead` (outside `text`, so no digest moves; design point 1)."""
    from cite import unit_designator  # noqa: PLC0415
    decided = _decided(blocks)
    spans = _spans(lines, cat, units, after)
    furn, page_of, below = furniture(lines), pages_of(lines), folios_below(lines)
    recon = reconstructed(lines)
    minted: list[tuple[int, dict]] = []
    for clause, us in units.items():
        year, _, loc = clause.removeprefix("§").partition(":")
        section, _, paragraph = loc.partition(".")
        parent = cat.paragraphs.get(loc.rsplit(".", 1)[0])
        module, levels = MODULES.get(section, (None, 0))
        for u, order in text_units.keyed(us):
            e = decided.get(clause, {}).get((u.digest, order))
            if e is None or (kind := e.get("disposition")) not in text_units.ROW_KINDS:
                continue
            span = spans[clause, u.line]
            box = _box(lines, span, furn)
            designator = (citation.rule_designator(kind, u.ordinal, u.sublist) if u.shape == "item"
                          else unit_designator(u.label))
            row = {"id": unit_rule_id(kind, clause, u.label), "citation": f"{clause} {designator}",
                   "document": AMENDMENT_1 if year == "1989" else BASE, "section": section, "paragraph": paragraph,
                   "locator": loc, "module": module, "subject": parent.title if parent else "", "kind": kind,
                   "ordinal": u.ordinal, "sublist": u.sublist, "box": box,
                   "level": None if module is None else (2 if box == "whole" and levels == 2 else 1),
                   "amended_by": [], "status": element_standing.NORMAL, "page": page_of[u.line],
                   "folio": recon[u.line - 1] or below[u.line - 1], "band": "", "text": u.text,
                   "provisional": PROVISIONAL[frozenset(recon[i - 1] is not None for i in span
                                                        if lines[i - 1].strip())], "shape": u.shape}
            if e.get("definitional"):
                row["definitional"] = True
            minted.append((u.line, row))
    # In the order of the text: a minted row goes before the first row of the catalog that opens after it.
    at_line = {r["id"]: cat.row_line.get(r["id"]) for r in cat.rules}
    merged: list[dict] = []
    pending = sorted(minted, key=lambda m: m[0])
    for r in cat.rules:
        line = at_line[r["id"]]
        while pending and line is not None and pending[0][0] < line:
            merged.append(pending.pop(0)[1])
        merged.append(r)
    merged += [row for _, row in pending]
    for line, row in minted:
        cat.row_line[row["id"]] = line
    cat.rules[:] = merged
    _leads(cat, units)
    return len(minted)


def _leads(cat: Catalog, units: dict[str, list]) -> None:
    """Give every row of a list (a block's numbered rules of one sub-list, or a prose clause's items of one list) the
    words of the colon unit that leads it, as `lead` (design point 1: "the colon unit's words before the list are also
    each item's `lead` field"): the clause's paragraph unit printed last before the list's first row, with no row of
    another list between, when it ends in a colon."""
    by_list: dict[tuple[str, int], list[dict]] = collections.defaultdict(list)
    for r in cat.rules:
        if r["kind"] != "FMT" and r.get("shape") in (None, "item"):
            by_list[r["citation"].split(" ")[0], r["sublist"]].append(r)
    for (clause, _sub), rows in by_list.items():
        first = min(cat.row_line[r["id"]] for r in rows)
        before = [r for r in cat.rules if r["citation"].split(" ")[0] == clause and r["kind"] != "FMT"
                  and r.get("shape") in (None, "item") and cat.row_line[r["id"]] < first]
        floor = max((cat.row_line[r["id"]] for r in before), default=0)
        lead = max((u for u in units.get(clause, []) if u.shape == "paragraph" and floor < u.line < first),
                   key=lambda u: u.line, default=None)
        if lead is not None and text_units.LEADS.search(lead.text):
            for r in rows:
                r["lead"] = lead.text


def cite_check(cat: Catalog) -> None:
    """Record every row whose citation names no region of the real transcription (citation.validate, the reader cite.py
    --check uses): a row is cited so that its text can be checked, and one cite.py refuses cannot be (kb/Work PB4886)."""
    cat.uncitable = [f"{r['id']}: {why}" for r in cat.rules
                     if (why := citation.validate(citation.parse(r["citation"]))) is not None]




# ── SELF-TEST ────────────────────────────────────────────────────────────────────────────────────────────────────────

PLANTED = """# SECTION IV: OVERALL LANGUAGE CONSIDERATION

## Page 149

## 3. <u>RULES</u>

### 3.1 SYNTAX RULES

Syntax rules are those rules that define the order of the elements.

# SECTION VI: NUCLEUS MODULE

## Page 257

Nucleus - Level-Number

5.7 LEVEL-NUMBER

5.7.2 General Format

<pre style="line-height:1">
level-number
</pre>

5.7.3 Syntax Rules

(1) A planted first syntax rule.

> ⚠ **CORRECTED — see the Errata (C1).** The standard prints `A planted frist syntax rule.` (page 257): a planted
> misprint, its note continued on a second line.

(2) A planted second rule with items:

a. its first item, ║boxed in part║, and

b. its second item.

VI-25

## Page 258

Nucleus - Level-Number

continued on the next page.

(3) A planted rule printed below its row:

a. its item.

### ║5.7.4 General Rules║

║(1) A planted rule boxed whole.║

FORMAT 2

(2) A planted rule under a band.

(3) A planted added rule.

## 5.8 THE OCCURS CLAUSE

### 5.8.2 General Format

<pre style="line-height:1">
╔ <u>OCCURS</u> integer-1 ╗
╚ [ INDEXED ]          ╝
</pre>

### 5.8.3 Syntax Rules

(1) The first.

(3) A gap in the list.

### 5.8.4 Fit Test Rules

(1) A rule-shaped block no kind resolves.

### 5.8.5 General Rules

VI-26

## Page 259

<!-- PDF page 259 repeats PDF page 258 (folio VI-26): planted. -->

### ║5.7.4 General Rules║

║(1) A planted rule boxed whole.║

## Page 260

### 5.9.5 Editing Rules

(1) A planted editing rule.

### 5.9.6 Precedence Rules

The chart shows the order of precedence, planted.

### ║6.2.3 Formation and Evaluation Rules║

║(1) A planted evaluation rule.║

║(2) A second.║

║(3) A planted formation rule.║

### 6.4.2 The ON SIZE ERROR Phrase

The size error condition occurs under the following circumstances, planted:

(1) Division by zero.

(2) An overflow.

### 6.4.3 The CORRESPONDING Phrase

(1) A planted correspondence rule.

(2) A rule its manifest map does not name.

VI-27

# SECTION XI: SORT-MERGE MODULE

## Page 300

### 4.3.3 Syntax Rules

║(1) A box in a one-level module.║

XI-3

# SECTION XIII: REPORT WRITER MODULE

## Page 633

#### 3.19.4 General Rules

(1) A planted SUM rule.

a. Its printed item.

XIII-53

## Page 634

> ⚠ **RECONSTRUCTED — not printed in our copy (folio XIII-54).** Planted: the rest of the SUM rules.

b. Its reconstructed item.

(2) A planted reconstructed SUM rule.

> ⚠ **END RECONSTRUCTED (folio XIII-54).** Planted.

> ⚠ **RECONSTRUCTED — not printed in our copy (folio XIII-55).** Planted: the TYPE clause's heading
> and its first syntax rules.

### 3.20 THE TYPE CLAUSE

#### 3.20.2 General Format

<pre>
<u>TYPE</u> IS DETAIL
</pre>

#### 3.20.3 Syntax Rules

(1) A planted reconstructed TYPE rule.

> ⚠ **END RECONSTRUCTED (folio XIII-55).** Planted.

Report Writer - TYPE

(2) A planted printed rule whose heading is reconstructed.

### 3.20.4 General Rules

(1) A planted TYPE rule.

XIII-56

# SECTION XVII: APPENDICES

## Page 790

### APPENDIX C: LANGUAGE ELEMENT LISTS

## 2. <u>IMPLEMENTOR-DEFINED LANGUAGE ELEMENT LIST</u>

(1) A planted list item. (See 4.2.2.1.2 on page IV-8.)

(2) A second.

### 3.1 Syntax Rules

(1) A planted heading in an appendix, which carries no rule block.

XVII-87

# SECTION XVIII: INDEX

## Page 400

(1) not a rule: the Index carries none.

# AMENDMENT 1: INTRINSIC FUNCTION MODULE (ANSI X3.23a-1989)

## Page 831

| Page No. | Description of Change |
|---|---|
| VI-25 | Paragraph 5.7 entitled *Level-Number*, syntax rule 2, change to read: planted. |
| XVII-93 | Add the following after item 80:<br><br>(81) Planted. (See 5.7.3, rule 1, on page VI-25.) |

## Page 832

**VI-25** Paragraph 5.7.3, rule (1), change to read: planted.

# SECTION A: INTRINSIC FUNCTION MODULE

## Page 863

### 2.5.3 Arguments

(1) Argument-1 is planted.

### 2.5.4 Returned Values

(1) The planted value.

A-33

# AMENDMENT 2: CORRECTION AMENDMENT (ANSI X3.23b-1993)

## Page 913

| Page No. | Description of Change |
|---|---|
| VI-26 | General rule 7 of nothing on this page, planted. |
| VI-25 | Paragraph 5.7.4 of the level-number, add the following as the new general rule 3:<br><br>(3) A planted added rule. |
| VI-25 | Paragraph 5.7.4 of the level-number, add the following as the new general rule 9:<br><br>(9) Planted, never applied in place. |
| (A-33) | Paragraph 2.5.3 of the ACOS function, argument rule 1, change to read: planted. |
| x | Add an entry to the table of contents, planted. |
| VI-25 | Paragraph 5.7.3 of the level-number, add the following as new syntax rule 3: |

(3) A planted rule printed below its row:

a. its item.

VI-26 — Paragraph 5.7.4 of the level-number, general rule 1, change to read: planted, a row in neither shape.

**VI-26** New paragraph 5.8.2, replace the general format with the following:

**VI-25 thru VI-26** Renumber paragraphs 5.7 thru 5.8, planted.

B-1
"""


#: The planted content-keyed scan manifest: two admitted paragraphs (one whose map misses a rule), one the text lacks,
#: and one whose ordinal count moved; PLANTED's VI.6.4.2 is measured and undeclared.
PLANTED_MANIFEST = {
    "§1985:VI.6.2.3": {"title": "Formation and Evaluation Rules", "ordinals": 3, "lists": 1, "longest": 3,
                       "disposition": "rules", "kind": {"items": {"1": "GR", "2": "GR", "3": "SR"}}, "why": "planted"},
    "§1985:VI.6.4.3": {"title": "The CORRESPONDING Phrase", "ordinals": 2, "lists": 1, "longest": 2,
                       "disposition": "rules", "kind": {"items": {"1": "GR"}}, "why": "planted"},
    "§1985:IV.4.2.1": {"title": "Separators", "ordinals": 9, "lists": 1, "longest": 9, "disposition": "rules",
                       "kind": "SR", "why": "planted: declared, and not in the planted text"},
    "§1985:C.2": {"title": "IMPLEMENTOR-DEFINED LANGUAGE ELEMENT LIST", "ordinals": 5, "lists": 1, "longest": 5,
                  "disposition": "not-rules", "why": "planted: its count moved"},
}


def self_test() -> int:
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(f"  {'ok  ' if cond else 'FAIL'} {name}")
        if not cond:
            failures.append(name)

    cat = parse(PLANTED.splitlines(), PLANTED_MANIFEST)
    by = {r["id"]: r for r in cat.rules}
    check("a rule is a column-0 (n) of a Syntax Rules paragraph, unheaded or headed",
          "1985-SR-VI.5.7.3-1" in by and "1985-GR-VI.5.7.4-1" in by)
    check("the row carries its citation, module and subject",
          by["1985-SR-VI.5.7.3-1"]["citation"] == "§1985:VI.5.7.3 SR1" and by["1985-SR-VI.5.7.3-1"]["module"] == "Nucleus"
          and by["1985-SR-VI.5.7.3-1"]["subject"] == "LEVEL-NUMBER")
    check("a correction note under a rule is not its text, nor the next rule's (kb/Work PB4972)",
          by["1985-SR-VI.5.7.3-1"]["text"] == "A planted first syntax rule."
          and "CORRECTED" not in by["1985-SR-VI.5.7.3-2"]["text"])
    r2 = by["1985-SR-VI.5.7.3-2"]
    check("lettered items are the rule's text, and a page break's furniture is not",
          "its second item" in r2["text"] and "continued on the next page." in r2["text"]
          and "VI-25" not in r2["text"] and "Nucleus - Level-Number" not in r2["text"])
    check("a boxed span makes a part-boxed rule of level 1", r2["box"] == "part" and r2["level"] == 1)
    check("a rule boxed whole is level 2", by["1985-GR-VI.5.7.4-1"]["box"] == "whole" and by["1985-GR-VI.5.7.4-1"]["level"] == 2)
    # THE ONE FURNITURE READING (kb/Work PB5998): `parse` and `_bodies` read the same lines as furniture, so a running
    # header after a reconstruction, after a folio inside a page, or on a page a region opens with, is text of neither.
    planted = PLANTED.splitlines()
    body = [t for _, t in _bodies(planted, cat.rules).get("§1985:XIII.3.20.3", [])]
    check("a running header after a reconstruction is no paragraph text either (both readers: `furniture`)",
          "Report Writer - TYPE" not in body and "(2) A planted printed rule whose heading is reconstructed." in body)
    pages = ["## Page 611", "", "Report Writer - Page Heading", "", "(1) A rule.", "", "XIII-30", "",
             "Report Writer - Body Group", "", "More text.", "", "XIII-31", "", "## Page 612", "",
             "<!-- PDF page 612 repeats PDF page 611 (folio XIII-31) -->", "", "(1) A rule.", "", "## Page 613", "",
             "Report Writer - TYPE", "", "Text after the repeat."]
    furn = furniture(pages)
    check("the furniture: a page heading, its running header, a folio, the header after a folio inside a page, a "
          "repeated page, and the header of the page after it",
          [(t, furn[i]) for i, t in enumerate(pages) if furn[i] is not None] == [
              ("## Page 611", PAGE), ("Report Writer - Page Heading", HEADER), ("XIII-30", FOLIO),
              ("Report Writer - Body Group", HEADER), ("XIII-31", FOLIO), ("## Page 612", PAGE),
              (pages[16], REPEAT), ("", REPEAT), ("(1) A rule.", REPEAT), ("", REPEAT), ("## Page 613", PAGE),
              ("Report Writer - TYPE", HEADER)])
    check("a printed band scopes the rules after it and is not rule text",
          by["1985-GR-VI.5.7.4-2"]["band"] == "FORMAT 2" and "FORMAT 2" not in by["1985-GR-VI.5.7.4-1"]["text"])
    check("the page and folio a rule starts on", r2["page"] == 257 and r2["folio"] == "VI-25"
          and by["1985-GR-VI.5.7.4-1"]["folio"] == "VI-26")
    check("a general format is an FMT row, its double-line box read",
          by["1985-FMT-VI.5.7.2"]["box"] == "none" and by["1985-FMT-VI.5.8.2"]["box"] == "whole")
    check("an ordinal that neither continues nor restarts is reported", ("VI.5.8.3", 3, 1) in cat.unexplained)
    check("a rule-shaped heading no kind resolves is reported", ("VI.5.8.4", "Fit Test Rules") in cat.unrecognised)
    check("a recognised block with no rule is reported", "GR VI.5.8.5" in cat.empty)
    check("the paragraphs that DEFINE the rule kinds are outside the denominator, and no finding",
          not any(r["section"] == "IV" for r in cat.rules) and "SR IV.3.1" not in cat.empty
          and not any(loc.startswith("IV.") for loc, _ in cat.unrecognised))
    check("a box inside a one-level module is reported", cat.one_level_boxes == ["1985-SR-XI.4.3.3-1"]
          and by["1985-SR-XI.4.3.3-1"]["module"] == "Sort-Merge")
    check("the Index carries no rule", not any(r["section"] == "XVIII" for r in cat.rules))
    check("Section A is Amendment 1's module, cited §1989",
          by["1989-AR-A.2.5.3-1"]["document"] == AMENDMENT_1 and by["1989-RV-A.2.5.4-1"]["citation"] == "§1989:A.2.5.4 RV1"
          and by["1989-AR-A.2.5.3-1"]["module"] == "Intrinsic Function")
    check("an Addendum change names its rule by page, paragraph and rule", r2["amended_by"] == [AMENDMENT_1])
    check("an Amendment 2 change to an Amendment 1 page resolves in Section A",
          by["1989-AR-A.2.5.3-1"]["amended_by"] == [AMENDMENT_2])
    check("a change naming no rule is no finding, one naming an absent rule is, and so is one adding a rule the body "
          "lacks (a change not applied in place)",
          cat.changes == 10 and [cell for _d, cell, _c in cat.unresolved] == ["VI-26", "VI-25"]
          and "general rule 9" in cat.unresolved[1][2])
    check("a change row in neither row shape is a finding, never swallowed as the open row's new text (review R1-1, "
          "kb/Work PB5045)",
          len(cat.unread) == 1 and cat.unread[0].startswith("page 913: VI-26 — Paragraph 5.7.4")
          and by["1985-GR-VI.5.7.4-1"]["amended_by"] == [])
    check("a change to a general format records itself on the FMT row, its paragraph read from 'New paragraph'",
          by["1985-FMT-VI.5.8.2"]["amended_by"] == [AMENDMENT_2] and by["1985-FMT-VI.5.7.2"]["amended_by"] == [])
    check("a bold page cell may span pages ('VI-25 thru VI-26') and is read as one row",
          not any("Renumber" in u for u in cat.unread))
    check("a change row printed without the table's pipes is read, its rule named in parentheses",
          by["1985-SR-VI.5.7.3-1"]["amended_by"] == [AMENDMENT_1])
    check("a rule the new text of a change mentions is no rule the change names (XVII-93's '(See 5.7.3, rule 1')",
          not any(cell == "XVII-93" for _d, cell, _c in cat.unresolved))
    check("a page the scan prints twice yields no rule: no duplicate id from the repeat",
          by["1985-GR-VI.5.7.4-1"]["page"] == 258 and not cat.duplicates())
    check("Editing and Precedence Rules are typed by THE kind table (GR, and SR with no numbered rule: no finding)",
          by.get("1985-GR-VI.5.9.5-1", {}).get("text") == "A planted editing rule." and ("VI.5.9.6", "SR") in cat.blocks
          and "SR VI.5.9.6" not in cat.empty)
    check("a paragraph the manifest admits types each rule by its ordinal (its items map)",
          "1985-GR-VI.6.2.3-1" in by and "1985-GR-VI.6.2.3-2" in by
          and by.get("1985-SR-VI.6.2.3-3", {}).get("citation") == "§1985:VI.6.2.3 SR3"
          and by["1985-SR-VI.6.2.3-3"]["level"] == 2 and ("VI.6.2.3", MIXED) in cat.blocks)
    check("a rule of an admitted paragraph its map does not name is reported, never guessed",
          cat.untyped == [("§1985:VI.6.4.3", 1, 2)] and "1985-GR-VI.6.4.3-1" in by)
    drift, drifts = cat.drift()
    check("THE CONTENT-KEYED SCAN measures a prose paragraph's numbered list, and an undeclared one is a drift",
          cat.unharvested.get("§1985:VI.6.4.2", {}).get("ordinals") == 2
          and any(ln.startswith("      + §1985:VI.6.4.2 (2 ordinals)") for ln in drift)
          and not any(r["paragraph"] == "6.4.2" for r in cat.rules))
    check("a declared paragraph the text no longer has, and a moved ordinal count, are drifts (4 in all, with the"
          " unrecognised Fit Test Rules list and VI.6.4.2)",
          any(ln.startswith("      - §1985:IV.4.2.1 ") for ln in drift)
          and any(ln.startswith("      ~ §1985:C.2  5 → 2") for ln in drift) and drifts == 4)
    check("an appendix paragraph is measured under its appendix's locator, and no appendix heading opens a rule block",
          "§1985:C.2" in cat.unharvested and ("C.3.1", "Syntax Rules") in cat.unrecognised
          and not any(r["section"] in ("XVII", "C") for r in cat.rules))
    # kb/Work PB4952: a reconstruction is read as text, a rule continuing across it as across a page break, and every row
    # that takes text from it is marked provisional: part, whole, or none for the printed rule after it.
    sums = {r["id"]: r for r in cat.rules if r["paragraph"] == "3.19.4"}
    check("a rule continues into a reconstruction (part), and a rule inside one is whole, on the folio it reconstructs",
          list(sums) == ["1985-GR-XIII.3.19.4-1", "1985-GR-XIII.3.19.4-2"]
          and sums["1985-GR-XIII.3.19.4-1"]["text"].endswith("a. Its printed item.\nb. Its reconstructed item.")
          and (sums["1985-GR-XIII.3.19.4-1"]["provisional"], sums["1985-GR-XIII.3.19.4-1"]["folio"]) == ("part", "XIII-53")
          and (sums["1985-GR-XIII.3.19.4-2"]["provisional"], sums["1985-GR-XIII.3.19.4-2"]["folio"]) == ("whole", "XIII-54")
          and not any("Planted" in r["text"] for r in sums.values()))
    check("a reconstruction's heading opens its block: its format and rule are whole, and the printed rule after it is "
          "none, cited by the standard's locator, the running header skipped",
          (by["1985-FMT-XIII.3.20.2"]["provisional"], by["1985-SR-XIII.3.20.3-1"]["provisional"]) == ("whole", "whole")
          and by["1985-SR-XIII.3.20.3-1"]["folio"] == "XIII-55"
          and by["1985-SR-XIII.3.20.3-2"]["provisional"] == "none"
          and by["1985-SR-XIII.3.20.3-2"]["citation"] == "§1985:XIII.3.20.3 SR2"
          and by["1985-SR-XIII.3.20.3-2"]["text"] == "A planted printed rule whose heading is reconstructed."
          and by["1985-SR-XIII.3.20.3-1"]["text"] == "A planted reconstructed TYPE rule."
          and by["1985-GR-XIII.3.20.4-1"]["provisional"] == "none"
          and not any(loc.startswith("XIII") for loc, _n, _l in cat.unexplained))
    # kb/Work PB5789: printed pages our scan lacks, transcribed from another scan after a SOURCE note, stand inside the
    # `## Page` after the gap, each closed by its own folio: the note is no rule text, each running header is skipped,
    # and each rule is on the folio below it.
    sourced = {r["id"]: r for r in parse(
        "# SECTION XIII: REPORT WRITER MODULE\n\n## Page 611\n\n#### 3.10.6 General Rules\n\n(1) A planted printed rule.\n\n"
        "XIII-29\n\n## Page 612\n\n> ⚠ **SOURCE — folio XIII-30, not in our copy.** Planted: from another scan.\n\n"
        "Report Writer - Planted\n\n(2) A planted rule from another scan.\n\nXIII-30\n\nReport Writer - Planted\n\n"
        "(3) A planted printed rule.\n\nXIII-32\n".splitlines()).rules}
    check("a page from another scan inside a PDF page: the SOURCE note and every running header are no rule text, and "
          "each rule is on the folio below it",
          [(r["text"], r["folio"], r["provisional"]) for r in sourced.values()]
          == [("A planted printed rule.", "XIII-29", "none"), ("A planted rule from another scan.", "XIII-30", "none"),
              ("A planted printed rule.", "XIII-32", "none")])
    check("an adjudication the text no longer has is reported, one it has is not",
          any(s.startswith("XIII.3.10:") for s in cat.stale) and not any(s.startswith("IV.3:") for s in cat.stale))
    check("THE shared measure counts lists, ascending items and the longest run, and no stray ordinal",
          ordinal_shape(["(1) a", "(2) b", "x (9) not", "(1) c", "(7) stray", "(2) d", "(3) e"], ORDINAL)
          == {"ordinals": 5, "lists": 2, "longest": 3} and ordinal_shape(["prose"], ORDINAL) is None)
    nested = {"sublists": {"1": {"items": {"1": "GR", "3": "SR"}}, "2": "GR"}}
    check("a sub-list's kind may be an items map (rule_kind), and kinds_of names every kind a map gives",
          (rule_kind(nested, 1, 3), rule_kind(nested, 2, 9), rule_kind(nested, 1, 2), rule_kind(nested, 3, 1))
          == ("SR", "GR", None, None) and kinds_of(nested) == {"GR", "SR"})
    check("an added rule the table prints below its row names the body's rule, applied in place",
          by["1985-SR-VI.5.7.3-3"]["amended_by"] == [AMENDMENT_2]
          and by["1985-SR-VI.5.7.3-3"]["text"] == "A planted rule printed below its row:\na. its item.")
    check("every id is unique", not cat.duplicates())
    twin = Catalog(rules=[{"citation": "§1985:IV.4.4.2 GR1"}, {"citation": "§1985:IV.4.4.2 GR1"},
                          {"citation": f"§1985:IV.4.4.2 {citation.rule_designator('GR', 1, 2)}"}])
    check("no two rows share a citation, a sub-list's rule naming its list (kb/Work PB4969)",
          not cat.shared_citations() and twin.shared_citations() == ["§1985:IV.4.4.2 GR1"])
    added = by["1985-GR-VI.5.7.4-3"]
    check("a change that adds a rule records itself on the body's row, which the transcription applied in place: one "
          "home, no row of the amendment (design 10.2, kb/Work PB5045)",
          added["document"] == BASE and added["citation"] == "§1985:VI.5.7.4 GR3" and added["amended_by"] == [AMENDMENT_2]
          and added["text"] == "A planted added rule." and not any(r["document"] == AMENDMENT_2 for r in cat.rules))
    check("a box in a one-level module leaves its rule at level 1", by["1985-SR-XI.4.3.3-1"]["level"] == 1)
    check("the summary counts per module and level (the whole-boxed rule and the whole-boxed format are level 2)",
          any(ln.split()[:7] == ["Nucleus", "level", "1", "11", "level", "2", "5"] for ln in summary(cat)))
    print(f"SELF-TEST: {'PASS' if not failures else 'FAIL'}")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    sys.exit("usage: rule_catalog_1985.py --self-test   (the catalog itself: extract_rule_catalog.py --edition 1985)")
