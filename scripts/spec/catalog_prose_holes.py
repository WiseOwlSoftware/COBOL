#!/usr/bin/env python3
"""THE CATALOG'S PROSE HOLES: the normative text of an edition's transcription that no catalog row holds (kb/Work PB5924
step 1, PB479's measure). The rule catalog takes the NUMBERED rules of a clause; normative text a paragraph states
outside a numbered rule has no row, so it is outside the inventory's GAP count. This tool measures that hole, so that
"the catalog holds every rule of the edition" is a computed answer.

    python scripts/spec/catalog_prose_holes.py --edition 1985 [--json] [--list N]

MEASURED FOR 1985 ONLY. The paragraphs are the ones `rule_catalog_1985.parse` walks (`Catalog.paragraphs`): ONE walk of
the 1985 transcription, shared with the catalog, so a paragraph's locator is the catalog's own (`<section or
appendix>.<paragraph number>`) and a change to that walk reaches both (kb/Work PB5924 review R1-3). `holes` refuses
(SystemExit) when a committed catalog row's locator is no paragraph of that walk: the two have drifted. The ISO editions
(2002, 2014, 2023) are read by another reader; for them `holes` raises `NotMeasured`, and a caller must show that rather
than a zero (an input the tool cannot resolve fails loudly).

THE NORMATIVE REGION (kb/Work PB479, "Phase A, continued" of docs/rearchitecture/DESIGN-spec-conformance-review.md):
Sections IV, VI to XVI and Amendment 1's Section A. Outside it: the front matter, Section I (introduction), Section II
(concepts, explanatory: the counterpart of 2023's informative Annex D), Section III (glossary), Section V (the composite
language skeleton), Section XVII (its appendices restate the body), Section XVIII (index) and the amendments' change
tables (applied in place in the transcription, kb/Work PB5045). General-format paragraphs (kind FMT) are skipped: their
rows are the formats.

THE UNIT, AND THE NUMBER A VERDICT USES (`Holes.count`): a PRINTED PARAGRAPH, one prose line of a paragraph's body (the
transcription writes a printed paragraph as one line), in the normative region, that is IN NO ROW: some sentence of it
longer than `MIN_SENTENCE` characters, normalised (markup, escapes and box sides dropped, whitespace collapsed, case
folded), is a substring of no row AT OR UNDER the paragraph's locator (a section's preamble: of no row at that locator).
Any verb or none: the verb criterion finds about a third of the unnumbered obligations (PB479's sample), so it is never
the count. Skipped: headings, tables (`|`), `<pre>` blocks, HTML comments, tag-only lines and a line with no sentence
longer than `MIN_SENTENCE` (a fragment). A leading rule ordinal `(n)`, `a.`, `1)` or bullet is dropped.

NO PARAGRAPH IS DISCOUNTED WITHOUT A DISPOSITION. kb/Work PB5452 decides, paragraph by paragraph, whether one is a rule
(and mints its row) or not a rule; until it records them (`DISPOSITIONS` names none), every printed paragraph in no row
is counted.

REPORTED BESIDE THE COUNT, never instead of it:
  - THE DEONTIC FLOOR (`Holes.deontic`): the sentences of the uncovered paragraphs that carry the requirement vocabulary
    (`NORMATIVE`) and lie in no row at or under the locator: a labelled floor inside the count;
  - THE ROWLESS PARAGRAPHS (`Holes.rowless`): the numbered paragraphs with prose and no row at or under their locator;
  - THE ELSEWHERE LEAD (`Holes.elsewhere`): an uncovered printed paragraph whose every sentence is in SOME row of another
    clause (a restatement, or a row filed under the wrong locator): counted, and named as a lead (review R1-4).
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
from extract_rule_catalog import UNHARVESTED_OF, manifest_blocks  # noqa: E402  (the 1985 content-keyed scan's manifest)
from inventory_schema import REPO  # noqa: E402
import rule_catalog_1985  # noqa: E402  (THE walk of the 1985 transcription: its paragraphs)
import spec_pdf  # noqa: E402  (the transcription's path)
from spec_pdf import BOX_SIDE  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

#: The editions this module can read: the year -> its committed catalog, relative to the repository (the transcription is
#: `spec_pdf.markdown`).
MEASURED = {1985: "docs/rearchitecture/spec-rule-catalog-1985.json"}
#: The disposition store of each edition (kb/Work PB5452): none is recorded yet, so no paragraph is discounted.
DISPOSITIONS: dict[int, str] = {}

NORMATIVE = re.compile(
    r"\b(must|cannot|can ?not|shall|may only|may not|(is|are) required|required to|"
    r"(is|are) not (allowed|permitted)|(is|are) permitted only|"
    r"each (compiler|implementation)|implementor (shall|will|must|is required|defines|specifies|determines)|"
    r"(is|are) undefined|unpredictable)\b", re.IGNORECASE)

#: THE NORMATIVE REGION of 1985 (kb/Work PB479): a paragraph's section (an appendix of Section XVII is outside it, and its
#: letter is no section: Appendix A is not Amendment 1's Section A).
REGION_1985 = frozenset({"IV", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "A"})
#: A sentence this short is a fragment, never a rule.
MIN_SENTENCE = 25
ORD_PREFIX = re.compile(r"^(?:[-*•]\s+|\\?\(\d+\\?\)\s+|[a-z]\.\s+|\d+\)\s+)+")
SENTENCE_SPLIT = re.compile(r"(?<=[.;])\s+(?=[A-Z(])")
ABBREV_END = re.compile(r"\b(e\.g|i\.e|etc|viz|Vol|No|Fig|para)\.$", re.IGNORECASE)
#: The longest paragraph text a record keeps.
HOLE_TEXT = 300


class NotMeasured(LookupError):
    """The edition's transcription is read by no reader of this module: nothing is measured, and a caller says so."""


