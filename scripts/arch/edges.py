#!/usr/bin/env python3
"""edges.py — names the kb/Work notes that remove each tolerated type pair of the architecture edges file (kb/Work
PB2417; docs/rearchitecture/DESIGN-architecture-review.md §8.1).

WHY. `docs/rearchitecture/architecture-edges.json` lists every cross-layer type pair the layers do not allow, and the
drift test (`ArchitectureEdgesDriftTests`) fails a pair whose `removedBy` is empty: a tolerated pair with no remover is
a dependency nobody is going to remove (the seventh refuter, J1). The drift test's SEED mode writes the pairs from the
walk; this script names their removers, because a remover is a fact about the register (which note's wave edits the
code that holds the reference), not about the assemblies.

HOW (`--assign`). A tolerated row's candidates are the notes R1's plan for it names (`plan`, the §8.1 account, every
`PBnnnn`). A Move or Extract that ends the pair A -> B edits the file that declares A — the reference is cut there, or A
moves with its file, or B moves and A's `using` changes — so a candidate REACHES the pair iff its COMPUTED file set
(`member_index.note_file_set`: its sites plus every caller of what it moves or changes, DESIGN-architecture-review §8.7)
holds a file that declares A (the member index's `types`); and of the candidates that reach it, the ones that NAME A or B
(A, B or a member of either among the names it moves or changes, `note_file_set`'s `names`) are its removers, or, when
none names either end, every candidate that reaches it (a wave that rewrites A's file without naming the pair's types,
such as a pass-schedule move). That is the file-level image of the targets file's move maps, which hold no type ->
namespace map (`docs/rearchitecture/evidence/arch-census/targets/`, schema 2: one step carries member moves, none a type
move). The removers are written in the candidates' `blocked_by` order, the row's `removedBy` is their union, and a pair
no candidate reaches keeps an empty list: the drift test stays red on it until a note that removes it is filed and
named (the script never writes the register, CLAUDE.md rule 8). A pair that already names its removers keeps them;
`--all` recomputes every pair.

Usage:
    python scripts/arch/edges.py --assign [--all] [--cache <dir>]   # --cache: the member index of THIS tree
    python scripts/arch/edges.py --self-test

The last line printed is the verdict: `=== ARCH EDGES: ASSIGNED … ===`, `=== ARCH EDGES: UNASSIGNED … ===` (exit 1).
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import member_index  # noqa: E402  (THE note file set and the type -> declaring files map; kb/Work PB3242)
sys.path.append(str(Path(__file__).resolve().parents[1] / "spec"))
import work  # noqa: E402  (the register's one reader)
sys.path.append(str(Path(__file__).resolve().parents[1]))
from standard_streams import ensure_utf8  # noqa: E402

EDGES = Path("docs/rearchitecture/architecture-edges.json")
NOTE_ID = re.compile(r"\bPB\d+\b")
REMOVES_LINE = re.compile(r"^\*\*Removes\b[^\n]*", re.M)


def candidates(row: dict) -> list[str]:
    """The notes R1's plan for a row names, in their first-mention order."""
    return list(dict.fromkeys(NOTE_ID.findall(row.get("plan") or "")))


def blocked_by_order(ids: list[str], blocked_by: dict[str, list[str]]) -> list[str]:
    """`ids` with every note after the notes it waits for (through `blocked_by`, transitively), else in given order."""
    def waits(a: str, b: str, seen: set[str]) -> bool:
        for d in blocked_by.get(a, []):
            if d == b or (d not in seen and waits(d, b, seen | {d})):
                return True
        return False

    out: list[str] = []
    for i in ids:
        at = len(out)
        for k, j in enumerate(out):
            if waits(j, i, set()):
                at = k
                break
        out.insert(at, i)
    return out


def declaring_files(index: dict, metadata_name: str) -> set[str]:
    """The files that declare an outermost type, by its metadata name (`Ns.Type`, a nested `+` written `.`)."""
    return {index["files"][i] for i in index["types"].get(metadata_name.replace("+", "."), [])}


def names_either(names: set[str], pair: dict) -> bool:
    """Whether a note's moved-or-changed names hold either end of a pair, or a member of either."""
    ends = [pair["from"].replace("+", "."), pair["to"].replace("+", ".")]
    return any(n == e or n.startswith(e + ".") or n.startswith(e + "`") for n in names for e in ends)


def assign(edges: dict, file_sets: dict[str, set[str]], names: dict[str, set[str]], index: dict,
           blocked_by: dict[str, list[str]], recompute: bool) -> list[str]:
    """Writes each pair's `removedBy` and each row's union in place; returns the pairs no candidate removes."""
    unassigned = []
    for row in edges["tolerated"]:
        cands = blocked_by_order(candidates(row), blocked_by)
        for pair in row["pairs"]:
            if pair["removedBy"] and not recompute:
                continue
            declared = declaring_files(index, pair["from"])
            reach = [n for n in cands if declared & file_sets.get(n, set())]
            pair["removedBy"] = [n for n in reach if names_either(names.get(n, set()), pair)] or reach
            if not pair["removedBy"]:
                unassigned.append(f"{row['from']} -> {row['to']}: {pair['from']} -> {pair['to']}"
                                  + ("" if declared else " (the member index declares no file for the from type)"))
        row["removedBy"] = sorted({n for p in row["pairs"] for n in p["removedBy"]})
    return unassigned


def unlisted_rows(edges: dict, removes_lines: dict[str, str]) -> list[str]:
    """The (remover, row) pairs whose note's `**Removes …**` line does not name the row (`From → To`): a remover's
    Removes line names every row it removes from (DESIGN-architecture-review §8.1; the notes are rendered from the
    targets file, `file_census_notes.py --rerender`, so a gap is fixed there, never by a hand edit)."""
    return [f"{n}: {row['from']} → {row['to']}" for row in edges["tolerated"] for n in row["removedBy"]
            if f"{row['from']} → {row['to']}" not in removes_lines.get(n, "")]


