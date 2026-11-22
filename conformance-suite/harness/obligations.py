#!/usr/bin/env python3
"""The 1985 catalog's row obligations and what witnesses each row (DESIGN-conformance-suite.md §6.1, §8 Stage 0, kb/Work PB6048).

    python conformance-suite/harness/obligations.py                    # the 85 rows by witness, one verdict line
    python conformance-suite/harness/obligations.py --repair-headers   # add each omitted row to the header of its test

§6.1 cut to a 1985 row: a GR, RV or AR row owes an accepted test whose output shows the rule's effect; an SR row owes an
accepted test of the legal form (a violation test at 85 is a diagnosis-layer test, §8, and never meets the obligation);
an FMT row owes an accepted test and every coverage point (the points are PB6050's, so this module reports the
accepted-test half and `suite_status.py` reads the points term from its own tool). Each row is counted ONCE, under the
first witness kind that holds:

    non-support          the row's verdict is DOCUMENTED-NON-SUPPORT: nothing to observe until implemented
    suite                a test of the suite (a cleared test) that accepts at 85 names the row in its header `rules`
    determination        a DOC row docs/CONFORMANCE.md §7 determines
    derivation           a standing derivation settles the row (no test owed)
    accepted-uncleared   an accepted-at-85 test names it, none of them cleared yet (Stage 2 clears them)
    refusal-only         only tests WiseOwl refuses or the suite flags at 85 name it: no accepted test yet
    other-edition-only   only tests with no behaviour at 85 name it
    header-omits         no header names it, and its inventory `test-ref` names a test that accepts at 85 ALONE whose
                         header omits it: a header to repair (`--repair-headers`), never an authoring task
    shared-test          no header names it, and the `test-ref` tests that accept at 85 accept another edition too: a 1985
                         row's test runs at 85 alone (§12.1, and the generator refuses a test accepted outside its rules'
                         bands), so the row needs an 85-only test, an authoring obligation (Stage 3)
    no-test           no header names it and no `test-ref` test accepts at 85: an authoring obligation (Stage 3)

The module takes the parsed tests and the inventory's rows and returns facts; `generate.py` stays the one reader of
headers. `suite_status.py` reads the rows term from `classify`.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Iterable

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(SUITE.parent / "scripts" / "spec"))
sys.path.append(str(Path(__file__).resolve().parents[2] / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

VERDICT = "=== OBLIGATIONS 85: "

#: The witness kinds in the order a row is counted under the first that holds.
WITNESS = ("non-support", "suite", "determination", "derivation", "accepted-uncleared", "refusal-only",
           "other-edition-only", "header-omits", "shared-test", "no-test")
#: The kinds that owe no test: a row no program can observe yet, or one a derivation settles.
UNOBSERVABLE = frozenset({"non-support", "derivation"})

#: §6.1's row kinds (DOC rows are the later editions' only). A kind this table lacks is refused, never given a default obligation.
OBLIGATION = {
    "GR": "accepted test whose output shows the rule's effect",
    "RV": "accepted test whose output shows the rule's effect",
    "AR": "accepted test whose output shows the rule's effect",
    "SR": "accepted test of the legal form",
    "FMT": "accepted test and every coverage point",
    "DOC": "a test per value the row enumerates, otherwise the determination docs/CONFORMANCE.md carries",
}
NON_SUPPORT = "DOCUMENTED-NON-SUPPORT"
CONFORMANCE_REF = "conformance:"
MARGIN = {"fixed": 72, "free": 100}  # a wrapped header line stays inside margin R (fixed) or well inside 255 (free)
CONTINUATION = "    "  # generate.CONTINUATION: the indent that marks a field's continuation line


@dataclass(frozen=True)
class RowFacts:
    rule_id: str
    kind: str
    obligation: str
    witness: str
    #: the tests whose header names the row
    tests: tuple[str, ...]
    #: for `header-omits`: the accepted-at-85 tests of the row's `test-ref` whose header must name it
    repair: tuple[str, ...] = ()


def accepts(case: Any, edition: str) -> bool:
    return edition in case.accept


def refused_at(case: Any, diagnostics: dict[str, dict[str, Any]], edition: str) -> bool:
    """Does the suite flag the test at `edition`, or does WiseOwl refuse it there (§3.2)?"""
    if case.flag is not None and edition in case.flag.at:
        return True
    return edition in diagnostics.get(case.test_id, {}).get("refuse-at", ())


def classify(rows: Iterable[dict[str, Any]], cases: Iterable[Any], *, diagnostics: dict[str, dict[str, Any]],
             determined: Callable[[dict[str, Any]], bool], derived: Callable[[dict[str, Any]], bool],
             test_refs: Callable[[dict[str, Any]], list[str]], edition: str = "85") -> list[RowFacts]:
    """Every row of one edition's catalog with its obligation and its witness kind (the module docstring)."""
    cases = list(cases)
    by_id = {c.test_id: c for c in cases}
    named: dict[str, list[Any]] = {}
    for c in cases:
        for rule in c.rules:
            named.setdefault(rule, []).append(c)
    out: list[RowFacts] = []
    for row in rows:
        rule_id, kind = row["rule-id"], row.get("kind")
        if kind not in OBLIGATION:
            raise ValueError(f"{rule_id}: row kind {kind!r} has no obligation in §6.1 (obligations.OBLIGATION)")
        tests = named.get(rule_id, [])
        accepted = [c for c in tests if accepts(c, edition)]
        repair: tuple[str, ...] = ()
        if row.get("verdict") == NON_SUPPORT:
            witness = "non-support"
        elif any(c.in_suite for c in accepted):
            witness = "suite"
        elif determined(row):
            witness = "determination"
        elif derived(row):
            witness = "derivation"
        elif accepted:
            witness = "accepted-uncleared"
        elif any(refused_at(c, diagnostics, edition) for c in tests):
            witness = "refusal-only"
        elif tests:
            witness = "other-edition-only"
        else:
            at_85 = {tid for tid in map(_test_id_of, test_refs(row)) if tid in by_id and accepts(by_id[tid], edition)}
            repair = tuple(sorted(tid for tid in at_85 if tuple(by_id[tid].accept) == (edition,)))
            witness = "header-omits" if repair else "shared-test" if at_85 else "no-test"
        out.append(RowFacts(rule_id, kind, OBLIGATION[kind], witness, tuple(sorted(c.test_id for c in tests)), repair))
    return out


