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
carry the mark until the printed page's verbatim text replaces the reconstruction).

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
#: Rule-shaped paragraphs deliberately outside the denominator, each with its reason (the 2023 arm's EXCLUDED_BLOCKS).
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
#: Recognised blocks that rightly yield no numbered rule (the 2023 arm's KNOWN_EMPTY_BLOCKS).
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
class Catalog:
    """The rows, and every finding `--check` fails on."""
    rules: list[dict] = dataclasses.field(default_factory=list)
    blocks: list[tuple[str, str]] = dataclasses.field(default_factory=list)   # (locator, kind) of every block seen
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
                + self.drift()[1] + len(self.untyped) + len(self.uncitable) + len(self.standing))


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


def parse(lines: list[str], manifest: dict[str, dict] | None = None) -> Catalog:
    """The 1985 catalog of a transcription's lines (the module docstring states the structure it reads), with the
    content-keyed scan measured against `manifest`, the `blocks` of extract_rule_catalog.UNHARVESTED_OF[1985]: the
    paragraphs it admits are rule blocks of their declared kind."""
    cat = Catalog(declared=dict(manifest or {}))
    admitted = admitted_kinds(cat.declared)
    folio_of: dict[int, str] = {}
    page = 0
    for line in lines:
        if PAGE_HEADING.match(line):
            page = int(line.split()[-1])
        elif (f := FOLIO_85.match(line)) is not None:
            folio_of[page] = f.group(1)

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
    page_top = False
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
        year, folio = YEAR_OF[document], start["folio"] or folio_of.get(start["page"], "")
        add_row(cat, document=document, section=section, paragraph=para, kind=kind, n=start["n"],
                sublist=start["sublist"], text=text, page=start["page"], folio=folio,
                band=start["band"], subject=titles.get(para.rsplit(".", 1)[0], ""),
                citation=f"§{year}:{loc}" + ("" if kind == "FMT" else
                                             f" {citation.rule_designator(kind, start['n'], start['sublist'])}"),
                provisional=PROVISIONAL[frozenset(drawn_from)])
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
            cat.unharvested[scan[0]] = {"title": scan[1], **shape}
        scan, scan_body = None, []

    spec: str | dict | None = None  # the open block's kind as the manifest admits it (rule_kind), or None
    scan: tuple[str, str] | None = None   # (citation, title) of the paragraph the content-keyed scan reads
    scan_body: list[str] = []
    repeat = False                 # on a page the scan prints twice (SCAN_REPEAT_85): no rule is read from it
    in_note = False                # inside an editorial note's blockquote paragraph (EDITORIAL_NOTE)
    met: set[str] = set()          # every adjudicated locator the transcription still has
    # ⛔ A RECONSTRUCTION IS READ AS TEXT, ITS ROWS MARKED (kb/Work PB4952): it stands where the scan lacks printed pages
    # and carries their headings and rules, so a rule continues across it as across a page break; `drawn_from` records
    # whether the open rule's lines came from inside one (True), from the printed text (False), or both, and a rule
    # that opens inside one is on the folio it reconstructs.
    recon_folio = reconstructed(lines)
    inside = [f is not None for f in recon_folio]
    drawn_from: set[bool] = set()
    for at, line in enumerate(lines):
        if PAGE_HEADING.match(line):
            page, page_top, repeat = int(line.split()[-1]), True, False
            continue
        if repeat or not line.strip():
            in_note = False
            continue
        # ⛔ AN EDITORIAL NOTE IS NOT RULE TEXT (R66, kb/Work PB4972, PB4952): `> ⚠ **CORRECTED — see the Errata (Cn).**`
        # flags a correction where it was made and quotes the printed form, the rule's text being the corrected line above
        # it; a reconstruction's opening and closing notes name its folios, sources and confidence.
        if EDITORIAL_NOTE.match(line) or (in_note and line.startswith(">")):
            in_note = True
            if RECONSTRUCTED.match(line):
                page_top = False           # the reconstruction is no printed page: it carries no running header
            elif RECONSTRUCTED_END.match(line):
                page_top = True            # the printed page resumes with its running header
            continue
        in_note = False
        if SCAN_REPEAT_85.match(line):
            repeat = True
            continue
        if FOLIO_85.match(line):
            continue
        if page_top:
            page_top = False
            if running_header(line):
                continue
        if AMENDMENT_85.match(line):
            close_block()
            close_scan()
            amendment = AMENDMENT_1 if line.split()[2].rstrip(":") == "1" else AMENDMENT_2
            section, document, appendix = None, BASE, None
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
                    start = {"n": 0, "sublist": 1, "page": page, "band": "", "folio": recon_folio[at]}
                    buf, drawn_from = [], set()
                    block_rules = 1
            elif RULE_SHAPED.search(title):
                cat.unrecognised.append((loc, title))
            continue
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
            start = {"n": n, "sublist": sublist, "page": page, "band": band, "kind": kind, "folio": recon_folio[at]}
            buf, drawn_from = [m.group("box") + m.group("text")], {inside[at]}
        elif buf:
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
                          cat.standing)):
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
    out.append("✓ no findings" if not cat.findings() else f"FINDINGS: {cat.findings()}")
    return out


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
