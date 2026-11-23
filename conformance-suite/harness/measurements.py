#!/usr/bin/env python3
"""A second compiler's verdicts over every test of an edition's catalog rows: the status line's last measured term
(DESIGN-conformance-suite.md §5, §8 Stage 0, kb/Work PB6054).

    python conformance-suite/harness/measurements.py record --compiler gnucobol --edition 85 RESULTS.json [RESULTS.json ...]

`conformance-suite/measurements/<compiler>.json` holds, per edition, the verdict of every test that edition's catalog
rows name and the test accepts there (the POPULATION, `population()`), recorded from `run.py --compiler <c>` results
(`results.json`), and for every FAIL and NO VERDICT its BUCKET and CAUSE:

    verdicts   test id -> PASS | FAIL | NOT APPLICABLE | NO VERDICT   (run.py's vocabulary, config.VERDICTS)
    failures   test id -> {bucket, cause[, citation]}

A bucket is the mechanical key of the failure: for a compile failure the first diagnostic, its quoted names and numbers
normalised; for an output difference the test's clause folder; for a crash or a hang the kind. The CAUSE is the
adjudication Stage 2 makes, one of `gnucobol-nonconformance` (with the 1985 citation that shows it), `non-portable-test`
or `wiseowl-expectation-defect`; a bucket nobody has adjudicated is `unclassified`, and the status line counts it.

This is not `baselines/<compiler>.json`: a baseline names only tests the edition's SUITE holds (a cleared test,
`config.check_tree`), and the measurement covers the tests Stage 2 has yet to clear. `record` also writes the cleared
tests' verdicts into the baseline, so the regression net starts from them. `check` is the generator's refusal of a
measurement that names no test of its population, a verdict outside the vocabulary, a failure with no bucket and a
cause outside the three.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any, Iterable

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
sys.path.insert(0, str(HERE))
sys.path.append(str(Path(__file__).resolve().parents[2] / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import config  # noqa: E402  (this directory's: the verdict vocabulary and the baseline's writer)

VERDICT = "=== MEASUREMENTS: "
DIRECTORY = "measurements"
FAILED = ("FAIL", "NO VERDICT")
UNCLASSIFIED = "unclassified"
CAUSES = (UNCLASSIFIED, "gnucobol-nonconformance", "non-portable-test", "wiseowl-expectation-defect")
FIELDS = frozenset({"$comment", "compiler", "version", "editions"})
EDITION_FIELDS = frozenset({"verdicts", "failures"})
FAILURE_FIELDS = frozenset({"bucket", "cause", "citation"})
#: A citation of the held 1985 text, checked with `python scripts/spec/cite.py --edition 1985 --check` before it is recorded.
CITATION = re.compile(r"^§1985:\S")


def path_of(root: Path, compiler: str) -> Path:
    return root / DIRECTORY / f"{compiler}.json"


def population(cases: Iterable[Any], edition: str, spelling: str) -> list[str]:
    """The ids of the tests of `cases/` that accept `edition` and whose header names a row of that edition's own text
    (a rule id opening `<spelling>-`, as `1985-GR-...`), sorted."""
    return sorted(c.test_id for c in cases
                  if c.tree == "cases" and edition in c.accept and any(r.startswith(f"{spelling}-") for r in c.rules))


def populations(cases: Iterable[Any], schema: Any) -> dict[str, set[str]]:
    """edition spelling -> its population, for every held edition older than the newest (whose catalog is its own)."""
    cases = list(cases)
    return {ed.spelling: set(population(cases, ed.spelling, str(ed.year)))
            for ed in schema.all_editions if ed.held and not ed.newest}


# ── the bucket ───────────────────────────────────────────────────────────────────────────────────────────────────────

_DIAGNOSTIC = re.compile(r"^(?:\S+?:\d+: )?(?:(?:error|warning): )?")
_NOISE = re.compile(r"_FORTIFY_SOURCE|<command-line>|In file included|in paragraph|note: ")


def bucket_of(result: dict[str, Any]) -> str:
    """The mechanical bucket of one failing result of `run.py` (the module docstring); raises on a verdict that has none."""
    reason, verdict = result.get("reason", ""), result["verdict"]
    if verdict == "NO VERDICT":
        if "hang guard" in reason:
            return "hang: the program exceeded the hang guard"
        if "crashed" in reason:
            return "abend: the compiler crashed"
        raise ValueError(f"{result['id']}: a NO VERDICT of no known kind: {reason!r}")
    if verdict != "FAIL":
        raise ValueError(f"{result['id']}: {verdict} has no bucket")
    if reason.startswith("it did not translate"):
        diagnostics = [d.strip() for d in reason.split("): ", 1)[1].split(" | ") if d.strip()]
        # The first diagnostic of the compile: a driver's own note (a redefined macro, a source file name line) is not it.
        first = next((d for d in diagnostics if re.search(r"\b(?:error|warning): ", d) and not _NOISE.search(d)), diagnostics[0])
        message = _DIAGNOSTIC.sub("", first, count=1)
        message = re.sub(r"'[^']*'|‘[^’]*’", "'N'", message)
        return "compile: " + re.sub(r"\d+", "#", message)
    if reason.startswith(("the output differs", "the output file")):
        return f"output: {result['clause']}"
    raise ValueError(f"{result['id']}: a FAIL of no known kind: {reason!r}")


def record_results(results: Iterable[dict[str, Any]], previous: dict[str, Any] | None) -> dict[str, Any]:
    """The `{verdicts, failures}` of one edition from run.py's `tests` entries; an adjudicated cause of an earlier
    record is kept for a test whose bucket is the same."""
    verdicts: dict[str, str] = {}
    failures: dict[str, dict[str, str]] = {}
    old = (previous or {}).get("failures", {})
    for r in results:
        verdicts[r["id"]] = r["verdict"]
        if r["verdict"] in FAILED:
            bucket = bucket_of(r)
            kept = old.get(r["id"])
            failures[r["id"]] = dict(kept) if kept and kept["bucket"] == bucket else {"bucket": bucket, "cause": UNCLASSIFIED}
    return {"verdicts": dict(sorted(verdicts.items())), "failures": dict(sorted(failures.items()))}


# ── reading and checking ─────────────────────────────────────────────────────────────────────────────────────────────

def load(path: Path) -> tuple[dict[str, Any] | None, list[str]]:
    """The measurement file and its problems; a missing file is no measurement (None) and no problem."""
    if not path.is_file():
        return None, []
    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        return None, [f"{path.name}: unreadable: {e}"]
    if not isinstance(raw, dict):
        return None, [f"{path.name}: a measurement is a JSON object"]
    problems = [f"{path.name}: unknown field {k!r}" for k in sorted(set(raw) - FIELDS)]
    if raw.get("compiler") != path.stem:
        problems.append(f"{path.name}: compiler is the file's base name {path.stem!r}, not {raw.get('compiler')!r}")
    editions = raw.get("editions")
    if not isinstance(editions, dict):
        return None, problems + [f"{path.name}: editions maps each edition to {{verdicts, failures}}"]
    for edition, body in editions.items():
        if not isinstance(body, dict) or set(body) != EDITION_FIELDS:
            problems.append(f"{path.name}: {edition}: holds exactly {', '.join(sorted(EDITION_FIELDS))}")
            continue
        verdicts, failures = body["verdicts"], body["failures"]
        for t, v in verdicts.items():
            if v not in config.VERDICTS:
                problems.append(f"{path.name}: {edition} {t}: {v!r} is not one of {', '.join(config.VERDICTS)}")
        for t in sorted(t for t, v in verdicts.items() if v in FAILED and t not in failures):
            problems.append(f"{path.name}: {edition} {t}: a {verdicts[t]} with no bucket and cause")
        for t, f in failures.items():
            if verdicts.get(t) not in FAILED:
                problems.append(f"{path.name}: {edition} {t}: a bucket for a test that did not FAIL")
            if not isinstance(f, dict) or set(f) - FAILURE_FIELDS or not {"bucket", "cause"} <= set(f):
                problems.append(f"{path.name}: {edition} {t}: a failure holds bucket, cause and, for a conformance defect, citation")
                continue
            if f["cause"] not in CAUSES:
                problems.append(f"{path.name}: {edition} {t}: cause {f['cause']!r} is not one of {', '.join(CAUSES)}")
            elif (f["cause"] == "gnucobol-nonconformance") != ("citation" in f):
                problems.append(f"{path.name}: {edition} {t}: a citation goes with the cause gnucobol-nonconformance, and only with it")
            elif "citation" in f and not CITATION.match(f["citation"]):
                problems.append(f"{path.name}: {edition} {t}: the citation names no clause of the held 1985 text (§1985:...)")
    return (None if problems else raw), problems


def check_tree(root: Path, populations: dict[str, set[str]]) -> list[tuple[str, str]]:
    """Every problem of every measurement under `root`, as `(relative path, message)`: its shape and a test no
    population of its edition holds (`populations`: edition -> the ids of that edition's population)."""
    out: list[tuple[str, str]] = []
    for path in sorted((root / DIRECTORY).glob("*.json")):
        raw, problems = load(path)
        rel = f"{DIRECTORY}/{path.name}"
        out += [(rel, p.removeprefix(f"{path.name}: ")) for p in problems]
        for edition, body in ((raw or {}).get("editions") or {}).items():
            if edition not in populations:
                out.append((rel, f"edition {edition!r} has no population"))
                continue
            out += [(rel, f"{edition} {t}: no test of the {edition} population has this id")
                    for t in sorted(set(body["verdicts"]) - populations[edition])]
    return out


def unmet(root: Path, compiler: str, edition: str, population_ids: set[str]) -> dict[str, int]:
    """The second-compiler term of the status line for one edition: the population's tests with no recorded verdict, and
    the recorded failures nobody has classified by cause. A missing measurement leaves every test unrecorded."""
    raw, problems = load(path_of(root, compiler))
    if problems:
        raise ValueError("; ".join(problems))
    body = ((raw or {}).get("editions") or {}).get(edition, {"verdicts": {}, "failures": {}})
    return {"population": len(population_ids),
            "unrecorded": len(population_ids - set(body["verdicts"])),
            "unclassified": sum(1 for f in body["failures"].values() if f["cause"] == UNCLASSIFIED)}


# ── recording ────────────────────────────────────────────────────────────────────────────────────────────────────────

def record(root: Path, compiler: str, edition: str, version: str, results_files: list[Path],
           population_ids: set[str], in_suite: set[str]) -> tuple[dict[str, Any], int]:
    """Write the measurement of `edition` from run.py's results files (a later file's result replaces an earlier one's
    for a test; a test no file holds keeps its recorded verdict while it is in the population), and the verdicts of the
    `in_suite` tests into the compiler's baseline; return the edition's record and the number of baseline verdicts
    written. Refuses a result for a test outside the population."""
    merged: dict[str, dict[str, Any]] = {}
    for f in results_files:
        for r in json.loads(f.read_text(encoding="utf-8"))["tests"]:
            if r["edition"] == edition:
                merged[r["id"]] = r
    stray = sorted(set(merged) - population_ids)
    if stray:
        raise ValueError(f"results for tests outside the {edition} population: {', '.join(stray[:5])}")
    path = path_of(root, compiler)
    previous, problems = load(path)
    if problems:
        raise ValueError("; ".join(problems))
    editions = dict((previous or {}).get("editions", {}))
    prior = editions.get(edition) or {"verdicts": {}, "failures": {}}
    fresh = record_results(merged.values(), prior)
    # A run of some tests re-measures those and keeps the others' recorded verdicts (a test that left the population
    # goes), so a re-measure of the tests a clearing rewrote needs no run of the whole population.
    body = {key: dict(sorted({**{t: v for t, v in prior[key].items() if t not in merged and t in population_ids},
                              **fresh[key]}.items())) for key in ("verdicts", "failures")}
    editions[edition] = body
    content = {
        "$comment": [
            f"{compiler}'s verdicts over the tests each edition's catalog rows name (DESIGN-conformance-suite.md §5, §8 Stage 0),",
            "recorded from `run.py --compiler` results by `harness/measurements.py record`; every failure carries its bucket",
            "and its cause (`unclassified` until Stage 2 adjudicates it), and the status line counts the unclassified.",
        ],
        "compiler": compiler, "version": version,
        "editions": {e: editions[e] for e in sorted(editions, key=int)},
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(_dump(content), encoding="utf-8", newline="\n")
    base_path = root / "baselines" / f"{compiler}.json"
    baseline, problems = config.load_baseline(base_path)
    if problems:
        raise ValueError("; ".join(problems))
    verdicts = {e: dict(t) for e, t in (baseline or {}).items()}
    cleared = {t: v for t, v in body["verdicts"].items() if t in in_suite}
    verdicts.setdefault(edition, {}).update(cleared)
    config.write_baseline(base_path, compiler, verdicts)
    return body, len(cleared)


def _dump(content: dict[str, Any]) -> str:
    """JSON with one line per test: the file is read in diffs, one verdict a line."""
    lines = ["{", f'  "$comment": {json.dumps(content["$comment"], ensure_ascii=False)},',
             f'  "compiler": {json.dumps(content["compiler"])},', f'  "version": {json.dumps(content["version"])},',
             '  "editions": {']
    editions = []
    for e, body in content["editions"].items():
        parts = [f'    {json.dumps(e)}: {{']
        for key in ("verdicts", "failures"):
            entries = [f'        {json.dumps(t)}: {json.dumps(v, ensure_ascii=False)}' for t, v in body[key].items()]
            parts.append(f'      "{key}": {{' + ("\n" + ",\n".join(entries) + "\n      }" if entries else "}")
                         + ("," if key == "verdicts" else ""))
        parts.append("    }")
        editions.append("\n".join(parts))
    lines.append(",\n".join(editions))
    lines += ["  }", "}", ""]
    return "\n".join(lines)


# ── running ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = ap.add_subparsers(dest="command")
    rec = sub.add_parser("record", help="record a run's results as the edition's measurement")
    rec.add_argument("--compiler", required=True)
    rec.add_argument("--edition", required=True)
    rec.add_argument("--version", required=True, help="the compiler's version line, as `cobc --version` prints it")
    rec.add_argument("results", nargs="+", type=Path, help="run.py's results.json, a later file replacing an earlier one")
    args = ap.parse_args(argv)
    if args.command != "record":
        ap.error("a command (record)")
    sys.path.insert(0, str(SUITE.parent / "scripts" / "spec"))
    import generate as g
    inputs, problems = g.repository_inputs()
    rc, reader = g.generate(g.SUITE, inputs, problems, None, echo=False, report=False)
    if rc != 0:
        print(f"{VERDICT}ERROR — generate.py refuses the tree ===")
        return 2
    held = populations(reader.cases, inputs.schema)
    if args.edition not in held:
        print(f"{VERDICT}ERROR — {args.edition} is no held edition older than the newest ({', '.join(held)}) ===")
        return 2
    ids = held[args.edition]
    cleared = {c.test_id for c in reader.cases if c.in_suite and args.edition in c.accept}
    body, written = record(SUITE, args.compiler, args.edition, args.version, args.results, ids, cleared)
    by = {v: sum(x == v for x in body["verdicts"].values()) for v in config.VERDICTS}
    print(f"{VERDICT}RECORDED — {args.compiler} at {args.edition}: {len(body['verdicts'])} of {len(ids)} tests "
          f"({' · '.join(f'{v} {n}' for v, n in by.items())}), {len(body['failures'])} bucketed, "
          f"{written} cleared verdict(s) in the baseline ===")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
