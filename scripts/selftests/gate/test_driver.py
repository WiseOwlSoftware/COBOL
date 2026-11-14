#!/usr/bin/env python3
"""The driver's own defects: shared settings, work beside the critical path, one shape, a crash, a failed build, the run history (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import json
import sys
import time
from detached import kill_tree, pid_alive, spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536), THE one tree kill (PB4072), THE one liveness probe (PB4008)
from pathlib import Path
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT, this_platform  # noqa: E402
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)

import fixtures
import selftest_cases
from gate_fake_host import FakeHost
from standard_streams import ensure_utf8
from scratch_repo import scratch_dir

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
CHANGE_SCOPED = gate_mod.CHANGE_SCOPED
Gate = gate_mod.Gate
Host = gate_mod.Host
KEEP_RUNS = gate_mod.KEEP_RUNS
SELF_TESTS_REUSE = gate_mod.SELF_TESTS_REUSE
VERDICT = gate_mod.VERDICT
VERDICT_FILE = gate_mod.VERDICT_FILE
_no_slot = gate_mod._no_slot
not_run = gate_mod.not_run
read_verdict = gate_mod.read_verdict
run_dirs = gate_mod.run_dirs


def case_00_shared_gate_settings_turn_malformed_while_gate(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("slot-settings", plant={"slot-settings"})
    expect("shared gate settings that turn malformed while the gate queues: NOT RUN [settings] (ENVIRONMENT), exit 2, "
        "with the reason, the audits already run",
        o.verdict == "NOT RUN [settings]" and o.exit_code == 2 and "while the gate queued" in o.line
        and not h.built and not read_verdict(o.line[len(VERDICT):]).branch_carried
        and h.events == ["fetch", "slot"], o.line)
    class HungSelfTests(Host):  # the real Host.audits over planted exit codes: no audit runs
        def __init__(self):
            super().__init__(root)
            self.commands: list[list[str]] = []

        def audit_prerequisites(self):
            return ()

        def _captured(self, cmd, spawn, live=None):
            self.commands.append(cmd)
            return (SELF_TESTS_HANG_EXIT if cmd[0] == "scripts/self_tests.py" else 0), "", 0.0
    class PrerequisiteFails(HungSelfTests):  # a real prerequisite process that fails: no audit may start
        def audit_prerequisites(self):
            return (("PLANTED PREREQUISITE", [sys.executable, "-c", "print('not ready'); raise SystemExit(3)"]),)
    unready, heard_unready = PrerequisiteFails(), []
    reasons = unready.audits(_no_slot, heard_unready.append, True, None)
    expect("a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its "
        "output, and no audit runs (kb/Work PB1957 row 44)",
        reasons == ["PLANTED PREREQUISITE RED"] and unready.commands == [] and "not ready" in heard_unready,
        f"{reasons} {unready.commands} {heard_unready}")
    hung = HungSelfTests()
    reasons = hung.audits(_no_slot, lambda _: None, True, ("base-1", "tree-1"))
    expect("a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run "
        "with --reuse (kb/Work PB2914, PB3022)",
        reasons == ["SELF-TESTS HANG"] and ["scripts/self_tests.py", SELF_TESTS_REUSE] in hung.commands,
        f"{reasons} {hung.commands}")
    whole = HungSelfTests()
    whole.audits(_no_slot, lambda _: None, False, None)
    expect("a CHANGE_SCOPED audit (LINE ENDINGS) reads only the change the gate hands it, `--base <base> --tree "
        "<measured tree>`, and the whole tree when the gate has no change; no other audit is told (kb/Work PB5225)",
        CHANGE_SCOPED == ("LINE ENDINGS",)
        and ["scripts/line_endings.py", "--check", "--base", "base-1", "--tree", "tree-1"] in hung.commands
        and ["scripts/line_endings.py", "--check"] in whole.commands
        and sum("--base" in c for c in hung.commands) == 1 and not any("--base" in c for c in whole.commands),
        f"{hung.commands} {whole.commands}")
    class FirstRedStops(Host):  # the real Host.audits over REAL processes: a slow audit with a child, and a red one
        def __init__(self, mark: Path):
            super().__init__(root)
            self.mark = mark

        def audit_prerequisites(self):
            return ()

        def audit_commands(self, tooling):
            slow = ("import pathlib, subprocess, sys; "
                    "p = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(3600)']); "
                    f"pathlib.Path({str(self.mark)!r}).write_text(str(p.pid), encoding='utf-8'); p.wait()")
            red = (f"import pathlib, time\nm = pathlib.Path({str(self.mark)!r})\n"
                   "while not m.exists():\n    time.sleep(0.05)\nraise SystemExit(1)")
            return (("SLOW", ["-c", slow]), ("QUICK-RED", ["-c", red]))
    mark = scratch_dir("first-red-") / "grandchild.pid"
    heard: list[str] = []
    reasons = FirstRedStops(mark).audits(_no_slot, heard.append, False, None)
    grandchild = int(mark.read_text(encoding="utf-8"))
    while pid_alive(grandchild):  # termination is asynchronous; a tree that never dies is the hang detector's
        time.sleep(0.05)
    expect("the first RED audit stops every audit still running, its whole process tree included, and the stopped one "
        "is reported STOPPED, never RED (kb/Work PB3061)",
        reasons == ["QUICK-RED RED"] and any(s.startswith("--- audit SLOW: STOPPED") for s in heard)
        and any("1 audit(s) still running stopped" in s for s in heard), f"{reasons} {heard}")


def case_01_defect_work_beside_critical_path_named_red(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("post-build-crash", plant={"post-build-crash", "tooling"})
    expect("a defect in work beside the critical path is a named RED with its traceback, and the hosts still ran",
        o.verdict == "RED" and o.exit_code == 1 and "POST BUILD SELF TESTS FAILED (RuntimeError" in o.line
        and len(o.runs) == len(GATED_ASSEMBLIES) and said("planted: a defect in the post-build self-tests' runner"), o.line)


def case_02_one_shape_kb_work_pb3022_every_gate(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("one-shape", plant={"red"})
    expect("ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits beside the build and the hosts and runs "
        "the whole population; there is no mode and no scope",
        h.slots_taken == 1 and h.events[:3] == ["fetch", "slot", "build"] and o.verdict == "RED"
        and not {"mode", "scope", "leg1_only", "plan"} & set(json.loads(
            run_dirs(root / "one-shape" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))), o.line)


def case_03_defect_driver_still_ends_one_verdict_line(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("crash", plant={"crash"})
    expect("a defect in the driver still ends in ONE verdict line (NOT RUN [driver], exit 2) — the callers block on "
        "it — which the one verdict reader classifies BRANCH: the driver is the tree's own code (review R2-1)",
        o.verdict == "NOT RUN [driver]" and o.exit_code == 2 and sink[-1] == o.line and "RuntimeError" in o.line
        and read_verdict(o.line[len(VERDICT):]) == not_run("driver")
        and said("Traceback"), o.line)


def case_04_failed_build_build_failed_exit_1_test(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("build", plant={"build"})
    expect("a failed build is BUILD FAILED, exit 1, and no test host runs",
        o.verdict == "BUILD FAILED" and o.exit_code == 1 and not o.runs, o.line)


def case_05_previous_run_newest_one_holding_verdict_only(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "history"
    (d / "runs").mkdir(parents=True)
    for i in range(7):
        p = d / "runs" / f"2026010{i}T000000Z-00000{i}"
        p.mkdir()
        (p / VERDICT_FILE).write_text("{}", encoding="utf-8", newline="\n")
        synthetic_trx(p / "Unit.trx", [("N.Unit.A", ["Passed"])])
    (d / "runs" / "20260109T000000Z-000009").mkdir()  # newest, but it never reached a verdict
    g = Gate(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append)
    prev = g.previous_run(d / "runs" / "20260110T000000Z-00000a")
    g._prune(d / "runs" / "20260109T000000Z-000009")
    kept = sorted(p.name for p in (d / "runs").iterdir())
    expect("the previous run is the newest one holding a verdict, and only the newest five runs are kept",
        prev is not None and prev.name == "20260106T000000Z-000006" and len(kept) == KEEP_RUNS
        and kept[-1] == "20260109T000000Z-000009", f"{prev} {kept}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
