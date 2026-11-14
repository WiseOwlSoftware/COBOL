#!/usr/bin/env python3
"""The lanes of a change (the toolchain, INERT, TOOLING, post-build self-tests) (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import json
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)

import sys

import fixtures
import selftest_cases
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
from gate_audits import AUDITS, TOOLING_ONLY
from gate_verdict import GREEN_READING, NOT_RUN_CAUSES, VERDICT, read_verdict
Gate = gate_mod.Gate
from gate_host import Host, HostRun
Outcome = gate_mod.Outcome
from gate_runs import VERDICT_FILE, run_dirs


def case_00_script_self_tests_audits_tooling_only_post(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("tooling-no", plant=set())
    o2, h2 = gate("tooling-yes", plant={"tooling"})
    rec = json.loads(run_dirs(root / "tooling-yes" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
    rec_no = json.loads(run_dirs(root / "tooling-no" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
    expect("the script self-tests (the audits in TOOLING_ONLY, and the post-build ones) run ONLY when the change touches "
        "tooling: the audits are told so, the post-build half runs beside the hosts only then, and the record says "
        "which (kb/Work PB4326)",
        h.tooling_seen is False and h2.tooling_seen is True and "post-build self-tests" not in h.events
        and "post-build self-tests" in h2.events and rec["tooling"] is True and rec_no["tooling"] is False,
        f"{h.events} {h2.events} {rec['tooling']} {rec_no['tooling']}")
    expect("the audits a change that touches no tooling runs are AUDITS less TOOLING_ONLY (SELF-TESTS), in order; with "
        "tooling they are all of AUDITS (the real host; kb/Work PB4326)",
        [n for n, _ in Host(root).audit_commands(False)] == [n for n, _ in AUDITS if n not in TOOLING_ONLY]
        and "SELF-TESTS" not in [n for n, _ in Host(root).audit_commands(False)]
        and Host(root).audit_commands(True) == AUDITS and TOOLING_ONLY == ("SELF-TESTS",)
        and "SELF-TESTS" in [n for n, _ in AUDITS], str(TOOLING_ONLY))
    two = Outcome(runs=[HostRun("Unit", 0, False, 30.0, 30.0), HostRun("Conformance", 0, False, 60.0, 60.0)],
                  timings={"tests_s": 60.0})
    expect("the verdict file records, per assembly, the seconds its host ran and the hosts' effective concurrency "
        "(their summed wall seconds over the span they took; kb/Work PB4326)",
        sorted(rec_no["assembly_wall_s"]) == sorted(GATED_ASSEMBLIES)
        and all(isinstance(v, float) for v in rec_no["assembly_wall_s"].values())
        and isinstance(rec_no["effective_concurrency"], float)
        and Gate._concurrency(two) == 1.5 and Gate._assembly_walls(two) == {"Conformance": 60.0, "Unit": 30.0}
        and Gate._concurrency(Outcome()) is None and Gate._assembly_walls(Outcome()) == {},
        f"{rec_no['assembly_wall_s']} {rec_no['effective_concurrency']}")


def case_01_host_whose_tools_not_toolchain_env_s(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("toolchain", plant={"toolchain"})
    expect("a host whose tools are not toolchain.env's is NOT RUN [toolchain] before anything runs, naming the tool, "
        "exit 2, an ENVIRONMENT cause (kb/Work PB4007)",
        o.verdict == NOT_RUN_CAUSES["toolchain"].verdict and o.exit_code == 2 and h.toolchain_checked
        and "jdk 23.0.2 (toolchain.env 27.0.1)" in o.line and h.events == [] and not o.runs
        and not read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)


def case_02_gate_names_toolchain_ran_under_verdict_line(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("toolchain-named")
    expect("a gate names the toolchain it ran under, in its verdict line and its record (kb/Work PB4007)",
        "toolchain dotnet-sdk 10.0.403, python 3.15.2" in o.line and o.toolchain == "dotnet-sdk 10.0.403, python 3.15.2",
        o.line)


def case_03_change_inert_lane_kb_work_pb2878_runs(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("inert", plant={"inert"})
    expect("a change in the INERT LANE (kb/Work PB2878) runs the audits and then ends GREEN, with no fetch, slot, build "
        "or test host, the lane recorded",
        o.verdict == "GREEN" and o.exit_code == 0 and "INERT LANE (kb/Work PB2878)" in o.line
        and h.audited and h.events == [] and not h.built and not o.runs and o.lane == "inert"
        and o.tooling is False and h.tooling_seen is False and h.growth == []
        and read_verdict(o.line[len(VERDICT):]) == GREEN_READING, f"{o.line} {h.events}")
    expect("the gate hands its audits the change it classified its lane from: (the base, the measured tree) (kb/Work "
        "PB5225)", h.change_seen == ("0" * 40, "tree-1") and (o.base, o.tree) == h.change_seen,
        f"{h.change_seen} {o.base} {o.tree}")


def case_04_red_audit_red_inert_lane_too_audits(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("inert-audit", plant={"inert", "audit"})
    expect("a red audit is RED in the inert lane too: the audits ARE the inert lane's gate",
        o.verdict == "RED" and o.exit_code == 1 and h.audited and h.events == [] and o.lane == "inert"
        and "RENDERED VIEWS RED" in o.line, o.line)


def case_05_change_tooling_lane_kb_work_pb4395_runs(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("tooling-lane", plant={"tooling-lane"})
    rec = json.loads(run_dirs(root / "tooling-lane" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
    expect("a change in the TOOLING LANE (kb/Work PB4395) runs the audits WITH the script self-tests and then ends "
        "GREEN, with no fetch, slot, build or test host, its record saying `lane: tooling`",
        o.verdict == "GREEN" and o.exit_code == 0 and "TOOLING LANE (kb/Work PB4395)" in o.line
        and h.audited and h.tooling_seen is True and h.events == [] and not h.built and not o.runs
        and o.lane == "tooling" and rec["lane"] == "tooling" and rec["tooling"] is True and h.growth == []
        and read_verdict(o.line[len(VERDICT):]) == GREEN_READING, f"{o.line} {h.events}")


def case_06_red_audit_red_tooling_lane_audits_self(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("tooling-lane-audit", plant={"tooling-lane", "audit"})
    expect("a red audit is RED in the tooling lane: the audits and the self-tests ARE its gate",
        o.verdict == "RED" and o.exit_code == 1 and h.audited and h.events == [] and not h.built
        and o.lane == "tooling" and "RENDERED VIEWS RED" in o.line, o.line)


def case_07_red_post_build_self_test_one_reads(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("post-build", plant={"post-build", "tooling"})
    expect("a red post-build self-test (one that reads the built assemblies, kb/Work PB2563) runs beside the hosts and "
        "makes the gate RED after them, its output printed whole",
        o.verdict == "RED" and o.exit_code == 1 and "POST-BUILD SELF-TESTS RED" in o.line
        and len(o.runs) == len(GATED_ASSEMBLIES) and not any(r.red for r in o.runs)
        and said("PLANTED-POST-BUILD-RED"), o.line)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