def write(edges: dict, path: Path) -> None:
    path.write_text(json.dumps(edges, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def self_test() -> int:
    failures = []

    def arm(name: str, ok: bool) -> None:
        print(f"  [{'PASS' if ok else 'FAIL'}] {name}")
        if not ok:
            failures.append(name)

    index = {"files": ["src/A/Binder.cs", "src/B/Emit.cs", "src/C/Other.cs"],
             "types": {"N.A.Binder": [0], "N.B.Emit": [1], "N.B.Emit.Inner": [1], "N.C.Other": [2]}}
    edges = {"tolerated": [{"from": "N.A", "to": "N.B", "plan": "PB2 (the move) and PB1 (the extract); PB3 too",
                            "removedBy": [], "pairs": [
                                {"from": "N.A.Binder", "to": "N.B.Emit", "removedBy": []},
                                {"from": "N.C.Other", "to": "N.B.Emit", "removedBy": []},
                                {"from": "N.A.Binder", "to": "N.B.Kept", "removedBy": ["PB9"]}]}]}
    sets = {"PB1": {"src/A/Binder.cs"}, "PB2": {"src/A/Binder.cs", "src/B/Emit.cs"}, "PB3": {"src/B/Emit.cs"}}
    order = {"PB2": ["PB1"]}
    missing = assign(edges, sets, {}, index, order, recompute=False)
    pairs = edges["tolerated"][0]["pairs"]
    arm("a candidate whose file set holds the from type's declaring file removes the pair, in blocked_by order",
        pairs[0]["removedBy"] == ["PB1", "PB2"])
    arm("a candidate whose file set holds only the to type's file does not", "PB3" not in pairs[0]["removedBy"])
    arm("a pair no candidate's file set reaches stays unassigned and is reported",
        pairs[1]["removedBy"] == [] and len(missing) == 1 and "N.C.Other -> N.B.Emit" in missing[0])
    arm("a pair that already names its removers keeps them", pairs[2]["removedBy"] == ["PB9"])
    arm("the row's removedBy is the union of its pairs'", edges["tolerated"][0]["removedBy"] == ["PB1", "PB2", "PB9"])
    assign(edges, sets, {}, index, order, recompute=True)
    arm("--all recomputes a pair that already names removers", pairs[2]["removedBy"] == ["PB1", "PB2"])
    assign(edges, sets, {"PB2": {"N.B.Emit.Render"}, "PB1": {"N.Z.Elsewhere"}}, index, order, recompute=True)
    arm("of the candidates that reach a pair, the ones naming an end (a member of it here) are its removers",
        pairs[0]["removedBy"] == ["PB2"])
    arm("a pair whose reaching candidates name neither end keeps every one that reaches it",
        pairs[2]["removedBy"] == ["PB1", "PB2"])
    arm("a nested type's metadata name reads the index's dotted key",
        declaring_files(index, "N.B.Emit+Inner") == {"src/B/Emit.cs"})
    arm("the plan's note ids are the candidates, first mention first",
        candidates({"plan": "PB2 then PB1, PB2 again"}) == ["PB2", "PB1"])
    arm("a remover whose Removes line does not name the row is reported",
        unlisted_rows(edges, {"PB1": "**Removes:** `N.A → N.B` 3", "PB2": "**Removes:** `N.X → N.B` 1"}) == ["PB2: N.A → N.B"])
    print(f"=== ARCH EDGES SELF-TEST: {'PASS' if not failures else 'FAIL'} ({len(failures)} failed) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--assign", action="store_true")
    g.add_argument("--self-test", action="store_true")
    ap.add_argument("--all", action="store_true", help="recompute the removers of every pair")
    ap.add_argument("--cache", type=Path, help="read the member index of THIS tree (member_index.current), regenerated here")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    path = REPO / EDGES
    edges = json.loads(path.read_text(encoding="utf-8"))
    index = member_index.current(REPO, a.cache) if a.cache else member_index.load(REPO)
    items = {i["id"]: i for i in work.load()}
    notes = sorted({n for row in edges["tolerated"] for n in candidates(row)})
    file_sets, names, blocked_by, removes = {}, {}, {}, {}
    missing_notes = [n for n in notes if n not in items]
    for n in notes:
        if n in missing_notes:
            continue
        text = (REPO / "kb" / "Work" / f"{n}.md").read_text(encoding="utf-8")
        fs = member_index.note_file_set(text, REPO, index)
        file_sets[n], names[n] = set(fs["files"]), set(fs["names"])
        blocked_by[n] = list(items[n].get("blocked_by") or [])
        removes[n] = "\n".join(REMOVES_LINE.findall(text))
    unassigned = assign(edges, file_sets, names, index, blocked_by, a.all)
    unlisted = unlisted_rows(edges, removes)
    write(edges, path)
    for n in missing_notes:
        print(f"  ⛔ a plan names {n}, which kb/Work does not hold")
    for u in unassigned:
        print(f"  no remover: {u}")
    for u in unlisted:
        print(f"  ⛔ a remover's Removes line does not name its row (re-render it from the targets file): {u}")
    pairs = sum(len(r["pairs"]) for r in edges["tolerated"])
    verdict = "UNASSIGNED" if unassigned or unlisted or missing_notes else "ASSIGNED"
    print(f"=== ARCH EDGES: {verdict} {pairs - len(unassigned)} of {pairs} pair(s) (index {index['commit'][:12]}) ===")
    return 1 if verdict == "UNASSIGNED" else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