def _test_id_of(ref: str) -> str | None:
    """The test id a `conformance:<path>/<id>` reference names, None for any other reference kind."""
    return ref[len(CONFORMANCE_REF):].rsplit("/", 1)[-1] if ref.startswith(CONFORMANCE_REF) else None


def counts(facts: Iterable[RowFacts]) -> dict[str, int]:
    """Rows per witness kind, plus `rows` and `observable` (the rows a program can observe and no derivation settles)
    and `unmet` (the observable rows no cleared test witnesses yet: the rows term of the status line)."""
    c = dict.fromkeys(WITNESS, 0)
    for f in facts:
        c[f.witness] += 1
    c["rows"] = sum(c[k] for k in WITNESS)
    c["observable"] = c["rows"] - sum(c[k] for k in UNOBSERVABLE)
    c["unmet"] = c["observable"] - c["suite"] - c["determination"]
    return c


def repairs(facts: Iterable[RowFacts]) -> dict[str, list[str]]:
    """test id -> the rows whose `test-ref` names it and whose header omits them, in row order."""
    out: dict[str, list[str]] = {}
    for f in facts:
        for tid in f.repair:
            out.setdefault(tid, []).append(f.rule_id)
    return out


# ── the header repair ────────────────────────────────────────────────────────────────────────────────────────────────

_RULES_LINE = re.compile(r"^(?P<lead>(?:      )?\*>) rules: \[(?P<body>.*)$")


