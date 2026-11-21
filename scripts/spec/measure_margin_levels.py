#!/usr/bin/env python3
"""Measure, on the printed PDF, the LEVEL of every paragraph a numbered rule's text holds after the rule's own, and
record it as data the rule catalogs read (kb/Work PB5449 item 3; decision PB5448 point 4).

WHY A MEASUREMENT. A rule block's numbered rule runs, in the transcription, to the next rule, so a paragraph printed
after it is appended to its text. That is right when the page prints the paragraph at the rule's hanging indent (a
continuation of the rule) and wrong when the page prints it at the block's own margin: then it is a MARGIN PARAGRAPH of
the clause, a unit of unnumbered text with a disposition of its own (§2023:14.8.2.2's "If either the formal parameter or
the argument is a variable length group" sits inside the row `SR-14.8.2.2-2`). The transcription cannot tell the two
apart (the 1985 transcription keeps no indentation at all), so the answer is the printed page, as
`measure_sublist_levels.py`'s is for a sub-list.

ONE MECHANISM FOR EVERY EDITION. The candidates are the edition's reader's (`READERS`: the paragraphs after a rule, and
the block lead-ins that calibrate), each with its key (clause, digest of its text, order among equal digests: the key a
unit's disposition has) and the PDF page its text starts on. Each is found on its page and the next by its opening words
(fuzzily: the 1985 text layer is OCR), and the x of its first printed line is set against the page's own margin (the
commonest left edge of its long lines). Level 0 is CALIBRATED on the lead-ins, which print at the block's margin by
definition: their median offset (1985 indents every paragraph's first line, 17.7 pt; 2023 indents none), each level
`LEVEL_PT` (17.5 pt) deeper. A paragraph whose best matches print at more than one level, or not found, is read on the
rendered page (`scripts/render-spec-page.py <page>`) and the reading recorded by hand, with the page and what was seen,
in `scripts/spec/data/margin-readings-<year>.json` (`readings`); `--write` records each reading's level in the data
(`level` 0, or `READ_CONTINUES`, with `read_on_page`). A paragraph neither measured nor read stays `ambiguous` or `not
found`, and in its rule's text; a reading whose paragraph is gone, or is now measured, is refused (kb/Work PB5998).

`--write` records the measurement in `scripts/spec/data/margin-levels-<year>.json`, committed so the catalog
regeneration (`extract_rule_catalog.py`) and its `--check` read it without the licensed PDF: a candidate the data does
not record, or a record no candidate has, is a finding of the edition's catalog (`refusals`), and a level-0 paragraph is
a unit of the content-keyed manifest's `paragraph-blocks` (`at_margin`).

    python scripts/spec/measure_margin_levels.py --edition 1985 [--private <dir holding the PDFs>]   # report
    python scripts/spec/measure_margin_levels.py --edition 1985 --write                             # record it
    python scripts/spec/measure_margin_levels.py --edition 1985 --check                             # data vs text, no PDF

2023's reader of its candidates is not built yet (kb/Work PB5449, item 3's 2023 half): its measurement at design time,
outside the repository, found 122 level-0 paragraphs after a rule.
"""
from __future__ import annotations

import argparse
import collections
import difflib
import json
import pathlib
import re
import statistics
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts" / "spec"))
from cite import norm  # noqa: E402  (ONE notion of "the same words" as the citation checker's)
from measure_sublist_levels import LEVEL_PT, _visual_lines  # noqa: E402  (ONE notion of a printed line and a level)
import spec_pdf  # noqa: E402  (the one PDF locator, kb/Work PB3208)
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

DATA = REPO / "scripts" / "spec" / "data"
#: The recorded levels that are not numbers: the paragraph's first line was found at more than one level, or not at all.
AMBIGUOUS, NOT_FOUND = "ambiguous", "not found"
#: The level of a paragraph read on the rendered page as continuing its rule (at the rule's indent or deeper, or printed
#: as no paragraph of its own: a later amendment's added sentence), where the measurement gives no number.
READ_CONTINUES = "continues the rule (read on the page)"
#: What a reading in `margin-readings-<year>.json` says, and the level `--write` records for it.
READ_LEVELS = {"level 0": 0, "continues the rule": READ_CONTINUES}
#: A printed line matches its paragraph when its opening is within this difflib ratio of the paragraph's (an OCR text
#: layer misreads a few characters a line), over at most `MATCH_CHARS` characters, and holds at least `MIN_CHARS`.
FUZZ, MATCH_CHARS, MIN_CHARS = 0.9, 60, 20
#: The left-edge bin, in points, in which lines share an edge.
MARGIN_BIN = 4
#: How many long lines must share a left edge for it to be the margin (`_margin_of`).
MARGIN_LINES = 2
#: Where each edition's PDF body band begins, in points from the page top: below it is the running head. The 1985 scan
#: prints a page's first body line as high as 41 pt and its running head above 40 pt (kb/Work PB5998: at 60 pt the band
#: dropped the first line of a page, 8 of the 63 paragraphs then not found: on pages 161, 247, 249, 267, 325, 347, 419
#: and 614).
HEAD_Y_OF = {1985: 40}


