#!/usr/bin/env python3
"""THE CATALOG'S PROSE HOLES: the normative sentences of an edition's transcription that are the text of no catalog row
(kb/Work PB5924, step 1). The rule catalog takes the NUMBERED rules of a clause; normative text a paragraph states
outside a numbered rule has no row, so it is outside the inventory's GAP count. This tool measures that hole, so that
"the catalog holds every rule of the edition" is a computed answer.

    python scripts/spec/catalog_prose_holes.py --edition 1985 [--json] [--list N]

MEASURED FOR 1985 ONLY. The 1985 transcription has its own structure (sections, appendices, amendments, paragraph
numbers, folios), read by the structure readers of `citation.py`, `spec_pdf.py`, `cite.py` and `rule_catalog_1985.py`,
which this module REUSES, so a paragraph's locator is the catalog's own (`<section or appendix>.<paragraph number>`). The
ISO editions (2002, 2014, 2023) are read by another reader; for them `holes` raises `NotMeasured`, and a caller must
show that rather than a zero (an input the tool cannot resolve fails loudly).

SCOPE (1985): Sections I, II, IV, VI-XVI, Section A (Amendment 1) and Appendix C of Section XVII. Excluded: the front
matter, Section III (glossary), Section V (the composite language skeleton, a summary of the formats), Section XVIII
(index), Appendices A and B (history and differences) and the amendments' change tables (applied in place in the
transcription, kb/Work PB5045). General-format paragraphs (kind FMT) are skipped: their rows are the formats. Appendix C
restates elements the body specifies (the obsolete, implementor-defined and undefined lists): it is measured and reported
(`Holes.summary`) but is not part of the count a verdict uses (`Holes.count`).

UNIT: a prose line of a paragraph's body (the transcription writes a printed paragraph as one line), split into
sentences. Skipped: headings, tables (`|`), `<pre>` blocks, HTML comments and tag-only lines, editorial notes, folios and
running headers. A leading rule ordinal `(n)`, `a.`, `1)` or bullet is dropped.

NORMATIVE: a sentence matching `NORMATIVE` (the modal and requirement vocabulary of PB5924). KNOWN UNDERCOUNT: a sentence
that states a requirement or a runtime effect without one of these words is not counted, and neither is text inside a
table. COVERED: the sentence, normalised (markup, escapes and box sides dropped, whitespace collapsed, case folded), is a
substring of some row's normalised `text`; a sentence of 25 characters or fewer is a fragment and is ignored.
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import json
import pathlib
import re
import sys
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from cite import EDITORIAL_NOTE  # noqa: E402
from citation import (AMENDMENT_85, APPENDIX_85, FOLIO_85, MARK_85, PARAGRAPH_85,  # noqa: E402
                      SCAN_REPEAT_85, SECTION_85)
from inventory_schema import REPO  # noqa: E402
from rule_catalog_1985 import kind_of, running_header  # noqa: E402
import spec_pdf  # noqa: E402  (the transcription's path)
from spec_pdf import BOX_SIDE, PAGE_HEADING  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

#: The editions this module can read: the year -> its catalog, relative to the repository (the transcription is `spec_pdf.markdown`).
MEASURED = {1985: "docs/rearchitecture/spec-rule-catalog-1985.json"}

NORMATIVE = re.compile(
    r"\b(must|cannot|can ?not|shall|may only|may not|(is|are) required|required to|"
    r"(is|are) not (allowed|permitted)|(is|are) permitted only|"
    r"each (compiler|implementation)|implementor (shall|will|must|is required|defines|specifies|determines)|"
    r"(is|are) undefined|unpredictable)\b", re.IGNORECASE)

INCLUDED_SECTIONS = {"I", "II", "IV", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "A"}
INCLUDED_APPENDICES = {"C"}
SUMMARY_APPENDICES = {"C"}
#: A sentence this short is a fragment, never a rule.
MIN_SENTENCE = 25
ORD_PREFIX = re.compile(r"^(?:[-*•]\s+|\\?\(\d+\\?\)\s+|[a-z]\.\s+|\d+\)\s+)+")
SENTENCE_SPLIT = re.compile(r"(?<=[.;])\s+(?=[A-Z(])")
ABBREV_END = re.compile(r"\b(e\.g|i\.e|etc|viz|Vol|No|Fig|para)\.$", re.IGNORECASE)
#: The longest hole text a record keeps.
HOLE_TEXT = 300


class NotMeasured(LookupError):
    """The edition's transcription is read by no reader of this module: nothing is measured, and a caller says so."""


