#!/usr/bin/env python3
"""run_drafts.py - THE compiler run of the edition audit's drafts (kb/Work PB4995; DESIGN-spec-conformance-review.md §10.2).

The audit's `drafts` pass (audit_batch.py) wrote, for each 1985 rule that is a row of its own, a minimal program and the
outcome the rule requires: the compiler must REFUSE it, or it must run and DISPLAY the exact lines given. This script asks
the compiler, not an agent: it compiles each draft at the audit's edition and runs it, through the conformance suite's
harness and its compiler adapter (conformance-suite/harness/run.py `Run`, the one place that knows how to compile, run and
compare a program), and records whether the outcome MATCHES the draft's expectation:

    python scripts/spec/run_drafts.py --state DIR [--cobol PATH] [--edition 85] [--jobs 8]   # DIR/drafts/runs.json
    python scripts/spec/run_drafts.py --self-test

A MATCH is a candidate witness of the rule's verdict at that edition, once the draft is checked against the rule text (a
draft is a model's, so it is never a witness unchecked: the fleet checks it and places it in the conformance suite). A
MISMATCH is a candidate defect, or a suspect draft, for the fleet to tell apart. NO VERDICT is a crash, a hang, or a
program the harness could not run. The record names the compiler it ran (its commit), so a later run on another build is
a new record, never a silent merge with this one.
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import shutil
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor
from typing import Any

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(REPO / "conformance-suite" / "harness"))
import config  # noqa: E402  (the conformance harness's adapter and profile formats)
import run as harness  # noqa: E402  (THE compile, run and compare of one program: harness.Run)
from audit_batch import Pass  # noqa: E402  (the audit run's passes)
from message_batches import write_atomic  # noqa: E402
sys.path.append(str(REPO / "scripts"))
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "run_drafts"
MATCH, MISMATCH, NO_VERDICT = "MATCH", "MISMATCH", "NO VERDICT"
#: The adapter and profile the drafts run through: WiseOwl COBOL's (conformance-suite/adapters/wiseowl.json).
ADAPTER = REPO / "conformance-suite" / "adapters" / "wiseowl.json"
DEFAULT_COBOL = REPO / "src" / "WiseOwl.Cobol.Cli" / "bin" / "Debug" / "net10.0" / ("cobol.exe" if sys.platform == "win32"
                                                                                      else "cobol")


def entry_of(rule_id: str, draft: dict[str, Any]) -> dict[str, Any]:
    """A draft as a conformance-harness entry (the manifest's shape, DESIGN-conformance-suite.md §4): its source and its
    expected output under `cases/drafts/` of the run's scratch root."""
    stem = "d" + rule_id.replace(".", "_").replace("-", "_")
    return {"id": stem, "clause": "drafts", "source": f"cases/drafts/{stem}.cob", "source-format": "fixed",
            "rules": [rule_id], "files": [], "requires": [], "words": [], "expect": "accept", "implementor-defined": [],
            "expected-output": {"": f"cases/drafts/{stem}.out"}}


def judge(run: harness.Run, edition: str, rule_id: str, draft: dict[str, Any]) -> dict[str, Any]:
    """One draft's outcome against its expectation."""
    entry = entry_of(rule_id, draft)
    src = run.root / entry["source"]
    src.parent.mkdir(parents=True, exist_ok=True)
    src.write_text(draft["program"].rstrip("\n") + "\n", encoding="utf-8", newline="\n")
    expected = draft["expected"]
    (run.root / entry["expected-output"][""]).write_text("\n".join(expected.get("output") or []), encoding="utf-8",
                                                          newline="\n")
    work = run.work / entry["id"]
    try:
        if expected["outcome"] == "output":
            verdict, reason = run.accept(edition, entry, work)
            got = {harness.PASS: MATCH, harness.FAIL: MISMATCH}.get(verdict, NO_VERDICT)
            return {"expected": "output", "verdict": got, "reason": reason}
        source, program = run.stage(entry, work)
        step = run.operation("compile", edition, entry, work, source, program)
        if why := run.unjudgeable(step, "the compiler"):
            return {"expected": "refused", "verdict": NO_VERDICT, "reason": why}
        if step.returncode == 0:
            return {"expected": "refused", "verdict": MISMATCH, "reason": "it translated"}
        return {"expected": "refused", "verdict": MATCH, "reason": harness.first_lines(step.text())}
    except (harness.HarnessError, OSError) as e:
        return {"expected": expected["outcome"], "verdict": NO_VERDICT, "reason": f"the harness could not run it: {e}"}
    finally:
        shutil.rmtree(work, ignore_errors=True)


def compiler_commit(cobol: pathlib.Path) -> str:
    """The commit the compiler was built from: the head of the tree that holds it."""
    done = subprocess.run(["git", "-C", str(cobol.parent), "rev-parse", "HEAD"], capture_output=True, text=True)
    return done.stdout.strip() if done.returncode == 0 else "unknown"


def run_drafts(state: pathlib.Path, adapter_path: pathlib.Path, variables: dict[str, str], edition: str, jobs: int,
               commit: str) -> dict[str, Any]:
    drafts = {a["id"]: a["answer"] for a in Pass(state, "drafts").answers().values()}
    if not drafts:
        raise SystemExit(f"{TOOL}: {state} has no drafts answers (audit_batch.py plan, submit, collect --pass drafts)")
    adapter, problems = config.load_adapter(adapter_path)
    if adapter is None:
        raise SystemExit(f"{TOOL}: {'; '.join(problems)}")
    profile, problems = config.load_profile(adapter_path.parents[1] / "profiles" / f"{adapter.profile}.json")
    if profile is None:
        raise SystemExit(f"{TOOL}: {'; '.join(problems)}")
    with tempfile.TemporaryDirectory(prefix="drafts-") as scratch:
        root = pathlib.Path(scratch)
        run = harness.Run(root=root, adapter=adapter, profile=profile, variables={**adapter.variables, **variables},
                          tests={}, work=root / "work", timeout=harness.HANG_GUARD, keep=False)
        with ThreadPoolExecutor(max_workers=max(1, jobs)) as pool:
            got = dict(zip(sorted(drafts), pool.map(lambda rid: judge(run, edition, rid, drafts[rid]), sorted(drafts))))
    return {"compiler": commit, "edition": edition, "runs": got}


def write_record(state: pathlib.Path, record: dict[str, Any]) -> pathlib.Path:
    out = Pass(state, "drafts").dir / "runs.json"
    write_atomic(out, json.dumps(record, indent=1, ensure_ascii=False))
    tally = collections.Counter(r["verdict"] for r in record["runs"].values())
    print(f"{TOOL}: {len(record['runs'])} draft(s) at {record['edition']} on {record['compiler'][:10]}: "
          f"{dict(sorted(tally.items()))} -> {out}", flush=True)
    return out


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    """Planted drafts through the conformance harness's own stub compiler (run.py `_STUB`): a matched output, a mismatched
    output, a matched refusal, a refusal the compiler translates, and a crash."""
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        stub = root / "stubcc.py"
        stub.write_text(harness._STUB, encoding="utf-8", newline="\n")
        (root / "adapters").mkdir()
        (root / "profiles").mkdir()
        (root / "adapters" / "stub.json").write_text(json.dumps(harness._stub_adapter("stub", stub)), encoding="utf-8",
                                                     newline="\n")
        (root / "profiles" / "stub.json").write_text(json.dumps({"compiler": "stub", "editions": ["85"], "modules": {},
                                                                 "elements": {}, "implementor-defined": {}}),
                                                     encoding="utf-8", newline="\n")
        state = root / "run"
        drafts = {"1985-GR-A.1-1": ("*> STUB run out HELLO", "output", ["HELLO"]),
                  "1985-GR-A.1-2": ("*> STUB run out BYE", "output", ["HELLO"]),
                  "1985-SR-A.1-3": ("*> STUB refuse", "refused", None),
                  "1985-SR-A.1-4": ("*> STUB run out X", "refused", None),
                  "1985-SR-A.1-5": ("*> STUB crash", "output", ["X"])}
        p = Pass(state, "drafts")
        p.answers_dir.mkdir(parents=True)
        for n, (rid, (program, outcome, output)) in enumerate(drafts.items()):
            exp = {"outcome": outcome, **({"output": output} if output is not None else {})}
            (p.answers_dir / f"drafts-{n}.json").write_text(json.dumps(
                {"id": rid, "answer": {"program": program, "expected": exp, "basis": "planted"}}), encoding="utf-8", newline="\n")
        record = run_drafts(state, root / "adapters" / "stub.json", {}, "85", 2, "planted")
        got = {rid: r["verdict"] for rid, r in record["runs"].items()}
        ok(got == {"1985-GR-A.1-1": MATCH, "1985-GR-A.1-2": MISMATCH, "1985-SR-A.1-3": MATCH,
                   "1985-SR-A.1-4": MISMATCH, "1985-SR-A.1-5": NO_VERDICT},
           f"output matched and mismatched, a refusal matched and translated, a crash no verdict ({got})")
        ok("HELLO" in record["runs"]["1985-GR-A.1-2"]["reason"] and "refused" in record["runs"]["1985-SR-A.1-3"]["reason"],
           "a mismatch names the differing output; a matched refusal quotes the compiler's diagnostic")
        ok(record["compiler"] == "planted" and record["edition"] == "85", "the record names the compiler and the edition")
        out = write_record(state, record)
        ok(json.loads(out.read_text(encoding="utf-8")) == record and out.parent == p.dir,
           "the record is the run's drafts/runs.json")
        try:
            run_drafts(root / "empty", root / "adapters" / "stub.json", {}, "85", 1, "planted")
            ok(False, "a run with no drafts is refused")
        except SystemExit as e:
            ok("no drafts answers" in str(e), "a run with no drafts is refused")
    print(f"=== RUN_DRAFTS SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Compile and run the edition audit's drafts at their edition.")
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--state", type=pathlib.Path, help="the audit run's directory (its drafts pass)")
    ap.add_argument("--cobol", type=pathlib.Path, default=DEFAULT_COBOL, help="the compiler (default: this tree's Debug build)")
    ap.add_argument("--edition", default="85", help="the edition, as the adapter names it (default: 85)")
    ap.add_argument("--jobs", type=int, default=8)
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.state is None:
        ap.error("--state is required")
    if not a.cobol.is_file():
        raise SystemExit(f"{TOOL}: no compiler at {a.cobol} (dotnet build WiseOwl.Cobol.sln)")
    write_record(a.state, run_drafts(a.state, ADAPTER, {"cobol": str(a.cobol)}, a.edition, a.jobs, compiler_commit(a.cobol)))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
