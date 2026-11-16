#!/usr/bin/env python3
"""THE GATE'S PREFLIGHT on a planted machine (kb/Work PB5872; DESIGN-test-build-ci.md section 3.14.15): a red one ends the
gate before the population starts, a green one leaves the population to run less its cases with no case run twice, a
declared class that runs no case is red, and the audits and the inert and tooling lanes are unchanged (no preflight)."""
import json
import sys

import fixtures
import selftest_cases
from gate_fake_host import FakeHost
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
Gate = gate_mod.Gate
from gate_host import ONLY_CASES
from gate_runs import VERDICT_FILE, run_dirs
from gate_verdict import PREFLIGHT, read_verdict


def _pinned_world(root, name: str, plant: set[str] = frozenset()):
    """A planted machine whose Unit holds a pin class (`N.Pin`) beside its own, and whose preflight declares the planted
    Characterization whole and that pin class: one whole assembly and one part of another."""
    d = root / name
    d.mkdir()
    host = FakeHost(d, d / "gate.lock", plant=set(plant), preflight={"Characterization": ("N.Char",), "Unit": ("N.Pin",)})
    host.POPULATION = {**host.POPULATION, "Unit": [*host.POPULATION["Unit"], "N.Pin.A", "N.Pin.B"]}
    return d, host


