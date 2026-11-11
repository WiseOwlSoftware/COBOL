#!/usr/bin/env python3
"""ci_plan.py — THE MERGE GROUP'S PLAN: each build target's key at the merged tree on each leg of the test matrix, and the
passes the store ref holds for them (kb/Work PB5235, stage 6 of PB5207; DESIGN-test-build-ci.md §3.14.14).

    python scripts/ci_plan.py --remote origin [--tree T] [--github-output F] [--summary F]   # a merge group
    python scripts/ci_plan.py --no-records [...]                                             # any other event
    python scripts/ci_plan.py --self-test

A LEG (`CI_LEGS`) is one platform and configuration the merge group tests on. For each leg and each target of
scripts/build_targets.json, the plan computes the target's key at the merged tree with `buildgraph.keys` (the function the
local gate uses), under the leg's facets: its platform, its configuration, and the toolchain toolchain.env names for its OS,
exactly as the gate on that OS names it. A leg that ACCEPTS RECORDS takes a pass the store ref holds at that key whose
facets are the leg's (`buildgraph.Published.lookup`, the store's one rule) and does not run that target; every other target
runs. The `changes` job of .github/workflows/build-and-test.yml runs this, and its outputs (per leg, the configuration and
the JSON list of target assemblies to run) are the ONLY thing the shard jobs decide on: `ci_wiring` holds the workflow to
that, for every target the targets file declares and every leg here.

The records travel on `buildgraph.STORE_REF` (buildgraph.py's transport: `publish`, `fetch_published`).
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import buildgraph  # noqa: E402  — the targets, their keys and the store's transport
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]


# ── the legs ─────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class Leg:
    """One platform and configuration the merge group tests on. `accepts_records`: whether a pass recorded elsewhere at
    this leg's key stands for the leg's run; `why` says what the leg proves that no record can."""
    name: str
    platform: str
    configuration: str
    system: str          # toolchain.py's name for the OS (its `gate` scope's tools differ by OS: git-linux, git-windows)
    accepts_records: bool
    why: str


#: ⛔ THE ONE STATEMENT OF WHAT EACH MERGE-GROUP LEG RUNS. The workflow's shard jobs read the configuration and the
#: target set from `plan`'s outputs (`ci_wiring` holds them to it). Windows runs Debug, the local gate's configuration,
#: so a pass the owner's machine recorded at the merged tree stands for it (owner 2026-11-10); Linux runs Release and
#: accepts no record: it is the merge group's own proof of Linux, of Release, of the inert watch and of INV-1-strong.
CI_LEGS = {
    "linux": Leg("linux", "linux", "Release", "Linux", False,
                 "Linux and Release are proven only here; the inert watch and INV-1-strong run only in this leg"),
    "windows": Leg("windows", "windows", "Debug", "Windows", True,
                   "the local gate's own facets (windows, Debug, toolchain.env's tools): a pass recorded at the merged "
                   "tree's key stands for this leg; with none, the leg runs"),
}


def leg_facets(leg: Leg, manifest: dict[str, str]) -> dict[str, str]:
    """The facets of `leg`'s keys: its platform and configuration and the toolchain toolchain.env names for its OS,
    exactly as the gate on that OS names it (`toolchain.summary(…, "gate", …)`, the gate's `Host.toolchain`)."""
    import toolchain  # noqa: PLC0415  — THE reader of toolchain.env
    return {"platform": leg.platform, "configuration": leg.configuration,
            "toolchain": toolchain.summary(manifest, "gate", leg.system)}


@dataclass(frozen=True)
class Planned:
    leg: str
    target: str
    assembly: str
    key: str
    record: dict | None   # the accepted pass; None: the leg runs this target

    def line(self) -> str:
        where = f"{self.leg}/{self.target} ({self.assembly}) key {self.key}"
        if self.record is None:
            return f"plan: {where}: RUN (no pass at this key)"
        r = self.record
        return (f"plan: {where}: PASSED, not run: the record of run {r.get('run')} at {str(r.get('head'))[:12]} "
                f"(tree {str(r.get('tree'))[:12]}, {r.get('when')}, counts {json.dumps(r.get('counts'), sort_keys=True)})")


def plan(repo: Path, tree: str, records: buildgraph.Published, targets: dict[str, buildgraph.Target] | None = None,
         manifest: dict[str, str] | None = None) -> list[Planned]:
    """THE MERGE GROUP'S PLAN: each target's key at `tree` on each leg; a leg that accepts records takes a pass of that
    target at that key whose facets are the leg's (toolchain.env's tools at `tree`), and runs every other target."""
    import toolchain  # noqa: PLC0415
    targets = targets if targets is not None else buildgraph.load_targets(repo / "scripts" / "build_targets.json")
    manifest = manifest if manifest is not None else toolchain.load(repo / "toolchain.env")
    out: list[Planned] = []
    for leg in CI_LEGS.values():
        facets = leg_facets(leg, manifest)
        for name, k in buildgraph.keys(repo, tree, facets, targets).items():
            rec = records.lookup(name, k) if leg.accepts_records else None
            if rec is not None and rec.get("facets") != facets:
                rec = None
            out.append(Planned(leg.name, name, targets[name].assembly, k, rec))
    return out


def plan_outputs(rows: list[Planned]) -> dict[str, str]:
    """The `changes` job's outputs: per leg, its configuration and the JSON list of the assemblies it runs."""
    out: dict[str, str] = {}
    for leg in CI_LEGS.values():
        out[f"{leg.name}_configuration"] = leg.configuration
        out[f"{leg.name}_targets"] = json.dumps(sorted(r.assembly for r in rows if r.leg == leg.name and r.record is None))
    return out


def ci_wiring(repo: Path) -> list[str]:
    """⛔ THE WORKFLOW RUNS WHAT THE PLAN SAYS, AND NOTHING DECIDES IT BESIDE THE PLAN (kb/Work PB5235): the drift test
    that makes a new target or leg automatic. For each leg of CI_LEGS, its `<leg>-shard` job: runs only when the plan
    gives it a target; builds and tests in the plan's configuration; runs a `dotnet test` step for every target of the
    targets file, each gated on that target's assembly being in the plan's set; lists its populations from that set;
    and `ci-gate` checks the population of exactly that set. Returns the findings."""
    import ci_workflow  # noqa: PLC0415
    text = (repo / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8").replace("\r\n", "\n")
    jobs = {j.id: j for j in ci_workflow.jobs(text)}
    targets = buildgraph.load_targets(repo / "scripts" / "build_targets.json")
    bad: list[str] = []
    changes = jobs.get("changes")
    if changes is None or not all(w in changes.code for w in (
            'if [ "$EVENT" = "merge_group" ]; then records=(--remote origin); else records=(--no-records); fi',
            'python3 scripts/ci_plan.py "${records[@]}"')):
        bad.append("the `changes` job no longer runs `ci_plan.py`, reading the store ref in a merge group only")
    gate = jobs.get("ci-gate")
    for leg in CI_LEGS.values():
        out = f"needs.changes.outputs.{leg.name}_targets"
        conf = f"--configuration ${{{{ needs.changes.outputs.{leg.name}_configuration }}}}"
        for o in ("targets", "configuration"):
            line = f"{leg.name}_{o}: ${{{{ steps.plan.outputs.{leg.name}_{o} }}}}"
            if changes is not None and line not in changes.code:
                bad.append(f"the `changes` job does not output `{line}`")
        job = jobs.get(f"{leg.name}-shard")
        if job is None:
            bad.append(f"no `{leg.name}-shard` job for the {leg.name} leg of CI_LEGS")
            continue
        if f"{out} != '[]'" not in (job.value("if") or ""):
            bad.append(f"{job.id}: its `if` does not skip the job when the plan gives it no target ({out} != '[]')")
        steps = job.steps
        builds = [s for s in steps if "dotnet build WiseOwl.Cobol.sln" in s.code]
        if not builds or any(conf not in s.code for s in builds):
            bad.append(f"{job.id}: the solution is not built in the plan's configuration ({conf})")
        lists = [s for s in steps if "scripts/test_population.py list" in s.code]
        if not lists or any(out not in s.code or conf not in s.code for s in lists):
            bad.append(f"{job.id}: the populations are not listed from the plan's set in its configuration")
        tests = [s for s in steps if re.search(r"\bdotnet test tests/", s.code)]
        for s in tests:
            m = re.search(r"\bdotnet test (tests/[^/\s]+/)", s.code)
            gated = re.search(rf"contains\(fromJSON\({re.escape(out)}\), '(\w+)'\)", s.value("if") or "")
            if conf not in s.code:
                bad.append(f"{job.id}: `{s.label}` does not test in the plan's configuration")
            if gated is None:
                bad.append(f"{job.id}: `{s.label}` runs whatever the plan says (no contains(fromJSON({out}), …))")
            elif m and not any(t.project == m.group(1) and t.assembly == gated.group(1) for t in targets.values()):
                bad.append(f"{job.id}: `{s.label}` is gated on {gated.group(1)!r}, not on its own project's target")
        tested = {m.group(1) for s in tests if (m := re.search(r"\bdotnet test (tests/[^/\s]+/)", s.code))}
        for t in targets.values():
            if t.project not in tested:
                bad.append(f"{job.id}: no test step of {t.project} for target {t.name}: the leg would never run it")
        if gate is None or out not in gate.code:
            bad.append(f"ci-gate's population check does not read the {leg.name} leg's plan ({out})")
    return bad


def plan_main(repo: Path, tree: str, remote: str | None, github_output: Path | None, summary: Path | None,
              say=print) -> int:
    """`plan`: read the remote's store ref (a ref that cannot be read is said, and every target runs: a missing record
    costs minutes, never a verdict), print one line per leg and target, and write the outputs. `remote` None
    (`--no-records`: a dispatch run or a push to main, which run the whole matrix) reads no record."""
    try:
        if remote is None:
            raise LookupError
        records = buildgraph.fetch_published(repo, remote)
        source = (f"{remote} {buildgraph.STORE_REF} at {records.commit[:12]}, {len(records.records)} record(s)" if records.commit
                  else f"{remote} has no {buildgraph.STORE_REF}")
    except subprocess.CalledProcessError as e:
        records = buildgraph.Published({}, None)
        source = f"{remote} {buildgraph.STORE_REF} could not be read ({(e.stderr or b'').decode(errors='replace').strip()})"
        say(f"::warning::build store: {source}; every target runs")
    except LookupError:
        records, source = buildgraph.Published({}, None), "not read (--no-records: only a merge group accepts a recorded pass)"
    rows = plan(repo, tree, records)
    say(f"plan: the store: {source}")
    for r in rows:
        say(r.line())
    outputs = plan_outputs(rows)
    for k, v in outputs.items():
        say(f"plan: {k}={v}")
    nl = chr(10)
    if github_output is not None:
        with github_output.open("a", encoding="utf-8", newline=nl) as fh:
            fh.write("".join(f"{k}={v}{nl}" for k, v in outputs.items()))
    if summary is not None:
        with summary.open("a", encoding="utf-8", newline=nl) as fh:
            fh.write(f"### plan (scripts/ci_plan.py; kb/Work PB5235){nl}{nl}The store: {source}{nl}{nl}"
                     f"| leg | target | key | |{nl}|---|---|---|---|{nl}"
                     + "".join(f"| {r.leg} | {r.target} | `{r.key[:16]}` | "
                               + ("run" if r.record is None else f"passed: run {r.record.get('run')} at "
                                  f"`{str(r.record.get('head'))[:12]}`") + f" |{nl}" for r in rows))
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    root = scratch_dir("ci-plan-selftest-")
    try:
        repo = ScratchRepo.init(root / "repo")
        repo.commit("base", {"src/A.cs": "class A {}\n", "conformance-suite/c.cob": "c\n", "docs/n.md": "n\n"})
        targets = {"unit": buildgraph.Target("unit", "Unit", "tests/U/", ("src/",), ()),
                   "conformance": buildgraph.Target("conformance", "Conformance", "tests/C/",
                                                    ("src/", "conformance-suite/"), ())}
        manifest = {"DOTNET_SDK_VERSION": "10.0.404", "PYTHON_VERSION": "3.15.2", "JDK_VERSION": "27.0.1",
                    "PWSH_VERSION": "7.7.0", "GIT_LINUX_VERSION": "2.56.0", "GIT_WINDOWS_VERSION": "2.56.0",
                    "GH_VERSION": "2.103.3", "JQ_VERSION": "1.8.2"}
        facets = leg_facets(CI_LEGS["windows"], manifest)
        tree = repo.git("rev-parse", "HEAD^{tree}")
        k = buildgraph.keys(repo.path, tree, facets, targets)
        rec = {"target": "conformance", "key": k["conformance"], "result": "pass", "assembly": "Conformance",
               "facets": facets, "run": "000007", "head": "f" * 40, "tree": tree, "when": "2026-11-10T12:00:00Z",
               "counts": {"discovered": 9, "ran": 9, "skipped": 0, "carried": 0}}
        held = buildgraph.Published({("conformance", k["conformance"]): rec}, "c" * 40)

        def rows(records: buildgraph.Published, at: str = tree, m: dict[str, str] = manifest) -> dict:
            return {(r.leg, r.target): r for r in plan(repo.path, at, records, targets, m)}

        same = rows(held)
        arm("the Windows leg accepts a pass at the key it computes itself and names the record; it runs the target "
            "with no pass; the Linux leg accepts no record; the outputs list what each leg runs",
            same[("windows", "conformance")].record is not None and same[("windows", "unit")].record is None
            and same[("linux", "conformance")].record is None
            and "PASSED, not run: the record of run 000007" in same[("windows", "conformance")].line()
            and plan_outputs(list(same.values())) == {
                "linux_configuration": "Release", "linux_targets": '["Conformance", "Unit"]',
                "windows_configuration": "Debug", "windows_targets": '["Unit"]'},
            str([r.line() for r in same.values()]))
        repo.commit("edit", {"conformance-suite/c.cob": "c2\n"})
        moved = rows(held, repo.git("rev-parse", "HEAD^{tree}"))
        tools = rows(held, tree, {**manifest, "DOTNET_SDK_VERSION": "10.0.999"})
        repo.commit("docs only, against the recorded tree", {"conformance-suite/c.cob": "c\n", "docs/n.md": "n2\n"})
        docs_tree = repo.git("rev-parse", "HEAD^{tree}")
        docs = rows(held, docs_tree)
        arm("a record is accepted only at the key CI computes: a change to one of the target's inputs (the merged tree "
            "moved), or another toolchain named by toolchain.env, runs the target; a tree that differs from the "
            "recorded one outside the target's inputs (docs/) still takes the pass",
            moved[("windows", "conformance")].record is None and tools[("windows", "conformance")].record is None
            and docs_tree != tree and docs[("windows", "conformance")].record is not None,
            f"{moved[('windows', 'conformance')].line()} {tools[('windows', 'conformance')].line()} "
            f"{docs[('windows', 'conformance')].line()}")
        wrong = rows(buildgraph.Published({("conformance", k["conformance"]):
                                           {**rec, "facets": {**facets, "configuration": "Release"}}}, "x"))
        forged = rows(buildgraph.Published({("conformance", k["conformance"]): {**rec, "key": k["unit"]}}, "x"))
        arm("a pass whose facets are not the leg's, and a record filed under a key it does not name, are refused",
            wrong[("windows", "conformance")].record is None and forged[("windows", "conformance")].record is None)
        said: list[str] = []
        out = root / "out.txt"
        plan_main(REPO, "HEAD^{tree}", None, out, None, said.append)
        written = dict(line.split("=", 1) for line in out.read_text(encoding="utf-8").splitlines())
        arm("--no-records reads no ref and runs every target of the real targets file on every leg, writing each "
            "leg's configuration and target list as GitHub outputs",
            any("not read (--no-records" in s for s in said)
            and set(written) == {f"{leg}_{o}" for leg in CI_LEGS for o in ("configuration", "targets")}
            and all(json.loads(written[f"{leg}_targets"]) == sorted(t.assembly for t in
                                                                    buildgraph.load_targets().values())
                    for leg in CI_LEGS), f"{said} {written}")
        wiring = ci_wiring(REPO)
        arm("the real workflow runs each leg as CI_LEGS states it: its configuration and target set from the plan's "
            "outputs, a step per target assembly gated on that set, and ci-gate's population check over that set",
            wiring == [], "; ".join(wiring))
        planted = root / "planted"
        (planted / ".github" / "workflows").mkdir(parents=True)
        (planted / "scripts").mkdir()
        wf = (REPO / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8")
        ungated = wf.replace("        if: contains(fromJSON(needs.changes.outputs.windows_targets), 'Unit')\n", "", 1)
        (planted / ".github" / "workflows" / "build-and-test.yml").write_text(ungated, encoding="utf-8", newline="\n")
        (planted / "scripts" / "build_targets.json").write_text(
            (REPO / "scripts" / "build_targets.json").read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
        found = ci_wiring(planted)
        arm("the wiring check names a Windows Unit step that runs whatever the plan says",
            ungated != wf and any("windows-shard: `Unit, this shard` runs whatever the plan says" in f for f in found),
            str(found))
    finally:
        remove_tree(root)
    bad = [n for n, ok, _ in results if not ok]
    print(f"=== ci_plan SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(bad)}" if bad else "") + ") ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--tree", default="HEAD^{tree}")
    src = ap.add_mutually_exclusive_group()
    src.add_argument("--remote", help="read the passes this remote's store ref holds (a merge group)")
    src.add_argument("--no-records", action="store_true", help="read none: every target runs (any other event)")
    ap.add_argument("--github-output", type=Path, help="append the legs' outputs here ($GITHUB_OUTPUT)")
    ap.add_argument("--summary", type=Path, help="append the plan as Markdown here ($GITHUB_STEP_SUMMARY)")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.remote and not args.no_records:
        ap.error("name the records' source: --remote <name> (a merge group) or --no-records")
    return plan_main(REPO, args.tree, None if args.no_records else args.remote, args.github_output, args.summary)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