@dataclasses.dataclass(frozen=True)
class Uncovered:
    """One printed paragraph in no row at or under its locator."""
    locator: str
    title: str
    rows_under_locator: int
    text: str
    page: int
    folio: str
    deontic: tuple[str, ...]     # its sentences in the requirement vocabulary that no row at or under the locator holds
    elsewhere: bool              # every sentence of it is in SOME row (another clause's): a lead, still counted


@dataclasses.dataclass(frozen=True)
class Holes:
    """What `holes` measured over one edition."""
    edition: int
    catalog_rows: int
    numbered_paragraphs: int     # paragraphs of the region with a measured printed paragraph
    printed_paragraphs: int      # printed paragraphs measured (fragments excluded)
    uncovered: tuple[Uncovered, ...]
    rowless: tuple[str, ...]     # numbered paragraphs with a measured printed paragraph and no row at or under them

    @property
    def count(self) -> int:
        """THE NUMBER A VERDICT USES: the printed paragraphs of the region in no row (none discounted: `DISPOSITIONS`)."""
        return len(self.uncovered)

    @property
    def deontic(self) -> int:
        """The labelled floor inside the count: the uncovered sentences in the requirement vocabulary."""
        return sum(len(u.deontic) for u in self.uncovered)

    @property
    def elsewhere(self) -> int:
        return sum(1 for u in self.uncovered if u.elsewhere)

    def by_section(self) -> dict[str, int]:
        return dict(collections.Counter(u.locator.split(".")[0] for u in self.uncovered))

    def as_doc(self, listed: int = 0) -> dict[str, Any]:
        """The measurement as JSON-able data: counts, the paragraphs per section, and the `listed` paragraphs with the most
        uncovered printed paragraphs."""
        per = collections.Counter(u.locator for u in self.uncovered)
        titles = {u.locator: u.title for u in self.uncovered}
        largest = sorted(per, key=lambda loc: (-per[loc], loc))[:listed]
        return {"edition": self.edition, "catalog-rows": self.catalog_rows,
                "numbered-paragraphs": self.numbered_paragraphs, "printed-paragraphs": self.printed_paragraphs,
                "uncovered-paragraphs": self.count, "deontic-floor": self.deontic,
                "rowless-paragraphs": len(self.rowless), "elsewhere-lead": self.elsewhere,
                "paragraphs-with-holes": len(per), "dispositions": DISPOSITIONS.get(self.edition),
                "by-section": self.by_section(),
                "largest": [{"locator": loc, "title": titles[loc], "uncovered": per[loc],
                             "text": [u.text for u in self.uncovered if u.locator == loc]} for loc in largest]}


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


def prose(para: rule_catalog_1985.Paragraph) -> list[tuple[str, int, str]]:
    """The prose lines of a walked paragraph: headings, tables, `<pre>` blocks, HTML comments and tag-only lines dropped."""
    out, in_pre = [], False
    for text, page, folio in para.lines:
        if in_pre:
            in_pre = "</pre>" not in text
            continue
        if text.startswith("<pre"):
            in_pre = "</pre>" not in text
            continue
        if text.startswith(("#", "|", "<!--")) or re.fullmatch(r"<[^>]+>", text):
            continue
        out.append((text, page, folio))
    return out