def data_path(year: int) -> pathlib.Path:
    return DATA / f"margin-levels-{year}.json"


def readings_path(year: int) -> pathlib.Path:
    return DATA / f"margin-readings-{year}.json"


def readings(year: int) -> dict[tuple[str, str, int], dict]:
    """THE READINGS of `year` (`readings_path`): each paragraph the measurement cannot place, read on the rendered page,
    by key (clause, digest, order); empty when the edition records none. A reading that says anything but `READ_LEVELS`
    fails loudly."""
    path = readings_path(year)
    if not path.is_file():
        return {}
    out = {}
    for r in json.loads(path.read_text(encoding="utf-8"))["readings"]:
        if r["read"] not in READ_LEVELS:
            raise ValueError(f"{path.name}: {r['clause']} {r['follows']}: reading {r['read']!r} is not one of "
                             f"{', '.join(READ_LEVELS)}")
        out[(r["clause"], r["digest"], r["order"])] = r
    return out


def _1985():
    """1985's candidates and calibration (rule_catalog_1985.block_paragraphs over the committed catalog's rows)."""
    import inventory_schema  # noqa: PLC0415
    import rule_catalog_1985  # noqa: PLC0415
    lines = spec_pdf.markdown(1985).read_text(encoding="utf-8").splitlines()
    leads, after = rule_catalog_1985.block_paragraphs(lines, inventory_schema.edition(1985).rules())
    return leads, after


#: Each edition's reader of its block paragraphs: (lead-ins, paragraphs after a rule), each a BlockParagraph-shaped
#: object (`clause`, `follows`, `unit`, `order`, `page`, `key`).
READERS = {1985: _1985}


def load(year: int, root: pathlib.Path = REPO) -> dict[tuple[str, str, int], dict]:
    """The recorded measurement of `year` in the tree at `root` by key (clause, digest, order); empty when the edition
    records none."""
    path = root / data_path(year).relative_to(REPO)
    if not path.is_file():
        return {}
    return {(e["clause"], e["digest"], e["order"]): e
            for e in json.loads(path.read_text(encoding="utf-8"))["paragraphs"]}


def refusals(after: list, recorded: dict[tuple[str, str, int], dict],
             read: dict[tuple[str, str, int], dict] | None = None) -> list[str]:
    """THE TRANSCRIPTION HELD TO THE DATA, without the PDF: a paragraph after a rule that the data does not record (new
    text, or text a repair changed: measure it), a record no paragraph has (the text it measured is gone), and a
    reading (`readings`) no paragraph has or the data does not carry (write the data again)."""
    keys = {p.key for p in after}
    out = [f"{p.clause} {p.follows}: a paragraph after the rule that the margin-level data does not record (digest "
           f"{p.unit.digest}, line {p.unit.line}): measure it (measure_margin_levels.py --write)"
           for p in after if p.key not in recorded]
    out += [f"{k[0]} {e.get('follows', '?')}: a margin-level record no paragraph has (digest {k[1]}): its text changed "
            f"or left the rule; measure again" for k, e in recorded.items() if k not in keys]
    for k, r in (read or {}).items():
        if k not in keys:
            out.append(f"{k[0]} {r['follows']}: a reading on page {r['page']} no paragraph has (digest {k[1]}): its text "
                       "changed or left the rule; read it again")
        elif recorded.get(k, {}).get("read_on_page") != r["page"]:
            out.append(f"{k[0]} {r['follows']}: a reading on page {r['page']} the margin-level data does not carry: "
                       "record it (measure_margin_levels.py --write)")
    return out


def at_margin(after: list, recorded: dict[tuple[str, str, int], dict]) -> list:
    """The paragraphs after a rule that the data measures at level 0: margin paragraphs of the clause, units."""
    return [p for p in after if recorded.get(p.key, {}).get("level") == 0]


