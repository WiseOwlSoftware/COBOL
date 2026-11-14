#!/usr/bin/env python3
"""The gate's population verdicts on a planted machine (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""
import json
from collections import Counter
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)

import sys

import fixtures
import selftest_cases
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
FIRST_CLASSES = gate_mod.FIRST_CLASSES
RECORD_SCHEMA = gate_mod.RECORD_SCHEMA
RUNS_LOCK = gate_mod.RUNS_LOCK
RUN_DIR = gate_mod.RUN_DIR
VERDICT = gate_mod.VERDICT
VERDICT_FILE = gate_mod.VERDICT_FILE


def case_00_whole_planted_population_green_every_assembly_exact(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("green")
    expect("a whole planted population is GREEN, every assembly EXACT, one slot taken",
        o.verdict == "GREEN" and o.exit_code == 0 and all(p.exact for p in o.populations.values())
        and len(o.populations) == len(GATED_ASSEMBLIES) and h.slots_taken == 1, o.line)
    expect("every assembly runs its WHOLE population in one host, handed no narrowing and no first classes",
        sorted(h.envs) == sorted(GATED_ASSEMBLIES) and sorted(r.asm for r in o.runs) == sorted(GATED_ASSEMBLIES)
        and all(FIRST_CLASSES not in v for v in h.envs.values()) and o.first == [], str(h.envs.keys()))
    o2 = [l for l in sink if l.startswith(VERDICT)]
    expect("the verdict line is the last line, and counts every assembly's discovered cases",
        sink[-1] == o.line and "Conformance 6/6" in o.line and "Unit 4/4" in o.line
        and "Characterization 2/2" in o.line and len(o2) == 1 and "LEG" not in o.line, o.line)
    runs = sorted(p.name for p in (root / "green" / "runs").iterdir() if p.name != RUNS_LOCK)  # its run numbers' lock
    rec = json.loads((root / "green" / "runs" / runs[0] / VERDICT_FILE).read_text(encoding="utf-8"))
    expect("the run directory is fresh and holds the verdict file, every listing, trx and log, and the record is "
        f"schema {RECORD_SCHEMA}",
        len(runs) == 1 and RUN_DIR.fullmatch(runs[0]) is not None and rec["verdict"] == "GREEN"
        and rec["schema"] == RECORD_SCHEMA
        and sorted(p.name for p in (root / "green" / "runs" / runs[0]).glob("*.trx"))
        == sorted(f"{a}.trx" for a in GATED_ASSEMBLIES)
        and len(list((root / "green" / "runs" / runs[0]).glob("listing-*.txt"))) == len(GATED_ASSEMBLIES), str(runs))
    expect("a GREEN code-lane gate leaves ONE growth sample (kb/Work PB4333): its run, the tests_s its record holds and "
        "the Conformance cases it discovered",
        h.growth == [(runs[0], rec["timings"]["tests_s"], 6)] and "tests_s" in rec["timings"], str(h.growth))


def case_01_growth_sample_cannot_be_written_said_never(expect):
    root, sink, gate, said = fixtures.world()
    o, hg = gate("growth-unwritable", plant={"growth-unwritable"})
    expect("a growth sample that cannot be written is said and never changes a GREEN verdict",
        o.verdict == "GREEN" and o.exit_code == 0 and hg.growth == [] and said("the growth sample was not recorded"),
        o.line)


def case_02_dropped_case_never_ran_gate_red(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("drop", plant={"drop"})
    expect("a dropped case is NEVER RAN, and the gate is RED",
        o.verdict == "RED" and sum(o.populations["Conformance"].never_ran.values()) == 1
        and said("NEVER RAN"), o.line)


def case_03_case_run_twice_ran_twice_gate_red(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("dup", plant={"duplicate"})
    expect("a case run twice is RAN TWICE, and the gate is RED",
        o.verdict == "RED" and sum(o.populations["Conformance"].ran_twice.values()) == 1, o.line)


def case_04_trx_definition_listing_never_printed_not_population(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("foreign", plant={"foreign"})
    expect("a trx definition the listing never printed is NOT IN THE POPULATION, and the gate is RED",
        o.verdict == "RED" and "Conformance POPULATION NOT EXACT" in o.line
        and o.populations["Conformance"].not_listed == Counter({"N.Conf.NeverListed": 1}), o.line)


def case_05_skipped_case_counted_skipped_never_ran_gate(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("skip", plant={"skip"})
    expect("a skipped case is counted skipped, never as ran — and the gate stays GREEN",
        o.verdict == "GREEN" and o.populations["Conformance"].skipped == 1 and "skipped 1" in o.line, o.line)


def case_06_host_wrote_trx_unmeasured_gate_red(expect):
    root, sink, gate, said = fixtures.world()
    o, _ = gate("no-trx", plant={"no-trx"})
    expect("a host that wrote no trx is UNMEASURED, and the gate is RED",
        o.verdict == "RED" and "Unit POPULATION UNMEASURED" in o.line, o.line)


def case_07_red_host_makes_gate_red_exit_1(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("red", plant={"red"})
    expect("a red host makes the gate RED, exit 1, and every other host still ran its whole population",
        o.verdict == "RED" and o.exit_code == 1 and "Conformance RED" in o.line and len(o.runs) == len(GATED_ASSEMBLIES)
        and o.populations["Unit"].exact and "first_red_s" in o.timings and h.growth == [], o.line)
    expect("a red host's whole output, its failure message included, reaches the console through test_leg_report.py",
        said("PLANTED-RED-MESSAGE") and said("Conformance: RED"), "\n".join(sink[-20:]))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