def walk(edition: int, root: pathlib.Path) -> rule_catalog_1985.Catalog:
    """THE walk of the edition's transcription in the tree at `root` (the catalog's own, `rule_catalog_1985.parse`)."""
    lines = spec_pdf.markdown(edition, root).read_text(encoding="utf-8").splitlines()
    manifest = UNHARVESTED_OF[edition] if root == REPO else root / UNHARVESTED_OF[edition].relative_to(REPO)
    return rule_catalog_1985.parse(lines, manifest_blocks(manifest))


def holes(edition: int, root: pathlib.Path = REPO) -> Holes:
    """THE MEASUREMENT of `edition` in the tree at `root`: `NotMeasured` for an edition no reader here reads."""
    if edition not in MEASURED:
        raise NotMeasured(f"catalog prose holes are measured for {sorted(MEASURED)} only: the {edition} transcription is "
                          "read by another reader, which this tool does not yet use")
    cat = walk(edition, root)
    rows = json.loads((root / MEASURED[edition]).read_text(encoding="utf-8"))["rules"]
    # ⛔ ONE WALK, CHECKED: every committed row sits at a paragraph of the walk, and every block the walk opened is one of
    # its paragraphs. A row the walk does not reach means the catalog and this measure read different documents.
    drift = sorted({r["locator"] for r in rows} - set(cat.paragraphs)) + sorted(
        {loc for loc, _ in cat.blocks} - set(cat.paragraphs))
    if drift:
        raise SystemExit(f"catalog prose holes: {len(drift)} catalog locator(s) are no paragraph of the transcription's "
                         f"walk (first {drift[0]}): regenerate the catalog (extract_rule_catalog.py --edition {edition})")
    by_loc: dict[str, list[str]] = collections.defaultdict(list)
    for r in rows:
        by_loc[r["locator"]].append(normalise(r["text"]))
    everything = "\x00".join(t for ts in by_loc.values() for t in ts)

    def under(loc: str, preamble: bool) -> list[str]:
        return [t for k, ts in by_loc.items() if k == loc or (not preamble and k.startswith(loc + ".")) for t in ts]

    found: list[Uncovered] = []
    rowless: list[str] = []
    numbered = printed = 0
    for para in cat.paragraphs.values():
        if para.appendix is not None or para.section not in REGION_1985 or para.kind == "FMT":
            continue
        preamble = "." not in para.locator
        scope = under(para.locator, preamble)
        corpus = "\x00".join(scope)
        measured_here = 0
        for text, page, folio in prose(para):
            sents = [s for s in sentences(text) if len(s) > MIN_SENTENCE]
            if not sents:
                continue
            measured_here += 1
            missing = [s for s in sents if normalise(s) not in corpus]
            if not missing:
                continue
            found.append(Uncovered(para.locator, para.title, len(scope), text[:HOLE_TEXT], page, folio,
                                   tuple(s[:HOLE_TEXT] for s in missing if NORMATIVE.search(s)),
                                   all(normalise(s) in everything for s in missing)))
        if measured_here:
            numbered += 1
            printed += measured_here
            if not scope and not preamble:
                rowless.append(para.locator)
    return Holes(edition, len(rows), numbered, printed, tuple(found), tuple(rowless))


def text_of(h: Holes, listed: int) -> str:
    d = h.as_doc(listed)
    out = [f"{h.edition}: {h.printed_paragraphs} printed paragraphs of the normative region measured, in "
           f"{h.numbered_paragraphs} numbered paragraphs, against {h.catalog_rows} catalog rows",
           f"  {h.count} printed paragraphs in no row at or under their locator (in {d['paragraphs-with-holes']} numbered "
           f"paragraphs; {len(h.rowless)} numbered paragraphs have no row at all); no disposition recorded (kb/Work "
           f"PB5452), none discounted",
           f"  floor inside it: {h.deontic} deontic sentences in no row · lead: {h.elsewhere} paragraphs whose text is in a "
           "row of another clause",
           "  by section: " + " · ".join(f"{k} {v}" for k, v in sorted(h.by_section().items()))]
    for big in d["largest"]:
        out.append(f"  {big['locator']}  {big['uncovered']}  {big['title'][:60]}")
    return "\n".join(out)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The normative text of an edition's transcription that no catalog row holds "
                                             "(kb/Work PB5924, PB479).")
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
