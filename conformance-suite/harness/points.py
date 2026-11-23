#!/usr/bin/env python3
"""The coverage-point catalog of the 1985 general formats (DESIGN-conformance-suite.md section 3.4, kb/Work PB6050).

A POINT is one thing a general format lets a program do that a test can show: an alternative of a brace or of a bracket of
stacked rows written, a bracket or an optional word omitted and written, a repeated portion written twice, one choice
indicator's alternative written alone, and the format itself written (section 5.2.6 of the 2023 text, which is the 1985
text's notation too: section II.2.1 of the held 1985 text). The catalog is the points of the 213 FMT rows, read from the decided
figures (`docs/rearchitecture/spec-reconciliation/figures-1985/`, as `specs/ANSI_COBOL_1985.md` holds them) by `formats.py`,
and it is committed as `scripts/spec/data/format-points-1985.json` so CI checks its shape and digest without the scan.

    python conformance-suite/harness/points.py --edition 85 --write     regenerate the committed catalog
    python conformance-suite/harness/points.py --edition 85 --check     the committed catalog is what the transcription yields
    python conformance-suite/harness/points.py --edition 85 --report    points per row

The catalog binds each row to the grammar rules whose parse nodes carry its formats (`FRAMES`, held by a drift check: every
row has one) and each operand word to the kinds of item it matches (`coverage.OPERAND_KINDS`); `coverage.py` reads both.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
REPO = SUITE.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams
import formats  # noqa: E402
import spec_pdf  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

CATALOG = REPO / "scripts" / "spec" / "data" / "format-points-1985.json"
TRANSCRIPTION = REPO / "specs" / "ANSI_COBOL_1985.md"
EDITION = 1985


#: A clause heading, as `sweep_figures.CLAUSE_LINE` reads it but with the full stop the scan prints after one clause number
#: (`3.2.2. General Format`, page 396), which that reading does not match and which names the same clause.
CLAUSE_HEADING = re.compile(rf"^\s*#*\s*[*{spec_pdf.BOX_SIDE}]*((?:\d+|[A-Z])(?:\.\d+)+)\.?\s+(.+?)[*{spec_pdf.BOX_SIDE}]*\s*$")


class CatalogError(Exception):
    """A figure or row the catalog cannot resolve: it fails loudly, since a catalog with a row missing counts no points for it."""


@dataclass(frozen=True)
class Figure:
    row: str            # the FMT row's rule id
    number: int         # 1-based among the row's figures
    page: int
    ordinal: int        # on its PDF page
    lines: tuple[str, ...]


def _sweep():
    import sweep_figures
    return sweep_figures


def figures(edition: int = EDITION) -> tuple[list[Figure], list[str]]:
    """Every general-format figure of the transcription that belongs to an FMT row, in document order, and the rows no figure
    reached. A figure a refuter decided is no format (`not-a-format`) is none; a figure under no FMT row's heading (the
    composite skeleton's, pages 194-232, which repeat the module formats) is none."""
    import inventory_view
    sf = _sweep()
    lines = TRANSCRIPTION.read_text(encoding="utf-8").split("\n")
    drawn = sf.drawn_figures(lines, sf.page_furniture(edition))
    ordinals = sf.figure_ordinals(drawn)
    verdicts = sf.load_verdicts(sf.verdicts_dir(edition))
    rows = {(r["section"].split(".", 1)[1], r["page"]): r["rule-id"]
            for r in inventory_view.inventory(edition) if r["kind"] == "FMT"}
    # the page each clause heading stands on, in document order: a figure's row is the heading above it, on its page
    headings, page = [], None
    for i, line in enumerate(lines):
        if (m := spec_pdf.PAGE_HEADING.match(line)):
            page = int(m.group(1))
        elif (m := CLAUSE_HEADING.match(line)):
            headings.append((i, m.group(1), page))
    starts = [h[0] for h in headings]
    import bisect
    out, seen = [], collections.Counter()
    for d, k in zip(drawn, ordinals):
        v = verdicts.get((d.page, k))
        if not d.general_format or (v is not None and v.decided == "not-a-format"):
            continue
        at = bisect.bisect_left(starts, d.start) - 1
        if at < 0 or (row := rows.get((headings[at][1], headings[at][2]))) is None:
            continue
        seen[row] += 1
        out.append(Figure(row, seen[row], d.page, k, tuple(d.rows)))
    return out, sorted(set(rows.values()) - set(seen))


# ── the points of a tree ─────────────────────────────────────────────────────────────────────────────────────────────

def label(n: formats.Node) -> str:
    """The words a point is named by: the first three words of its subtree joined by `+`, or the operands it holds."""
    words = list(n.words())[:3]
    if words:
        return "+".join(words)
    ops = []

    def walk(x):
        if x.kind == formats.OPERAND:
            ops.append(x.text)
        for k in x.kids:
            walk(k)
    walk(n)
    return "opnd:" + "+".join(ops[:3]) if ops else "empty"


def nullable(n: formats.Node) -> bool:
    """Whether the node can be satisfied by no token at all."""
    if n.kind in (formats.WORD, formats.OPERAND):
        return False
    if n.kind in (formats.OPTIONAL, formats.OPT, formats.BAR):
        return True
    if n.kind == formats.SEQ:
        return all(nullable(k) for k in n.kids)
    if n.kind == formats.ALT:
        return any(nullable(k) for k in n.kids)
    if n.kind == formats.REP:
        return nullable(n.kids[0])
    return False