def case_00_a_preflight_red_ends_the_gate_before_the_population(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("preflight-red", plant={"preflight-red"})
    run = run_dirs(root / "preflight-red" / "runs")[-1]
    rec = json.loads((run / VERDICT_FILE).read_text(encoding="utf-8"))
    expect("a red case in the preflight ends the gate RED at once (exit 1), its line saying PREFLIGHT and naming the "
           "case; no population host started, so nothing of the whole population ran",
           o.verdict == "RED" and o.exit_code == 1 and f"{PREFLIGHT} RED: Characterization N.Char.A" in o.line
           and "the whole population did NOT run" in o.line and h.hosts_run == ["preflight-Characterization"]
           and not o.runs and not o.populations and h.slots_taken == 1 and h.built and sink[-1] == o.line
           and read_verdict(o.line.split(": ", 1)[1].removesuffix(" ===")).word == "RED", o.line)
    expect("the record says PREFLIGHT: its verdict, the red case, the classes it ran and its seconds, and the first red "
           "is the preflight's, so a later measurement can count what the stage saved",
           rec["preflight"]["verdict"] == "RED" and rec["preflight"]["red"] == ["Characterization: N.Char.A"]
           and rec["preflight"]["classes"] == {"Characterization": ["N.Char"]}
           and rec["preflight"]["cases"] == {"Characterization": 2}
           and "preflight_s" in rec["timings"] and "first_red_s" in rec["timings"] and "tests_s" not in rec["timings"]
           and h.growth == [], json.dumps(rec["preflight"]))


def case_01_a_green_preflight_runs_the_population_less_its_cases(expect):
    root, sink, gate, said = fixtures.world()
    d, h = _pinned_world(root, "preflight-green")
    o = Gate(h, d / "runs", say=sink.append).run()
    unit_only = h.envs["Unit"].get(ONLY_CASES)
    expect("a GREEN preflight leaves the whole population to run, each case ONCE: the assembly the preflight ran whole "
           "starts no population host, the other's host runs everything but the preflight's class, and the population "
           "check, reading both trx files, is EXACT for every assembly",
           o.verdict == "GREEN" and o.exit_code == 0 and fixtures.hosted_once(h)
           and "preflight-Characterization" in h.hosts_run and "Characterization" not in h.hosts_run and "preflight-Unit" in h.hosts_run and "Unit" in h.hosts_run
           and unit_only is not None and "N.Pin" not in open(unit_only, encoding="utf-8").read()
           and sorted(c for c in h.cases_run if c.startswith("Unit: ")) == sorted(
               f"Unit: {n}" for n in h.POPULATION["Unit"])
           and all(p.exact for p in o.populations.values()) and o.populations["Unit"].ran == 6
           and o.populations["Characterization"].ran == 2 and "Unit 6/6 ran" in o.line, f"{o.line} {h.hosts_run}")
    expect("the line and the record say the preflight ran GREEN, and its cases are counted as ran, never as carried "
           "from an earlier run",
           f"{PREFLIGHT} GREEN: 4 case(s) of 2 class(es)" in o.line and o.preflight["verdict"] == "GREEN"
           and o.carried == {} and o.composed_from == [] and "carried from run" not in o.line
           and said("the preflight ran Unit 2, Characterization 2 case(s)"), o.line)


def case_02_a_case_run_by_both_is_ran_twice(expect):
    root, sink, gate, said = fixtures.world()
    d, h = _pinned_world(root, "preflight-twice", plant={"ignore-only"})
    o = Gate(h, d / "runs", say=sink.append).run()
    expect("a population host that runs a case the preflight already ran is RAN TWICE, and the gate is RED: the "
           "population check is what keeps a case from counting twice",
           o.verdict == "RED" and sum(o.populations["Unit"].ran_twice.values()) == 2
           and "Unit POPULATION NOT EXACT" in o.line, o.line)


def case_03_a_declared_class_that_runs_no_case_is_red(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("preflight-gone", preflight={"Characterization": ("N.Char", "N.Gone")})
    expect("a declared class that runs no case (renamed or removed since it was declared) is a PREFLIGHT red naming "
           "it, never a silent green over nothing",
           o.verdict == "RED" and f"{PREFLIGHT} RED" in o.line and "N.Gone ran no case" in o.line
           and not o.runs, o.line)


def case_04_the_audits_and_the_other_lanes_are_unchanged(expect):
    root, sink, gate, said = fixtures.world()
    o, h = gate("audit", plant={"audit"})
    expect("an audit RED before the slot still ends the gate first: no preflight host and no population host starts",
           o.verdict == "RED" and "RENDERED VIEWS RED" in o.line and "NO TEST RAN" in o.line and h.hosts_run == []
           and o.preflight == {} and not h.built, o.line)
    oi, hi = gate("inert", plant={"inert"})
    ot, ht = gate("tooling-lane", plant={"tooling-lane"})
    expect("the inert and the tooling lanes run no preflight: no build, no host, nothing recorded for it",
           oi.verdict == "GREEN" and ot.verdict == "GREEN" and hi.hosts_run == [] == ht.hosts_run
           and oi.preflight == {} == ot.preflight and PREFLIGHT not in oi.line + ot.line, f"{oi.line} {ot.line}")
    on, hn = gate("none", preflight={})
    expect("a gate whose targets declare no preflight runs the whole population as before, one host per assembly",
           on.verdict == "GREEN" and on.preflight == {} and sorted(hn.hosts_run) == sorted(
               ["Conformance", "Unit", "RepoDrift", "Characterization"]) and PREFLIGHT not in on.line, on.line)


def case_05_a_regate_after_a_preflight_red(expect):
    root, sink, gate, said = fixtures.world()
    d = root / "regate"
    d.mkdir()
    red = Gate(FakeHost(d, d / "gate.lock", plant={"preflight-red"}), d / "runs", say=sink.append).run()
    same = FakeHost(d, d / "gate.lock")
    again = Gate(same, d / "runs", say=sink.append).run()
    changed = FakeHost(d, d / "gate.lock", trees=["tree-2"])
    fixed = Gate(changed, d / "runs", say=sink.append).run()
    expect("a re-gate at the same keys after a preflight red carries the preflight's passes (its trx is the run's) and "
           "retries only its red case, with no preflight host for that assembly (kb/Work PB5099); a re-gate at a changed "
           "key runs the preflight again",
           red.verdict == "RED" and again.verdict == "GREEN" and "preflight-Characterization" not in same.hosts_run
           and [c for c in same.cases_run if c.startswith("Characterization: ")] == ["Characterization: N.Char.A"]
           and again.carried.get("Characterization") == 1 and fixed.verdict == "GREEN"
           and "preflight-Characterization" in changed.hosts_run and "Characterization" not in changed.hosts_run,
           f"{again.line} {same.hosts_run} {fixed.line} {changed.hosts_run}")


def case_06_the_real_targets_declare_the_preflight(expect):
    from gate_host import Host  # noqa: PLC0415
    import buildgraph  # noqa: PLC0415
    real = Host().preflight()
    graph = buildgraph.load_targets()
    expect("the real host's preflight is the targets file's declaration (scripts/build_targets.json), bounded by its "
           "budget, and it holds the Characterization target's classes",
           real == graph.preflight() and graph.preflight_budget is not None and "Characterization" in real
           and "WiseOwl.Cobol.Tests.Characterization.EmittedCSharpSnapshotTests" in real["Characterization"], str(real))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