@dataclasses.dataclass(frozen=True)
class Hole:
    """The uncovered normative sentences of one paragraph."""
    locator: str
    title: str
    rows_under_locator: int
    summary: bool
    sentences: tuple[tuple[str, int, str], ...]   # (text, page, folio)


@dataclasses.dataclass(frozen=True)
class Holes:
    """What `holes` measured over one edition."""
    edition: int
    catalog_rows: int
    paragraphs_scanned: int
    normative: int
    covered: int
    holes: tuple[Hole, ...]

    @property
    def body(self) -> list[Hole]:
        """The holes of the body of the edition (not of a summary appendix)."""
        return [h for h in self.holes if not h.summary]

    @property
    def count(self) -> int:
        """THE NUMBER A VERDICT USES: the uncovered normative sentences of the body."""
        return sum(len(h.sentences) for h in self.body)

    @property
    def summary(self) -> int:
        """The uncovered normative sentences of the summary appendices, reported and not counted."""
        return sum(len(h.sentences) for h in self.holes if h.summary)

    def by_section(self) -> dict[str, int]:
        out: collections.Counter[str] = collections.Counter()
        for h in self.body:
            out[h.locator.split(".")[0]] += len(h.sentences)
        return dict(out)

    def as_doc(self, listed: int = 0) -> dict[str, Any]:
        """The measurement as JSON-able data: counts, the sentences per section, and the `listed` largest paragraphs."""
        largest = sorted(self.body, key=lambda h: (-len(h.sentences), h.locator))[:listed]
        return {"edition": self.edition, "catalog-rows": self.catalog_rows, "paragraphs-scanned": self.paragraphs_scanned,
                "normative-sentences": self.normative, "covered": self.covered, "uncovered-body": self.count,
                "uncovered-summary-appendix": self.summary, "paragraphs-with-holes": len(self.body),
                "paragraphs-with-holes-and-no-row": sum(1 for h in self.body if not h.rows_under_locator),
                "by-section": self.by_section(),
                "largest": [{"locator": h.locator, "title": h.title, "sentences": len(h.sentences),
                             "rows-under-locator": h.rows_under_locator,
                             "text": [s[0] for s in h.sentences]} for h in largest]}


def normalise(s: str) -> str:
    """`s` as the comparison reads it: markup, escapes and box sides dropped, quotes and dashes folded, whitespace
    collapsed, case folded."""
    s = re.sub(r"</?u>|</?b>|</?i>|\*\*|" + BOX_SIDE + r"|<br>", " ", s)
    s = s.replace("\\(", "(").replace("\\)", ")").replace("\\*", "*").replace("\\_", "_")
    s = re.sub(r"[‘’]", "'", s)
    s = re.sub(r"[“”]", '"', s)
    s = re.sub(r"[–—]", "-", s)
    return re.sub(r"\s+", " ", s).strip().lower()


def sentences(line: str) -> list[str]:
    """The sentences of one prose line, each without its leading ordinal or bullet."""
    parts, cur = [], ""
    for piece in SENTENCE_SPLIT.split(line):
        cur = f"{cur} {piece}".strip() if cur else piece
        if ABBREV_END.search(cur.rstrip()) is None:
            parts.append(cur)
            cur = ""
    if cur:
        parts.append(cur)
    return [ORD_PREFIX.sub("", p.strip().lstrip(BOX_SIDE).strip()).strip() for p in parts]


@dataclasses.dataclass
class _Paragraph:
    locator: str
    title: str
    kind: str | None
    lines: list[tuple[str, int, str]] = dataclasses.field(default_factory=list)   # (text, page, folio)


