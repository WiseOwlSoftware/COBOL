#!/usr/bin/env python3
"""The wiring of the real tree (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5397; kb/Work PB2878, PB4326, PB4395, PB4409): CI's workflow, the gate and the self-test runner decide the lanes through the one classification and carry no path list of their own, read from the real tree's scripts and workflow."""
import re
import sys
from pathlib import Path

import fixtures
import selftest_cases
from standard_streams import ensure_utf8

il = selftest_cases.subject(__file__)   # scripts/inert_lane.py
REPO = il.REPO


def _wiring(root: Path) -> list[str]:
    """⛔ ONE CLASSIFICATION DECIDES THE LANES, AND EVERY LINUX TEST RUN PROVES IT (kb/Work PB2878, PB4326, PB4395): the
    WIRING a refactor of a workflow step or a gate script would silently drop, read from the real tree's scripts and
    workflow (all tooling). A `dotnet test` step that loses its watch stops proving the classification, and a runner that
    grows its own path list stops agreeing with the others (the CI invariant, kb/Work PB1957). Returns the findings."""
    import ci_workflow  # noqa: PLC0415

    def text(rel: str) -> str:
        return (root / rel).read_text(encoding="utf-8").replace("\r\n", "\n")

    bad: list[str] = []

    def need(ok: bool, finding: str) -> None:
        if not ok:
            bad.append(finding)

    jobs = {j.id: j for j in ci_workflow.jobs(text(".github/workflows/build-and-test.yml"))}
    cls = "scripts/inert_lane.py"
    changes = jobs["changes"].code
    need(f"python3 {cls} classify --base" in changes, f"CI's `changes` job no longer decides the lane through {cls}")
    need(not re.search(r"""grep -[A-Za-z]*E[A-Za-z]* "\$IGNORE"|IGNORE='|\^\((DEVLOG|docs|kb)/""", changes),
         f"the `changes` job carries a path list of its own again; the lanes have ONE classification, {cls} (PB2878)")
    steps = [(j.id, m.group(0).strip()) for j in jobs.values() if (j.value("runs-on") or "").startswith("ubuntu")
             for m in re.finditer(r"^.*\bdotnet test tests/.*$", j.code, re.MULTILINE)]
    need(len(steps) >= 4, f"found only {len(steps)} `dotnet test` step(s) in the Linux jobs: the scan is broken")
    bad += [f"a Linux `dotnet test` step runs outside the watch, so a test that starts reading an inert or tooling-lane "
            f"path is seen by no one (PB2878): {jid}: {line}" for jid, line in steps
            if f"{cls} watch -- dotnet test" not in line]
    linux_gate, driver, runner = text("scripts/linux-gate.sh"), text("scripts/gate.py"), text("scripts/self_tests.py")
    need('inert_lane.py" classify' not in linux_gate, "the Linux gate classifies again (it classifies nothing, PB4327)")
    need("scripts/inert_lane.py watch" in linux_gate, "the Linux gate's test legs no longer run under the watch")
    need('export PYTHONPYCACHEPREFIX="$gate/$leg.pycache"' in linux_gate,
         "the Linux gate's test legs no longer run with a fresh bytecode cache, so its watch misses a script served from "
         "an earlier __pycache__ that CI's watch sees (PB4395 review R2-1)")
    for want in ("import inert_lane", "inert_lane.CLASSIFICATION.classify(", "return self._inert(out, run, lane)",
                 "return self._tooling(out, run, lane)", 'TOOLING_ONLY = ("SELF-TESTS",)',
                 "if tooling or name not in TOOLING_ONLY", "self.host.audits(_no_slot, lines.append, tooling, change)",
                 'self._beside(out, "audits", lambda: self._audits(out, lane.tooling))',
                 'if out.tooling:\n            self._beside(out, "post_build_self_tests"'):
        need(want in driver, f"scripts/gate.py no longer says `{want}`")
    for lane in ("_inert", "_tooling"):
        at = driver.find(f"    def {lane}(")
        body = driver[at:driver.find("\n    def ", at + 1)] if at >= 0 else ""
        need(bool(body) and "take_slot" not in body and "self.host.build(" not in body and "linux" not in body.lower(),
             f"gate.py's {lane} lane takes a slot, builds or reaches Linux: it runs the audits and nothing else")
    audits = jobs["audits"]
    need("python3 scripts/self_tests.py" in audits.code, "CI's `audits` job no longer runs the Linux self-tests")
    need(audits.value("if") == "github.event_name == 'merge_group' || needs.changes.outputs.run_matrix == 'true'",
         f"CI's `audits` job's `if` changed: {audits.value('if')!r} (PB4330: every merge group, no pull request)")
    need("pull_request" not in audits.code, "CI's `audits` job runs on a pull request again (PB4330)")
    need(f"python3 {cls} check" in audits.code, "CI's `audits` job no longer runs THE INERT LANE audit (check)")
    for want in ("reads_tooling: ${{ steps.decide.outputs.reads_tooling }}", "grep -qx 'reads_tooling=false'",
                 "reads_tooling=true", 'echo "reads_tooling=$reads_tooling"'):
        need(want in changes, f"CI's `changes` job no longer says `{want}` (the reads_tooling flag, PB4326, PB4409)")
    need("outputs.tooling" not in changes and "grep -qx 'tooling=" not in changes,
         "CI's `changes` job decides on the local gate's narrow `tooling=` flag: CI's self-tests run on `reads_tooling=` "
         "(PB4409 review R1-1)")
    need(jobs["windows-self-tests"].value("if")
         == "needs.changes.outputs.run_matrix == 'true' && needs.changes.outputs.reads_tooling == 'true'",
         "CI's `windows-self-tests` job no longer runs exactly when the change touches what a self-test reads "
         "(`reads_tooling`, PB4409)")
    need('print(f"reads_tooling={' in text("scripts/inert_lane.py"),
         "inert_lane.py classify no longer prints the `reads_tooling=` line CI's `changes` job reads")
    # The tooling flag is PROVED against what every self-test read (PB4326 R1-1), before its PASS is recorded; the one
    # exemption is a DECLARED population, held to the trace.
    for want in ("import inert_lane", "classification: inert_lane.Classification = inert_lane.CLASSIFICATION",
                 "self.classification.unclassified_reads(", "if not test.population:",
                 "if test.population and not whole:"):
        need(want in runner, f"scripts/self_tests.py no longer says `{want}`")
    need("if not whole:" not in runner, "scripts/self_tests.py exempts a whole-tree reader without its declaration")
    need(runner.find("self.classification.unclassified_reads(") < runner.find("self.store.record(test.path"),
         "scripts/self_tests.py records a PASS before the tooling flag's proof")
    for rel in ("scripts/selftests/orchestrator/plan_wave/test_planner.py",):
        need("# SELF-TEST-POPULATION: tree" in text(rel), f"{rel} no longer declares its population")
    return bad


def case_00_the_wiring_of_the_real_tree(expect):
    expect.equal("THE WIRING (the real tree): CI's changes job and the gate decide through this classification and carry no "
                 "path list; every Linux dotnet test step and the Linux gate's legs run under the watch; the gate's inert and "
                 "tooling lanes take no slot and build nothing; CI's audits job runs the self-tests and the check; the tooling "
                 "flag reaches windows-self-tests and the gate; self_tests.py proves the flag before it records a PASS",
                 _wiring(REPO), [])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
