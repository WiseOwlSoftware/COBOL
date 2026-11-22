#!/usr/bin/env python3
"""The status line of one edition's suite: how far it is from complete (DESIGN-conformance-suite.md §8 Stage 0, kb/Work PB6047).

    python conformance-suite/harness/suite_status.py --edition 85
    python conformance-suite/harness/suite_status.py --self-test

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
    ap.add_argument("--edition", help="the edition's spelling, as 85")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.edition:
        ap.error("--edition names the suite to measure (or --self-test)")
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


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    import tempfile
    failures: list[str] = []

    def check(what: str, ok: bool, detail: Any = "") -> None:
        print(f"  {'ok  ' if ok else 'FAIL'} {what}" + ("" if ok else f": {detail}"))
        if not ok:
            failures.append(what)

    import measurements
    import obligations
    sys.dont_write_bytecode = True  # a stub rewritten within a second at the same size must not be read from a stale .pyc
    importlib.invalidate_caches()
    zero = dict.fromkeys(obligations.WITNESS, 0) | {"rows": 0, "observable": 0, "unmet": 0}
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        (root / "harness").mkdir()
        (root / "baselines").mkdir()
        ledger = root / "LEDGER.json"
        ledger.write_text(json.dumps({"book_pages": 3, "second_pass": {"pages_swept": [1, 2, 3], "findings": 0}}), encoding="utf-8")
        recorded = {"85": {"verdicts": {"a": "PASS", "b": "FAIL"},
                           "failures": {"b": {"bucket": "output: x", "cause": "non-portable-test"}}}}
        (root / measurements.DIRECTORY).mkdir()
        (root / measurements.DIRECTORY / f"{SECOND_COMPILER}.json").write_text(
            json.dumps({"compiler": SECOND_COMPILER, "editions": recorded}), encoding="utf-8")

        def terms(rows: dict[str, int] | None = None, population: set[str] | None = None, led: Path | None = ledger) -> dict[str, int | str]:
            return status("85", rows=lambda: rows or zero, population=lambda: population or {"a", "b"}, root=root, ledger=led)[0]

        # Each term planted red on its own, the other four satisfied by a stub tool.
        (root / "harness" / "coverage.py").write_text("def unmet(edition):\n    return 0\n", encoding="utf-8")
        (root / "harness" / "crosswalk.py").write_text("def unmet(edition):\n    return 0\n", encoding="utf-8")
        sys.path.insert(0, str(root / "harness"))
        try:
            green = terms()
            check("arm: every term zero is COMPLETE", verdict_line("85", green) == "=== SUITE 85: COMPLETE ===", green)
            check("arm: rows red", verdict_line("85", terms(rows=zero | {"unmet": 4, "observable": 9})).startswith(
                "=== SUITE 85: INCOMPLETE — rows 4 ·"))
            check("arm: second compiler red for an unrecorded test", terms(population={"a", "b", "c"})["second compiler"] == 1)
            recorded["85"]["failures"]["b"]["cause"] = measurements.UNCLASSIFIED
            (root / measurements.DIRECTORY / f"{SECOND_COMPILER}.json").write_text(
                json.dumps({"compiler": SECOND_COMPILER, "editions": recorded}), encoding="utf-8")
            check("arm: second compiler red for a failure with no cause", terms()["second compiler"] == 1)
            ledger.write_text(json.dumps({"book_pages": 3, "second_pass": {"pages_swept": [1, 2], "findings": 2}}), encoding="utf-8")
            check("arm: transcription red for findings and unswept pages", terms()["transcription"] == 3, terms()["transcription"])
            (root / "harness" / "coverage.py").write_text("def unmet(edition):\n    return 5\n", encoding="utf-8")
            sys.modules.pop("coverage", None)
            importlib.invalidate_caches()
            check("arm: points red", terms()["points"] == 5, terms()["points"])
            (root / "harness" / "crosswalk.py").write_text("def unmet(edition):\n    return 2\n", encoding="utf-8")
            sys.modules.pop("crosswalk", None)
            importlib.invalidate_caches()
            check("arm: NIST red", terms()["NIST"] == 2, terms()["NIST"])
            (root / "harness" / "crosswalk.py").write_text("x = 1\n", encoding="utf-8")
            sys.modules.pop("crosswalk", None)
            try:
                terms()
                check("arm: a tool that cannot answer fails loudly", False, "no error")
            except StatusError:
                check("arm: a tool that cannot answer fails loudly", True)
        finally:
            sys.path.remove(str(root / "harness"))
            for name in ("coverage", "crosswalk"):
                sys.modules.pop(name, None)
        (root / "harness" / "coverage.py").unlink()
        (root / "harness" / "crosswalk.py").unlink()
        absent = terms(led=None)
        check("arm: a term whose tool does not exist prints UNMEASURED, never 0",
              absent["points"] == UNMEASURED and absent["NIST"] == UNMEASURED and absent["transcription"] == UNMEASURED, absent)
        check("arm: an unmeasured term keeps the line INCOMPLETE even when every number is zero",
              verdict_line("85", {"rows": 0, "points": UNMEASURED, "NIST": 0, "second compiler": 0,
                                  "transcription": 0}).startswith("=== SUITE 85: INCOMPLETE"))
        ledger.write_text(json.dumps({"book_pages": 3}), encoding="utf-8")
        check("arm: a ledger with no second pass is UNMEASURED", transcription_term(ledger) == UNMEASURED)
        ledger.write_text(json.dumps({"book_pages": 3, "second_pass": {"findings": "none"}}), encoding="utf-8")
        try:
            transcription_term(ledger)
            check("arm: a malformed second pass fails loudly", False, "no error")
        except StatusError:
            check("arm: a malformed second pass fails loudly", True)
    print(f"=== SUITE STATUS: SELF-TEST {'GREEN' if not failures else 'RED'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