def unread(recorded: dict[tuple[str, str, int], dict]) -> list[dict]:
    """The records still to be read on the rendered page (`ambiguous`, `not found`)."""
    return [e for e in recorded.values() if e["level"] in (AMBIGUOUS, NOT_FOUND)]


def _margin_of(vis: list[tuple[float, str]]) -> float:
    """The page's own margin: the leftmost left edge (`MARGIN_BIN` bins) that at least `MARGIN_LINES` of its long lines
    share. An OCR speck is short and a stray edge is one line, so neither is the margin; nor is the COMMONEST edge, which
    on a page of indented rule text is the indent (page 707 of the 1985 PDF: 4 lines at the margin, 9 at the indent)."""
    xs = [x for x, t in vis if len(t) >= MIN_CHARS]
    bins = collections.Counter(round(x / MARGIN_BIN) for x in xs)
    shared = [k for k, v in bins.items() if v >= MARGIN_LINES]
    if not shared:
        return min((x for x, _ in vis), default=0.0)
    near = [x for x in xs if round(x / MARGIN_BIN) == min(shared)]
    return sum(near) / len(near)


def _fuzzy(want: str, got: str) -> float:
    n = min(len(want), len(got), MATCH_CHARS)
    return difflib.SequenceMatcher(None, want[:n], got[:n]).ratio() if n else 0.0


#: One printed line a paragraph's opening matches: its PDF page, its x offset from the page's margin, and how well it
#: matches (`_fuzzy`'s ratio, then how many characters were compared: a short line matches a long opening only on its
#: own few characters, so a full-length match of equal ratio is the better).
Match = tuple[int, float, float, int]


def offsets(doc, paragraphs: list, head_y: float) -> list[list[Match]]:
    """For each paragraph, every printed line its opening matches (`Match`), on its page and, when not there, the next;
    a page's lines are read below `head_y` (`HEAD_Y_OF`)."""
    cache: dict[int, tuple[list, float]] = {}
    out = []
    for p in paragraphs:
        want = norm(p.unit.text.replace("\\", ""))
        found: list[tuple[int, float]] = []
        for pg in (p.page, p.page + 1):
            if not 1 <= pg <= doc.page_count:
                continue
            if pg not in cache:
                vis = _visual_lines(doc[pg - 1], head_y)
                cache[pg] = (vis, _margin_of(vis))
            vis, margin = cache[pg]
            found += [(pg, round(x0 - margin, 1), ratio, min(len(want), len(got), MATCH_CHARS)) for x0, text in vis
                      if len(got := norm(text)) >= MIN_CHARS and (ratio := _fuzzy(want, got)) >= FUZZ]
            if found:
                break
        out.append(found)
    return out


def best(found: list[Match]) -> list[Match]:
    """The matches that match best (`Match`: the highest ratio, then the most characters compared). A paragraph whose
    opening is a near copy of a sibling's (§1985:VI.6.23.4's "If index-name-2 is specified" beside rule (2)'s "If
    index-name-1 is specified", page 360) matches the sibling's line too, but less well than its own (kb/Work PB5998)."""
    top = max(((r, n) for *_, r, n in found), default=None)
    return [m for m in found if (m[2], m[3]) == top]


def level_of(found: list[Match], base: float) -> int | str:
    """The printed level of a paragraph from its best matched lines (`offsets`, `best`) and the calibrated level-0
    offset `base`: AMBIGUOUS only when equally good matches print at different levels. A negative level is a first line
    printed LEFT of a paragraph's opening (1985 indents every paragraph's first line, so such a line continues the text
    above it, after a display or a figure): it is no margin paragraph, and is recorded as measured."""
    levels = sorted({round((m[1] - base) / LEVEL_PT) for m in best(found)})
    return NOT_FOUND if not levels else levels[0] if len(levels) == 1 else AMBIGUOUS