def paragraphs_1985(lines: list[str]) -> list[_Paragraph]:
    """The in-scope paragraphs of the 1985 transcription `lines`, in order, each with its prose lines (module docstring)."""
    paras: dict[str, _Paragraph] = {}
    section = appendix = None
    in_amendment = False
    cur: _Paragraph | None = None
    page = 0
    page_top = repeat = in_note = in_pre = False
    folio = ""

    def in_scope() -> bool:
        if appendix is not None:
            return appendix in INCLUDED_APPENDICES
        return section in INCLUDED_SECTIONS

    def open_para(loc: str, title: str, kind: str | None) -> None:
        nonlocal cur
        cur = paras.setdefault(loc, _Paragraph(loc, title, kind))
        cur.kind = kind

    for line in lines:
        if PAGE_HEADING.match(line):
            page, page_top, repeat = int(line.split()[-1]), True, False
            continue
        if in_pre:
            in_pre = "</pre>" not in line
            continue
        if repeat or not line.strip():
            in_note = False
            continue
        if EDITORIAL_NOTE.match(line) or (in_note and line.startswith(">")):
            in_note = True
            continue
        in_note = False
        if SCAN_REPEAT_85.match(line):
            repeat = True
            continue
        if (f := FOLIO_85.match(line)) is not None:
            folio, page_top = f.group(1), True
            continue
        if page_top:
            page_top = False
            if running_header(line):
                continue
        if AMENDMENT_85.match(line):
            in_amendment, section, appendix, cur = True, None, None, None
            continue
        if (head := SECTION_85.match(line) or APPENDIX_85.match(line)) is not None:
            name = head.group(1)
            if SECTION_85.match(line):
                appendix = None
                section = name if (not in_amendment or name == "A") else None
            else:
                appendix = name
            cur = None
            if section is not None and in_scope():
                open_para(appendix or section, "(section preamble)", None)
            continue
        if section is None or not in_scope():
            continue
        if (p := PARAGRAPH_85.match(line)) is not None:
            num = (p.group(1) or p.group(2)).rstrip(".")
            title = re.sub(r"^#*\s*" + MARK_85 + re.escape(num) + r"\.?\s*", "", line).strip()
            title = re.sub(rf"</?u>|\*\*|{BOX_SIDE}", "", title).strip()
            open_para(f"{appendix or section}.{num}", title, kind_of(title))
            continue
        if cur is None or cur.kind == "FMT":
            continue
        s = line.strip()
        if s.startswith("<pre"):
            in_pre = "</pre>" not in s
            continue
        if s.startswith(("#", "|", "<!--")) or re.fullmatch(r"<[^>]+>", s):
            continue
        cur.lines.append((s, page, folio))
    return list(paras.values())


def holes(edition: int, root: pathlib.Path = REPO) -> Holes:
    """THE MEASUREMENT of `edition` in the tree at `root`: `NotMeasured` for an edition no reader here reads."""
    if edition not in MEASURED:
        raise NotMeasured(f"catalog prose holes are measured for {sorted(MEASURED)} only: the {edition} transcription is "
                          "read by another reader, which this tool does not yet use")
    lines = spec_pdf.markdown(edition, root).read_text(encoding="utf-8").splitlines()
    rows = json.loads((root / MEASURED[edition]).read_text(encoding="utf-8"))["rules"]
    corpus = "\x00".join(normalise(r["text"]) for r in rows)
    locators = [r["locator"] for r in rows]

    def rows_under(loc: str) -> int:
        return sum(1 for x in locators if x == loc or x.startswith(loc + "."))

    found: list[Hole] = []
    scanned = normative = covered = 0
    for para in paragraphs_1985(lines):
        if not para.lines:
            continue
        scanned += 1
        open_sentences = []
        for text, page, folio in para.lines:
            for sent in sentences(text):
                if len(sent) <= MIN_SENTENCE or NORMATIVE.search(sent) is None:
                    continue
                normative += 1
                if normalise(sent) in corpus:
                    covered += 1
                else:
                    open_sentences.append((sent[:HOLE_TEXT], page, folio))
        if open_sentences:
            found.append(Hole(para.locator, para.title, rows_under(para.locator),
                              para.locator.split(".")[0] in SUMMARY_APPENDICES, tuple(open_sentences)))
    return Holes(edition, len(rows), scanned, normative, covered, tuple(found))


def text_of(h: Holes, listed: int) -> str:
    d = h.as_doc(listed)
    out = [f"{h.edition}: scanned {h.paragraphs_scanned} paragraphs against {h.catalog_rows} catalog rows; "
           f"{h.normative} normative sentences, {h.covered} are the text of a row",
           f"  {h.count} uncovered in the body, in {len(h.body)} paragraphs ({d['paragraphs-with-holes-and-no-row']} with no "
           f"row at or under their locator); {h.summary} more in Appendix C (a summary, not counted)",
           "  by section: " + " · ".join(f"{k} {v}" for k, v in sorted(h.by_section().items()))]
    for big in d["largest"]:
        out.append(f"  {big['locator']}  {big['sentences']}  rows={big['rows-under-locator']}  {big['title'][:60]}")
    return "\n".join(out)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The normative sentences of an edition's transcription that no catalog row holds "
                                             "(kb/Work PB5924).")
    ap.add_argument("--edition", type=int, required=True)
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--list", type=int, default=0, metavar="N", help="also list the N paragraphs with the most holes")
    a = ap.parse_args(argv)
    try:
        h = holes(a.edition)
    except NotMeasured as e:
        print(f"not measured: {e}")
        return 2
    print(json.dumps(h.as_doc(a.list), indent=1, ensure_ascii=False) if a.json else text_of(h, a.list))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