def add_rules(text: str, new_ids: list[str]) -> str:
    """`text` with `new_ids` appended to the `rules` field of its metadata block (generate.py's header grammar), the
    field re-wrapped inside the margin of its form; every other byte unchanged. The ids already named are kept in
    place; one the field already holds is not repeated."""
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(newline)
    start = next((i for i, ln in enumerate(lines) if _RULES_LINE.match(ln)), None)
    if start is None:
        raise ValueError("the header has no single-line-opened `rules: [` field")
    lead = _RULES_LINE.match(lines[start]).group("lead")
    end = start + 1
    while end < len(lines) and lines[end].startswith(lead + CONTINUATION) and lines[end][len(lead):].strip():
        end += 1
    value = " ".join([lines[start][len(lead) + len(" rules:"):].strip(), *(ln[len(lead):].strip() for ln in lines[start + 1:end])])
    if not (value.startswith("[") and value.endswith("]")):
        raise ValueError(f"the `rules` value is not a bracketed list: {value!r}")
    held = [t.strip() for t in value[1:-1].split(",") if t.strip()]
    ids = held + [i for i in new_ids if i not in held]
    limit = MARGIN["fixed" if lead.startswith(" ") else "free"]
    wrapped: list[str] = []
    prefix = f"{lead} rules: ["
    current = prefix
    for n, rid in enumerate(ids):
        piece = rid + ("," if n < len(ids) - 1 else "]")
        fresh = current in (prefix, lead + CONTINUATION)
        if not fresh and len(current) + 1 + len(piece) > limit:
            wrapped.append(current)
            current = lead + CONTINUATION + piece
        else:
            current += ("" if fresh else " ") + piece
    wrapped.append(current)
    return newline.join(lines[:start] + wrapped + lines[end:])


def repair_headers(root: Path, cases: Iterable[Any], todo: dict[str, list[str]]) -> int:
    """Write `todo` (test id -> rows) into the headers of the tests; the number of files changed."""
    by_id = {c.test_id: c for c in cases}
    changed = 0
    for tid, ids in sorted(todo.items()):
        path = root / by_id[tid].path
        text = path.read_bytes().decode("utf-8")
        new = add_rules(text, ids)
        if new != text:
            path.write_bytes(new.encode("utf-8"))
            changed += 1
    return changed


# ── running ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def facts_of_repository(edition: str = "85") -> tuple[list[RowFacts], list[Any], Any]:
    """The facts of the repository's `edition` catalog, with the parsed tests and the generator's inputs; refuses (raises)
    when the generator refuses the tree, since a count over a refused tree is no measure."""
    import generate as g
    import inventory_view
    inputs, problems = g.repository_inputs()
    rc, reader = g.generate(g.SUITE, inputs, problems, None, echo=False, report=False)
    if rc != 0:
        raise RuntimeError("generate.py refuses the tree; run `generate.py --check` and fix it first")
    schema = inputs.schema
    year = int(edition) + 1900 if len(edition) == 2 else int(edition)
    rows = inventory_view.inventory(year, schema)
    facts = classify(rows, reader.cases, diagnostics=inputs.diagnostics, determined=inputs.determined,
                     derived=inputs.derived,
                     test_refs=lambda row: schema.split(row.get("test-ref", ""), schema.test_ref_sep), edition=edition)
    return facts, reader.cases, inputs


def verdict_line(c: dict[str, int]) -> str:
    kinds = ", ".join(f"{k} {c[k]}" for k in WITNESS)
    return (f"{VERDICT}{c['unmet']} of {c['observable']} observable rows lack a cleared test ({c['rows']} rows: {kinds}) ===")


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--repair-headers", action="store_true",
                    help="add every `header-omits` row to the header of each accepted-at-85 test its test-ref names")
    args = ap.parse_args(argv)
    facts, cases, _ = facts_of_repository()
    if args.repair_headers:
        todo = repairs(facts)
        changed = repair_headers(SUITE, cases, todo)
        print(f"  repaired {sum(len(v) for v in todo.values())} row name(s) in {changed} header(s)")
        return 0
    print(verdict_line(counts(facts)))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
