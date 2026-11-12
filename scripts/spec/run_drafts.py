#!/usr/bin/env python3
"""run_drafts.py - THE compiler run of the edition audit's drafts (kb/Work PB4995; DESIGN-spec-conformance-review.md §10.2).

The audit's `drafts` pass (audit_batch.py) wrote, for each 1985 rule that is a row of its own, a minimal program and the
outcome the rule requires: the compiler must REFUSE it, or it must run and DISPLAY the exact lines given. This script asks
the compiler, not an agent: it runs each draft at the audit's edition as a conformance-suite test, through the suite's
harness and its compiler adapter (conformance-suite/harness/run.py `Run`, the ONE judge of a program's outcome), and records
whether the outcome MATCHES the draft's expectation:

    python scripts/spec/run_drafts.py --state DIR [--cobol PATH] [--edition 85] [--jobs 8]   # DIR/drafts/runs.json
    python scripts/spec/run_drafts.py --self-test

A draft that must run is an ACCEPTED test (`Run.accept`: it compiles, runs, and DISPLAYs exactly the lines given). A draft
the compiler must refuse is a FLAGGED test (`Run.flagged`), so a refusal counts only when it is the rule's: the draft names
the line of its program that violates the rule and carries a control, the same program with that line made conforming; the
control must compile and the program must draw a diagnostic AT that line. A refusal for another reason (a diagnostic only on
other lines) is a MISMATCH, and so is a control the compiler refuses. Every stored draft carries both: the drafts pass's
answer shape requires them (audit_batch.py `parse_answer`), and a draft stored before it did is withdrawn and drafted again
(`audit_batch.py withdraw`, kb/Work PB4995), so no draft is judged without them.

A MATCH is a candidate witness of the rule's verdict at that edition, once the draft is checked against the rule text (a
draft is a model's, so it is never a witness unchecked: the fleet checks it and places it in the conformance suite). A
MISMATCH is a candidate defect, or a suspect draft, for the fleet to tell apart. NO VERDICT is a crash, a hang, or a
program the harness could not run. The record names the compiler binary it ran (`compiler_identity`: its version and the
digest of its assemblies, never a checkout's HEAD), so a run on another build is a new record, never a silent merge.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
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
import run as harness  # noqa: E402  (THE compile, run and judge of one program: harness.Run)
from audit_batch import Pass, answers_by_id, custom_id, draft_problem, write_plan  # noqa: E402  (the audit run's passes)
from message_batches import write_atomic  # noqa: E402
sys.path.append(str(REPO / "scripts"))
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "run_drafts"
MATCH, MISMATCH, NO_VERDICT = "MATCH", "MISMATCH", "NO VERDICT"
#: The harness's verdict of a draft's test as the draft's: a control the compiler refuses (the harness's NOT APPLICABLE) is
#: a MISMATCH here, since a draft's control is legal 1985 by its own claim (a refused control is a defect or a bad draft).
VERDICT_OF = {harness.PASS: MATCH, harness.FAIL: MISMATCH, harness.NOT_APPLICABLE: MISMATCH}
#: The adapter and profile the drafts run through: WiseOwl COBOL's (conformance-suite/adapters/wiseowl.json).
ADAPTER = REPO / "conformance-suite" / "adapters" / "wiseowl.json"
DEFAULT_COBOL = REPO / "src" / "WiseOwl.Cobol.Cli" / "bin" / "Debug" / "net10.0" / ("cobol.exe" if sys.platform == "win32"
                                                                                      else "cobol")


def tests_of(root: pathlib.Path, rule_id: str, draft: dict[str, Any]) -> list[dict[str, Any]]:
    """A draft as conformance-harness tests (the manifest's shape, DESIGN-conformance-suite.md §4), its files written in
    a folder of its own, `cases/drafts/<stem>/` of the run's scratch root: the draft's own test first, then its control
    when it has one. An output draft is an accepted test; a refused draft a flagged test at the line it names, with its
    control. The draft's library texts (`files`, audit_batch.py `draft_problem`) lie beside its programs and both tests
    declare them, so the harness stages them for the program and its control alike (kb/Work PB5067). A draft that is no
    draft is refused, naming the rule."""
    why = draft_problem(draft)
    if why:
        raise SystemExit(f"{TOOL}: the draft of {rule_id}: {why}")
    stem = "d" + rule_id.replace(".", "_").replace("-", "_")
    folder = f"cases/drafts/{stem}"
    expected = draft["expected"]
    (root / folder).mkdir(parents=True, exist_ok=True)
    declared = []
    for name, text in sorted(draft.get("files", {}).items()):
        (root / folder / name).parent.mkdir(parents=True, exist_ok=True)
        (root / folder / name).write_text(text, encoding="utf-8", newline="\n")
        declared.append(f"{folder}/{name}")

    def test(test_id: str, program: str, **shape: Any) -> dict[str, Any]:
        source = f"{folder}/{test_id}.cob"
        (root / source).write_text(program.rstrip("\n") + "\n", encoding="utf-8", newline="\n")
        return {"id": test_id, "clause": "drafts", "source": source, "source-format": "fixed", "rules": [rule_id],
                "files": declared, "requires": [], "words": [], **shape}

    if expected["outcome"] == "output":
        (root / f"{folder}/{stem}.out").write_text("\n".join(expected["output"]), encoding="utf-8", newline="\n")
        return [test(stem, draft["program"], expect="accept", **{"implementor-defined": [],
                                                                   "expected-output": {"": f"{folder}/{stem}.out"}})]
    control = test(f"{stem}_control", expected["control"], expect="accept", **{"implementor-defined": [],
                                                                               "expected-output": {}})
    return [test(stem, draft["program"], expect="flag", flag="syntax", line=expected["line"], control=control["id"]),
            control]


def judge(run: harness.Run, edition: str, test: dict[str, Any]) -> dict[str, Any]:
    """One draft's outcome against its expectation, as the harness judges its test."""
    work = run.work / test["id"]
    try:
        if test["expect"] == "accept":
            verdict, reason = run.accept(edition, test, work)
            return {"expected": "output", "verdict": VERDICT_OF.get(verdict, NO_VERDICT), "reason": reason}
        verdict, reason = run.flagged(edition, test, work)
        return {"expected": "refused", "verdict": VERDICT_OF.get(verdict, NO_VERDICT),
                "reason": reason or f"a diagnostic at the violating line {test['line']}"}
    except (harness.HarnessError, OSError) as e:
        return {"expected": "output" if test["expect"] == "accept" else "refused", "verdict": NO_VERDICT,
                "reason": f"the harness could not run it: {e}"}
    finally:
        shutil.rmtree(work, ignore_errors=True)


def assemblies_digest(directory: pathlib.Path) -> str:
    """The sha256 of a compiler build's own assemblies (cobol.dll and every WiseOwl.Cobol.*.dll in its directory), each by
    name and content: the bits that produce every verdict. Refused when the directory holds no cobol.dll."""
    if not (directory / "cobol.dll").is_file():
        raise SystemExit(f"{TOOL}: {directory} holds no cobol.dll, so the compiler's build cannot be identified")
    digest = hashlib.sha256()
    for assembly in sorted({directory / "cobol.dll", *directory.glob("WiseOwl.Cobol.*.dll")}):
        digest.update(assembly.name.encode("utf-8") + b"\0" + hashlib.sha256(assembly.read_bytes()).digest())
    return digest.hexdigest()


def compiler_identity(cobol: pathlib.Path) -> dict[str, Any]:
    """What the binary WAS BUILT FROM, read from the binary, never from the checkout that holds it: its informational
    version (`cobol --version`; a Release build's carries `+<commit>`, a Debug build's deliberately none, Directory.Build.props
    kb/Work PB985, so `commit` is then None) and the digest of its assemblies (`assemblies_digest`). Refused loudly when
    either cannot be read."""
    try:
        done = subprocess.run([str(cobol), "--version"], capture_output=True, text=True, timeout=120)
    except (OSError, subprocess.TimeoutExpired) as e:
        raise SystemExit(f"{TOOL}: {cobol} --version could not run ({e}), so the compiler's build cannot be identified")
    version = done.stdout.strip()
    if done.returncode != 0 or not version:
        raise SystemExit(f"{TOOL}: {cobol} --version exited {done.returncode} with no version, so the compiler's build "
                         f"cannot be identified")
    return {"version": version, "commit": version.split("+", 1)[1] if "+" in version else None,
            "assemblies": assemblies_digest(cobol.parent)}


def harness_run(root: pathlib.Path, adapter_path: pathlib.Path, variables: dict[str, str], edition: str,
                tests: dict[str, list[dict[str, Any]]]) -> harness.Run:
    """The conformance harness's run of drafts' tests (`tests_of`, by rule) staged under `root`, through the adapter at
    `adapter_path` and its profile: the ONE construction every reader of a draft's outcome judges through (this runner,
    and the placement of the checked drafts, place_witnesses.py)."""
    adapter, problems = config.load_adapter(adapter_path)
    if adapter is None:
        raise SystemExit(f"{TOOL}: {'; '.join(problems)}")
    profile, problems = config.load_profile(adapter_path.parents[1] / "profiles" / f"{adapter.profile}.json")
    if profile is None:
        raise SystemExit(f"{TOOL}: {'; '.join(problems)}")
    controls = {t["id"]: t for own in tests.values() for t in own[1:]}
    return harness.Run(root=root, adapter=adapter, profile=profile, variables={**adapter.variables, **variables},
                       tests={edition: controls}, work=root / "work", timeout=harness.HANG_GUARD, keep=False)


def run_drafts(state: pathlib.Path, adapter_path: pathlib.Path, variables: dict[str, str], edition: str, jobs: int,
               compiler: dict[str, Any]) -> dict[str, Any]:
    drafts = answers_by_id(state, "drafts")
    if not drafts:
        raise SystemExit(f"{TOOL}: {state} has no drafts answers (audit_batch.py plan, submit, collect --pass drafts)")
    with tempfile.TemporaryDirectory(prefix="drafts-") as scratch:
        root = pathlib.Path(scratch)
        tests = {rid: tests_of(root, rid, drafts[rid]) for rid in sorted(drafts)}
        run = harness_run(root, adapter_path, variables, edition, tests)
        with ThreadPoolExecutor(max_workers=max(1, jobs)) as pool:
            got = dict(zip(tests, pool.map(lambda rid: judge(run, edition, tests[rid][0]), tests)))
    return {"compiler": compiler, "edition": edition, "runs": got}


def write_record(state: pathlib.Path, record: dict[str, Any]) -> pathlib.Path:
    out = Pass(state, "drafts").dir / "runs.json"
    write_atomic(out, json.dumps(record, indent=1, ensure_ascii=False))
    tally = collections.Counter(r["verdict"] for r in record["runs"].values())
    built = record["compiler"]
    print(f"{TOOL}: {len(record['runs'])} draft(s) at {record['edition']} on cobol {built['version']} (assemblies "
          f"{built['assemblies'][:12]}): {dict(sorted(tally.items()))} -> {out}", flush=True)
    return out


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    """Planted drafts through the conformance harness's own stub compiler (run.py `_STUB`, whose `diag N` reports a
    diagnostic at line N): a matched output, a mismatched output, a refusal at its violating line, a refusal at another
    line, a refused control, a refusal the compiler translates, and a crash; then the
    compiler's identity."""
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
        two = "\n".join(["*> STUB diag 2", "*> the violating line"])
        clean = "*> STUB run out ok"
        drafts = {"1985-GR-A.1-1": ("*> STUB run out HELLO", {"outcome": "output", "output": ["HELLO"]}),
                  "1985-GR-A.1-2": ("*> STUB run out BYE", {"outcome": "output", "output": ["HELLO"]}),
                  "1985-SR-A.1-3": (two, {"outcome": "refused", "line": 2, "control": clean}),
                  "1985-SR-A.1-4": (two, {"outcome": "refused", "line": 1, "control": clean}),
                  "1985-SR-A.1-5": (two, {"outcome": "refused", "line": 2, "control": "*> STUB refuse"}),
                  "1985-SR-A.1-7": ("*> STUB run out X", {"outcome": "refused", "line": 1, "control": clean}),
                  "1985-SR-A.1-8": ("*> STUB crash", {"outcome": "output", "output": ["X"]}),
                  # drafts that COPY a library text (the stub's `need`: the compile fails without the file), carried as
                  # the draft's `files` (kb/Work PB5067): an output draft, and a refusal whose program and control both
                  # need a text in a library folder
                  "1985-GR-A.1-9": ("*> STUB need LIB1.cpy\n*> STUB run out COPIED",
                                    {"outcome": "output", "output": ["COPIED"]}),
                  "1985-SR-A.1-10": ("*> STUB need LIBA/TEXT1.cpy\n*> STUB diag 2",
                                     {"outcome": "refused", "line": 2,
                                      "control": "*> STUB need LIBA/TEXT1.cpy\n*> STUB run out ok"})}
        files = {"1985-GR-A.1-9": {"LIB1.cpy": "       01  W PIC X.\n"},
                 "1985-SR-A.1-10": {"LIBA/TEXT1.cpy": "       01  W PIC X.\n"}}
        p = Pass(state, "drafts")
        write_plan(p, [{"id": rid} for rid in drafts])
        p.answers_dir.mkdir(parents=True)
        for rid, (program, expected) in drafts.items():
            p.answer_file(custom_id("drafts", rid)).write_text(json.dumps(
                {"id": rid, "answer": {"program": program, "expected": expected, "basis": "planted",
                                       **({"files": files[rid]} if rid in files else {})}}),
                encoding="utf-8", newline="\n")
        planted = {"version": "planted", "commit": None, "assemblies": "0" * 64}
        record = run_drafts(state, root / "adapters" / "stub.json", {}, "85", 2, planted)
        got = {rid: r["verdict"] for rid, r in record["runs"].items()}
        ok(got == {"1985-GR-A.1-1": MATCH, "1985-GR-A.1-2": MISMATCH, "1985-SR-A.1-3": MATCH, "1985-SR-A.1-4": MISMATCH,
                   "1985-SR-A.1-5": MISMATCH, "1985-SR-A.1-7": MISMATCH,
                   "1985-SR-A.1-8": NO_VERDICT, "1985-GR-A.1-9": MATCH, "1985-SR-A.1-10": MATCH},
           "the harness judges each draft: output matched and mismatched; a refusal a MATCH only with a diagnostic at its "
           "violating line and a control that compiles; a crash no verdict; a draft that COPYs a library text runs with "
           f"its files, the program and its control, a library folder kept (kb/Work PB5067) ({got})")
        for bad in ({"../LIB1.cpy": "x"}, {"/abs.cpy": "x"}, {"A/B/C.cpy": "x"}, ["LIB1.cpy"], {"LIB1.cpy": 1}):
            try:
                tests_of(root / "bad", "1985-GR-A.1-11", {"program": "x", "expected": {"outcome": "output",
                                                                                         "output": []}, "files": bad})
                ok(False, f"a draft whose files are {bad!r} is refused, naming its rule")
            except SystemExit as e:
                ok("1985-GR-A.1-11" in str(e) and "files" in str(e), f"a draft whose files are {bad!r} is refused, "
                                                                     f"naming its rule")
        runs = record["runs"]
        ok("HELLO" in runs["1985-GR-A.1-2"]["reason"] and "marked line 1" in runs["1985-SR-A.1-4"]["reason"]
           and "control" in runs["1985-SR-A.1-5"]["reason"] and runs["1985-SR-A.1-7"]["reason"],
           "each reason says why: the differing output, the diagnostic elsewhere, the refused control, the translated one")
        ok(record["compiler"] == planted and record["edition"] == "85", "the record names the compiler and the edition")
        out = write_record(state, record)
        ok(json.loads(out.read_text(encoding="utf-8")) == record and out.parent == p.dir,
           "the record is the run's drafts/runs.json")
        try:
            run_drafts(root / "empty", root / "adapters" / "stub.json", {}, "85", 1, planted)
            ok(False, "a run with no drafts is refused")
        except SystemExit as e:
            ok("no drafts answers" in str(e), "a run with no drafts is refused")
        build = root / "build"
        build.mkdir()
        (build / "cobol.dll").write_bytes(b"cli")
        (build / "WiseOwl.Cobol.Compiler.dll").write_bytes(b"compiler")
        (build / "Other.dll").write_bytes(b"not ours")
        first = assemblies_digest(build)
        (build / "Other.dll").write_bytes(b"changed")
        unmoved = assemblies_digest(build) == first
        (build / "WiseOwl.Cobol.Compiler.dll").write_bytes(b"compiler2")
        ok(unmoved and assemblies_digest(build) != first,
           "the build's identity is its own assemblies' bits (a changed compiler assembly changes it, another does not)")
        for what, check in (("a build with no cobol.dll", lambda: assemblies_digest(root / "empty")),
                            ("a compiler that cannot report its version", lambda: compiler_identity(root / "no-cobol"))):
            try:
                check()
                ok(False, f"{what} is refused, never recorded as unknown")
            except SystemExit as e:
                ok("cannot be identified" in str(e), f"{what} is refused, never recorded as unknown")
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
    write_record(a.state, run_drafts(a.state, ADAPTER, {"cobol": str(a.cobol)}, a.edition, a.jobs,
                                     compiler_identity(a.cobol)))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