def measure(year: int, private: pathlib.Path) -> dict:
    """The measurement of `year`'s paragraphs after a rule, calibrated on its lead-ins: the data file's document."""
    import pymupdf  # noqa: PLC0415  (PyMuPDF, as the sibling tools use it; only a measurement needs it)
    leads, after = READERS[year]()
    with pymupdf.open(spec_pdf.require(year, private)) as doc:
        lead_found = offsets(doc, leads, HEAD_Y_OF[year])
        after_found = offsets(doc, after, HEAD_Y_OF[year])
    first = [f[0][1] for f in lead_found if len({round(m[1] / LEVEL_PT) for m in f}) == 1]
    base = statistics.median(first)
    lead_levels = collections.Counter(str(level_of(f, base)) for f in lead_found)
    read = readings(year)
    paragraphs, measured_read = [], []
    for p, f in zip(after, after_found):
        entry = {"clause": p.clause, "follows": p.follows, "digest": p.unit.digest, "order": p.order, "page": p.page,
                 "level": level_of(f, base), "x_offset_pt": best(f)[0][1] if f else None,
                 "text": re.sub(r"\s+", " ", p.unit.text)[:80]}
        if (r := read.get(p.key)) is not None:
            if entry["level"] not in (AMBIGUOUS, NOT_FOUND):
                measured_read.append(f"{p.clause} {p.follows} (page {r['page']}): measured at level {entry['level']}")
            entry |= {"level": READ_LEVELS[r["read"]], "read_on_page": r["page"]}
        paragraphs.append(entry)
    gone = sorted(read.keys() - {p.key for p in after})
    if measured_read or gone:
        raise SystemExit(f"⛔ readings in {readings_path(year).name} the measurement no longer needs, or no paragraph "
                         "has: remove them\n  " + "\n  ".join(
                             measured_read + [f"{k[0]} digest {k[1]}: no paragraph" for k in gone]))
    return {"$comment": [f"Written by scripts/spec/measure_margin_levels.py --edition {year} --write; never edit by hand "
                         "(kb/Work PB5449 item 3, decision PB5448 point 4).",
                         "Every paragraph a numbered rule's text holds after the rule's own, measured on the printed PDF: "
                         "level 0 prints at the block's margin (a margin paragraph of the clause, a unit), deeper at the "
                         "rule's hanging indent (its continuation). A paragraph the measurement cannot place is read on "
                         f"the rendered page ({readings_path(year).name}): its level is the reading's, with "
                         "'read_on_page'. Keyed as a unit's disposition is: clause, digest, order."],
            "calibration": {"lead-ins": len(leads), "measured": len(first), "base_offset_pt": round(base, 1),
                            "level_step_pt": LEVEL_PT, "lead-in levels": dict(sorted(lead_levels.items()))},
            "paragraphs": paragraphs}


def tally(paragraphs: list[dict]) -> dict[str, int]:
    """The recorded paragraphs by what their level makes them: a margin paragraph (level 0), a continuation printed at
    the rule's indent (deeper) or left of a paragraph's opening (negative), or one to read on the rendered page."""
    def kind(level) -> str:
        if level == READ_CONTINUES:
            return "read on the page: continues the rule"
        if not isinstance(level, int):
            return f"{level} (to read on the page)"
        if level == 0:
            return "level 0 (a unit)"
        return "deeper (continues the rule)" if level > 0 else "left of the indent (continues the text)"
    return dict(sorted(collections.Counter(kind(e["level"]) for e in paragraphs).items()))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    spec_pdf.add_edition_argument(ap)
    mode = ap.add_mutually_exclusive_group()
    mode.add_argument("--write", action="store_true", help="record the measurement in the edition's data file")
    mode.add_argument("--check", action="store_true", help="hold the transcription to the recorded data (no PDF)")
    ap.add_argument("--private", type=pathlib.Path, default=spec_pdf.PRIVATE,
                    help="the directory holding the licensed PDFs (default: the specs-private submodule)")
    args = ap.parse_args()
    reader = READERS.get(args.edition)
    if reader is None:
        sys.exit(f"the {args.edition} reader of the paragraphs after a rule is not built (kb/Work PB5449 item 3: only "
                 f"{', '.join(map(str, READERS))} measures)")
    if args.check:
        bad = refusals(reader()[1], load(args.edition), readings(args.edition))
        for b in bad:
            print(f"⛔ {b}")
        print(f"=== MARGIN LEVELS {args.edition}: {'RED' if bad else 'GREEN'} — {len(load(args.edition))} recorded, "
              f"{len(bad)} refusal(s) ===")
        return 1 if bad else 0
    doc = measure(args.edition, args.private)
    print(f"lead-ins (calibration): {doc['calibration']}")
    print(f"paragraphs after a rule: {len(doc['paragraphs'])} {tally(doc['paragraphs'])}")
    if args.write:
        data_path(args.edition).write_text(json.dumps(doc, indent=1, ensure_ascii=False) + "\n", encoding="utf-8",
                                           newline="\n")
        print(f"wrote {data_path(args.edition).relative_to(REPO).as_posix()}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