def number(root: formats.Node) -> list[formats.Node]:
    """The nodes in pre-order: a node's number in this list is its identity in the catalog's points."""
    out: list[formats.Node] = []

    def walk(n):
        out.append(n)
        for k in n.kids:
            walk(k)
    walk(root)
    return out


def points_of(row: str, fig: int, root: formats.Node) -> list[dict]:
    """The points of one format: `{id, event, node}` with `node` the pre-order number the event belongs to."""
    nodes = number(root)
    index = {id(n): i for i, n in enumerate(nodes)}
    base = f"{row}/f{fig}"
    out = [{"id": base, "event": "format", "node": 0}]

    made: set[tuple[int, str]] = {(0, "format")}

    def add(event: str, n: formats.Node, what: str) -> None:
        """One point per (node, event): an optional word that is also an alternative is written once, whoever names it."""
        if (index[id(n)], event) not in made:
            made.add((index[id(n)], event))
            out.append({"id": f"{base}/{event}:{what}", "event": event, "node": index[id(n)]})

    for n in nodes:
        if n.kind == formats.OPTIONAL:
            add("written", n, n.text)
            add("omit", n, n.text)
        elif n.kind == formats.OPT:
            body = n.kids[0]
            add("omit", n, label(body))
            if body.kind != formats.ALT:
                add("written", n, label(body))
        elif n.kind == formats.ALT:
            for alt in n.kids:
                add("written", alt, label(alt))
                if nullable(alt):
                    add("omit", alt, label(alt))
        elif n.kind == formats.REP:
            add("rep", n, label(n.kids[0]))
        elif n.kind == formats.BAR:
            for alt in n.kids:
                add("alone", alt, label(alt))
            add("order", n, label(n))
    # a name the format gives twice is told apart by its place, in reading order
    seen: collections.Counter = collections.Counter()
    for p in out:
        seen[p["id"]] += 1
    count: collections.Counter = collections.Counter()
    for p in out:
        if seen[p["id"]] > 1:
            count[p["id"]] += 1
            p["id"] = f"{p['id']}#{count[p['id']]}"
    return out


# ── the catalog ──────────────────────────────────────────────────────────────────────────────────────────────────────

def build(edition: int = EDITION) -> dict:
    figs, missing = figures(edition)
    if missing:
        raise CatalogError(f"{len(missing)} FMT row(s) reached no general-format figure: {', '.join(missing[:8])}")
    rows: dict[str, list] = collections.defaultdict(list)
    unreadable = []
    for f in figs:
        try:
            if (decided := formats.DECIDED_READINGS.get((f.row, f.number))) is not None:
                tree = formats.from_json(decided)
                if not formats.agrees(list(f.lines), tree):
                    raise formats.Unreadable("the figure no longer carries the words of the reading decided from the printed page")
            else:
                tree = formats.read(list(f.lines))
                if not formats.agrees(list(f.lines), tree):
                    raise formats.Unreadable("the tree lost or doubled a word of its drawing")
        except formats.Unreadable as e:
            unreadable.append(f"{f.row} figure {f.number} (page {f.page}, figure {f.ordinal}): {e}")
            continue
        rows[f.row].append({"format": f.number, "page": f.page, "tree": tree.as_json(),
                            "points": points_of(f.row, f.number, tree)})
    if unreadable:
        raise CatalogError(f"{len(unreadable)} figure(s) read no tree:\n  " + "\n  ".join(unreadable))
    return {"edition": edition, "rows": dict(sorted(rows.items()))}


def digest(catalog: dict) -> str:
    return hashlib.sha256(json.dumps(catalog, sort_keys=True, ensure_ascii=False).encode()).hexdigest()[:12]


def dump(catalog: dict) -> str:
    return json.dumps({**catalog, "digest": digest(catalog)}, indent=1, ensure_ascii=False) + "\n"


def load(path: Path = CATALOG) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    want = data.pop("digest", None)
    if want != digest(data):
        raise CatalogError(f"{path.name} does not match its own digest: regenerate it (points.py --write)")
    return data


def point_ids(catalog: dict) -> set[str]:
    return {p["id"] for fmts in catalog["rows"].values() for f in fmts for p in f["points"]}


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--edition", required=True, choices=["85"])
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true")
    mode.add_argument("--check", action="store_true")
    mode.add_argument("--report", action="store_true")
    args = ap.parse_args(argv)
    try:
        catalog = build()
    except CatalogError as e:
        print(f"=== POINT CATALOG 85: ERROR — {e} ===")
        return 2
    n = sum(len(f["points"]) for fm in catalog["rows"].values() for f in fm)
    if args.write:
        CATALOG.write_text(dump(catalog), encoding="utf-8", newline="\n")
        print(f"wrote {CATALOG.relative_to(REPO)}")
    elif args.check:
        if not CATALOG.is_file() or CATALOG.read_text(encoding="utf-8") != dump(catalog):
            print(f"=== POINT CATALOG 85: STALE — {CATALOG.name} is not what the transcription yields; run points.py --write ===")
            return 1
    else:
        for row, fm in catalog["rows"].items():
            print(f"{row:<28} {len(fm)} format(s) {sum(len(f['points']) for f in fm):>4} point(s)")
    print(f"=== POINT CATALOG 85: {len(catalog['rows'])} rows, {sum(len(f) for f in catalog['rows'].values())} formats, {n} points ===")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
