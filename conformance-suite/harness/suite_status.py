#!/usr/bin/env python3
"""The status line of one edition's suite: how far it is from complete (DESIGN-conformance-suite.md §8 Stage 0, kb/Work PB6047).

    python conformance-suite/harness/suite_status.py --edition 85

Five terms, each read from its own tool and each a count of what is still owed:

    rows            observable catalog rows no cleared test witnesses              obligations.py (PB6048)
    points          coverage points no test credits                                coverage.py `unmet(edition)` (PB6050)
    NIST            units neither dispositioned nor subsumed                       crosswalk.py `unmet(edition)` (PB6049)
    second compiler tests with no recorded verdict + failures with no cause       measurements.py (PB6054)
    transcription   the findings of the last full second pass of the edition's     the ledger's `second_pass` (PB6051):
                    text, or the pages it has not swept                            {"pages_swept": [...], "findings": n}

A term whose tool does not exist yet prints UNMEASURED, never 0. A tool that exists and cannot answer fails loudly
(exit 2). The line reads `=== SUITE 85: COMPLETE ===` only when every term is a number and every number is zero;
otherwise `=== SUITE 85: INCOMPLETE ...` with each term, and exit status 1.
"""
from __future__ import annotations

import argparse
import importlib
import json
import sys
from pathlib import Path
from typing import Any, Callable

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
REPO = SUITE.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(Path(__file__).resolve().parents[2] / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

UNMEASURED = "UNMEASURED"
#: edition -> the ledger of its text's reconciliation against the rendered pages (spec-reconciliation README).
LEDGERS = {"85": REPO / "docs/rearchitecture/spec-reconciliation/LEDGER-1985.json"}
#: the second-compiler measurement's compiler (adapters/<compiler>.json), read for every edition it records
SECOND_COMPILER = "gnucobol"
TERMS = ("rows", "points", "NIST", "second compiler", "transcription")


class StatusError(Exception):
    """An input a tool cannot resolve: the status line fails loudly rather than print a number it cannot stand behind."""


def module_term(module: str, edition: str, root: Path) -> int | str:
    """A term another tool owns: UNMEASURED while `<root>/harness/<module>.py` does not exist, else its `unmet(edition)`."""
    if not (root / "harness" / f"{module}.py").is_file():
        return UNMEASURED
    mod = importlib.import_module(module)
    unmet = getattr(mod, "unmet", None)
    if not callable(unmet):
        raise StatusError(f"harness/{module}.py exists but defines no unmet(edition): the status line cannot read its term")
    n = unmet(edition)
    if not isinstance(n, int) or n < 0:
        raise StatusError(f"{module}.unmet({edition!r}) returned {n!r}, not a count")
    return n


def transcription_term(ledger: Path | None) -> int | str:
    """The findings of the last full second pass of the text (PB6051), plus its pages not yet swept; UNMEASURED while
    the ledger records no second pass."""
    if ledger is None or not ledger.is_file():
        return UNMEASURED
    data = json.loads(ledger.read_text(encoding="utf-8"))
    second = data.get("second_pass")
    if second is None:
        return UNMEASURED
    if not isinstance(second, dict) or not isinstance(second.get("findings"), int) \
            or not isinstance(second.get("pages_swept"), list) or not isinstance(data.get("book_pages"), int):
        raise StatusError(f"{ledger.name}: second_pass is {{pages_swept: [...], findings: n}} beside book_pages")
    unswept = data["book_pages"] - len(set(second["pages_swept"]) & set(range(1, data["book_pages"] + 1)))
    return second["findings"] + unswept


def second_compiler_term(root: Path, edition: str, population: set[str]) -> int:
    import measurements
    m = measurements.unmet(root, SECOND_COMPILER, edition, population)
    return m["unrecorded"] + m["unclassified"]


def verdict_line(edition: str, terms: dict[str, int | str]) -> str:
    if all(isinstance(v, int) and v == 0 for v in terms.values()):
        return f"=== SUITE {edition}: COMPLETE ==="
    parts = " · ".join(f"{name} {terms[name]}" for name in TERMS)
    return f"=== SUITE {edition}: INCOMPLETE — {parts} ==="


def status(edition: str, *, rows: Callable[[], dict[str, int]], population: Callable[[], set[str]],
           root: Path = SUITE, ledger: Path | None = None) -> tuple[dict[str, int | str], dict[str, str]]:
    """The five terms of `edition` and a line of detail for each."""
    counts = rows()
    terms: dict[str, int | str] = {"rows": counts["unmet"]}
    detail = {"rows": f"{counts['unmet']} of {counts['observable']} observable rows have no cleared test "
                      f"(accepted-uncleared {counts['accepted-uncleared']}, refusal-only {counts['refusal-only']}, "
                      f"other-edition-only {counts['other-edition-only']}, header-omits {counts['header-omits']}, "
                      f"shared-test {counts['shared-test']}, no-test {counts['no-test']})"}
    terms["points"] = module_term("coverage", edition, root)
    terms["NIST"] = module_term("crosswalk", edition, root)
    ids = population()
    terms["second compiler"] = second_compiler_term(root, edition, ids)
    import measurements
    m = measurements.unmet(root, SECOND_COMPILER, edition, ids)
    detail["second compiler"] = (f"{m['unrecorded']} of {m['population']} tests have no {SECOND_COMPILER} verdict, "
                                 f"{m['unclassified']} recorded failures have no cause")
    terms["transcription"] = transcription_term(ledger)
    why = {"points": "coverage.py (PB6050) does not exist yet", "NIST": "crosswalk.py (PB6049) does not exist yet",
           "transcription": "the ledger records no second pass (PB6051)"}
    for name in why:
        detail[name] = f"{UNMEASURED}: {why[name]}" if terms[name] == UNMEASURED else f"{terms[name]} owed"
    return terms, detail


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--edition", required=True, help="the edition's spelling, as 85")
    args = ap.parse_args(argv)
    import measurements
    import obligations
    try:
        facts, cases, inputs = obligations.facts_of_repository(args.edition)
        held = measurements.populations(cases, inputs.schema)
        if args.edition not in held:
            raise StatusError(f"{args.edition} is no held edition older than the newest ({', '.join(held)})")
        terms, detail = status(args.edition, rows=lambda: obligations.counts(facts), population=lambda: held[args.edition],
                               ledger=LEDGERS.get(args.edition))
    except (StatusError, RuntimeError, ValueError) as e:
        print(f"=== SUITE {args.edition}: ERROR — {e} ===")
        return 2
    for name in TERMS:
        print(f"  {name:<16} {detail[name]}")
    line = verdict_line(args.edition, terms)
    print(line)
    return 0 if line == f"=== SUITE {args.edition}: COMPLETE ===" else 1


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
