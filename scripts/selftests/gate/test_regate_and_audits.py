#!/usr/bin/env python3
"""A re-gate, the population that cannot be listed, and the audits that stop the gate (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import os
import re
from gate_slot import (ExclusiveLock, GateSlots, Heartbeat, QueueAbandoned, Slot,  # noqa: E402
                       gate_class_from_env)
from pathlib import Path
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)

import sys

import fixtures
import selftest_cases
from gate_fake_host import FakeHost
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
from gate_host import CI_BOUNCE_RESULTS, FIRST_CLASSES, ONLY_CASES, host_env
Gate = gate_mod.Gate
from gate_verdict import VERDICT, read_verdict
from gate_runs import VERDICT_FILE, run_dirs


def case_00_re_gate_hands_every_host_classes_red(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "first"
    d.mkdir()
    first_host = FakeHost(d, d / "gate.lock", plant={"red"}, trees=["tree-red"])
    Gate(first_host, d / "runs", say=sink.append).run()
    bounce = d / CI_BOUNCE_RESULTS / "test-results-Linux-shard-2"
    bounce.mkdir(parents=True)
    synthetic_trx(bounce / "Unit.trx", [("N.Bounced.Case", ["Failed"]), ("N.Unit.A", ["Passed"])])
    again_host = FakeHost(d, d / "gate.lock", trees=["tree-fixed"])
    sink.clear()
    again = Gate(again_host, d / "runs", say=sink.append).run()
    named = Path(again_host.envs["Unit"].get(FIRST_CLASSES, ""))
    expect("a re-gate hands every host the classes red at the previous run and in the bounce (one file, FIRST_CLASSES), "
        "says so, and still runs and checks the whole population",
        again.verdict == "GREEN" and again.first == ["N.Bounced", "N.Conf"]
        and all(v.get(FIRST_CLASSES) == str(named) for v in again_host.envs.values())
        and named.read_text(encoding="utf-8") == "N.Bounced\nN.Conf\n"
        and said("2 class(es) red at the previous run or the merge queue run first")
        and "2 class(es) first" in again.line and all(p.exact for p in again.populations.values()),
        f"{again.line} {again.first}")
    (bounce / "Unit.trx").write_text("<TestRun><Results><UnitTestResult outcome=\"" + chr(1) + "\"/>",
                                     encoding="utf-8", newline="\n")
    torn_host = FakeHost(d, d / "gate.lock", trees=["tree-torn"])
    sink.clear()
    torn = Gate(torn_host, d / "runs", say=sink.append).run()
    kept = list(run_dirs(d / "runs")[-1].glob("unreadable-*"))
    expect("an unreadable trx among the first classes' sources is named and KEPT in the run directory (never as a .trx a "
        "later reader would glob), and the gate goes on GREEN without it (kb/Work PB3661)",
        torn.verdict == "GREEN" and said(str(bounce / "Unit.trx")) and len(kept) == 1
        and kept[0].name == "unreadable-test-results-Linux-shard-2-Unit.trx.bad", f"{torn.line} {kept}")
    stray = {"WISEOWL_COBOL_GATE_FIRST": "elsewhere", "WISEOWL_COBOL_GATE_ONLY": "elsewhere", "WISEOWL_COBOL_SHARD": "1/2",
             "VSTestTestCaseFilter": "FullyQualifiedName~Nothing"}
    os.environ.update(stray)
    try:
        observations = Path("run") / "Unit.observations.log"
        env = host_env(None, observations)
        ordered = host_env(Path("run") / "first-classes.txt", observations)
        retried = host_env(None, observations, Path("run") / "only-Unit.txt")
    finally:
        for k in stray:
            os.environ.pop(k, None)
    expect("a test host's environment is scrubbed of a stray ordering file, retry file, shard and VSTest property, and "
        "carries this gate's own first classes and retry file only when it has them",
        not set(stray) & set(env) and ordered[FIRST_CLASSES] == str(Path("run") / "first-classes.txt")
        and "WISEOWL_COBOL_SHARD" not in ordered and "VSTestTestCaseFilter" not in ordered
        and ONLY_CASES not in ordered and retried[ONLY_CASES] == str(Path("run") / "only-Unit.txt")
        and FIRST_CLASSES not in retried)
    expect("a test host writes the harness observation log into the run directory, one file per host (PB3108)",
        env.get("WISEOWL_COBOL_HARNESS_LOG") == str(observations) == ordered.get("WISEOWL_COBOL_HARNESS_LOG"),
        str(env.get("WISEOWL_COBOL_HARNESS_LOG")))


def case_01_population_cannot_be_listed_not_run_population(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("population", plant={"population"})
    expect("a population that cannot be listed is NOT RUN [population], exit 2, and the one verdict reader classifies "
        "it BRANCH: the tree's own test projects (review R2-1 of wave 1150)",
        o.verdict == "NOT RUN [population]" and o.exit_code == 2 and h.built
        and "planted: `--list-tests` exited 1" in o.line
        and read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)


def case_02_red_audit_tracked_rendered_view_known_before(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("audit", plant={"audit"})
    expect("a red audit (a tracked rendered view) that is known before the slot STOPS the gate before the build: RED, the "
        "audit named, no test ran",
        o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "NO TEST RAN" in o.line
        and h.audited and not h.built and not o.runs and not h.envs and sink[-1] == o.line
        and said("PLANTED-AUDIT-OUTPUT"), o.line)
    expect("the corpus fetch runs BEFORE the gate slot is taken, a red audit never queues for one (kb/Work PB2524), and "
        "the audits are no event of the main thread's order: they run beside it (kb/Work PB4326)",
        h.slots_taken == 0 and h.events == ["fetch"] and o.slot is None
        and gate("order")[1].events == ["fetch", "slot", "build"],
        str(h.events))


def case_03_audit_turns_red_while_gate_queues_slot(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("audit-slot", plant={"audit-slot"})
    expect("an audit that turns RED while the gate queues for the slot ends the gate AT ONCE (kb/Work PB5322): RED, the "
        "audit named, the ticket withdrawn, the slot never taken, nothing built, the verdict written and its line "
        "printed; a planted queue that never lets the gate in returns in seconds, not at the slot",
        o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line
        and "ticket was withdrawn and the slot never taken" in o.line and h.slots_taken == 0 and not h.ticket_live
        and o.slot is None and not h.built and not o.runs and h.events == ["fetch", "slot"]
        and sink[-1] == o.line and run_dirs(root / "audit-slot" / "runs")[-1].joinpath(VERDICT_FILE).exists()
        and o.timings.get("slot_wait_s", 99) < 30, f"{o.line} {h.events}")


def case_04_gate_test_phase_writes_heartbeat_line_console(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "slow-host"
    d.mkdir()
    slow = FakeHost(d, d / "gate.lock", plant={"slow-host"})
    beats: list[str] = []
    def console(line: str) -> None:
        beats.append(line)
        if "tests running" in line:
            slow.beat_seen.set()
    steps = iter(range(0, 10**9, 100))   # the arm's own clock: 100 s a reading, a beat every 300 s
    o = Gate(slow, d / "runs", say=console, heartbeat=lambda: Heartbeat(300.0, lambda: float(next(steps))),
             heartbeat_poll_s=0.01).run()
    expect("a gate in its test phase writes a heartbeat line to its console while its hosts run and say nothing "
        "(kb/Work PB5097), and the line names the hosts finished and the seconds since they started; the clock is "
        "the arm's own, so no wall-clock limit decides it",
        o.verdict == "GREEN" and any(re.search(r"build-local: tests running, 0 of \d+ test host\(s\) finished, \d+ s since the "
                                               r"hosts started", b) for b in beats), f"{o.line} {beats[-5:]}")


def case_05_audit_red_only_after_test_hosts_have(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("audit-late", plant={"audit-late"})
    expect("an audit that is RED only after the test hosts have started STOPS them: the verdict is RED, the audit "
        "named, the hosts' partial results are NOT measured (no population) (kb/Work PB4326)",
        o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "STOPPED them" in o.line
        and h.built and not o.runs and not o.populations and h.stopped_by_audit.is_set()
        and sink[-1] == o.line and said("PLANTED-AUDIT-OUTPUT"), f"{o.line} {h.events}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
