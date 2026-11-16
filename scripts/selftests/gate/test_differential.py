#!/usr/bin/env python3
"""The program differential inside the gate (kb/Work PB4333; scripts/gate_differential.py; DESIGN-test-build-ci.md section
3.14.13): over a planted suite, which Conformance cases a gate carries as byte-identical to a passed case and which it runs,
each arm one input that changes (a program, an expected output, the harness, the runtime, the compiler alone), one case that is
new, one pass set that is missing and one capture that fails. Each arm can fail: its expectation names the cases that run."""
import json
import sys
from pathlib import Path

import selftest_cases
from gate_fake_host import FakeHost
from scratch_repo import isolate, scratch_dir
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
Gate = gate_mod.Gate
import buildgraph  # noqa: E402
import gate_differential  # noqa: E402
from gate_host import ONLY_CASES  # noqa: E402

NAMES = FakeHost.DIFFERENTIAL_POPULATION
HAND = "N.Conf.Hand"


def _world():
    """The planted suite: four accept tests t1..t4 and the refusal r5, their files, the harness, runtime and compiler paths."""
    manifest = {"generator": "conformance-suite/harness/generate.py",
                "editions": {"2023": [{"id": f"t{i}", "clause": "c", "source": f"cases/c/t{i}.cob", "source-format": "fixed",
                                        "rules": [], "files": [], "requires": [], "words": [], "expect": "accept",
                                        "implementor-defined": [], "expected-output": {"": f"cases/c/t{i}.out"},
                                        "tree": "cases", "portable": True, "in-suite": True, "options": {}}
                                       for i in range(1, 7)]},
                "diagnostics": {"r5": {"source": "cases/c/r5.cob", "source-format": "fixed", "files": [], "options": {},
                                       "refuse-at": ["2023"], "expect": "COBOL0001"}}}
    blobs = {f"conformance-suite/cases/c/{n}": f"{i:040x}" for i, n in enumerate(
        [*(f"t{i}.{e}" for i in range(1, 7) for e in ("cob", "out")), "r5.cob"], start=1)}
    blobs.update({"src/WiseOwl.Cobol.Runtime/Move.cs": "a" * 40, "src/WiseOwl.Cobol.Compiler/Emit.cs": "b" * 40,
                  "tests/WiseOwl.Cobol.Tests.Conformance/ConformanceSuiteRunner.cs": "c" * 40})
    oracle = {f"suite/2023/t{i}": (f"cs{i}", f"dg{i}", f"pg{i}") for i in range(1, 7)}
    oracle["refusal/r5@2023"] = ("-", "dg5", "-")
    return manifest, blobs, oracle


def case_00_the_differential_carries_exactly_the_unchanged_cases(expect):
    isolate()
    d = scratch_dir("gate-differential-selftest-")
    sink: list[str] = []
    n = [0]

    def gate(plant=frozenset(), change=None, new_case=False):
        """One gate in the worktree `d` at a tree of its own (so Conformance is never up to date at its key), its suite
        planted, `change` editing the planted world first."""
        n[0] += 1
        tree = f"tree-{n[0]}"
        host = FakeHost(d, d / "gate.lock", plant={"differential", *plant}, trees=[tree],
                        keys={tree: {"Unit": "u", "RepoDrift": "r", "Characterization": "c"}})
        host.suite_manifest, host.blobs, host.oracle = _world()
        if new_case:
            host.POPULATION["Conformance"] = [*host.POPULATION["Conformance"],
                                             NAMES[0].replace("_P1.", "_P5.").replace('"t1"', '"t6"')]
        if change:
            change(host)
        sink.clear()
        out = Gate(host, d / "runs", say=sink.append).run()
        only = host.envs.get("Conformance", {}).get(ONLY_CASES)
        ran = Path(only).read_text(encoding="utf-8").splitlines() if only else (
            list(host.POPULATION["Conformance"]) if "Conformance" in host.envs else [])
        return out, host, sorted(ran)

    def t(i):   # the display name of accept test t<i>
        return next(x for x in NAMES if f'"t{i}"' in x) if i <= 4 else None

    first, h1, ran1 = gate()
    store = gate_differential.CaseStore(d / "store" / gate_differential.CASE_DIR)
    sets = sorted(store.dir.glob("*.txt"))
    expect("with no pass set at the harness key the whole Conformance population runs, the capture runs BESIDE the hosts, "
           "and the pass the gate records leaves the five carried-family digests at that key",
           first.verdict == "GREEN" and ran1 == sorted(h1.POPULATION["Conformance"]) and h1.captures == 1
           and first.differential.get("stands") is False and first.differential.get("carried") == 0
           and "Conformance" in first.recorded and len(sets) == 1 and len(store.lookup(sets[0].stem) or ()) == 5,
           f"{first.line} | {first.differential} | {sets}")

    second, h2, ran2 = gate()
    expect("at the same harness key, with every program, diagnostic, entry and file unchanged, the five are CARRIED: only "
           "the case of no carried family runs, the population is EXACT, the pass is recorded and the line says so",
           second.verdict == "GREEN" and ran2 == [HAND] and second.differential.get("carried") == 5
           and second.populations["Conformance"].exact and second.populations["Conformance"].carried == 5
           and "Conformance" in second.recorded and "5 carried as byte-identical" in second.line
           and h2.growth == [] and h2.compiled == [], f"{second.line} | {ran2} | compiled {h2.compiled}")

    def program(host):   # a compiler change, after which it emits other bytes for t1
        host.blobs["src/WiseOwl.Cobol.Compiler/Emit.cs"] = "d" * 40
        host.oracle["suite/2023/t1"] = ("cs1", "dg1", "pg1-changed")
    changed, h3, ran3 = gate(change=program)
    expect("a changed PROGRAM is selected: after a compiler change every compile is made again (a new compiler key), and t1, "
           "whose program digest differs, runs, with the case of no family",
           changed.verdict == "GREEN" and ran3 == sorted([t(1), HAND]) and len(h3.compiled) == 5, f"{changed.line} | {ran3}")

    def expected_output(host):
        host.blobs["conformance-suite/cases/c/t2.out"] = "e" * 40
    out_changed, h4, ran4 = gate(change=expected_output)
    expect("a changed EXPECTED OUTPUT is selected: t2, whose named file's blob differs, runs, and only its compile is made "
           "again (the others are the capture store's at the same compiler key)",
           out_changed.verdict == "GREEN" and ran4 == sorted([t(2), HAND]) and h4.compiled == ["suite/2023/t2"],
           f"{out_changed.line} | {ran4} | compiled {h4.compiled}")

    def diagnostics(host):   # another compiler change, after which it reports r5 otherwise
        host.blobs["src/WiseOwl.Cobol.Compiler/Emit.cs"] = "7" * 40
        host.oracle["refusal/r5@2023"] = ("-", "dg5-changed", "-")
    diag_changed, _, ran5 = gate(change=diagnostics)
    expect("a changed DIAGNOSTIC STREAM is selected: the refusal r5 runs",
           diag_changed.verdict == "GREEN" and ran5 == sorted([NAMES[4], HAND]), f"{diag_changed.line} | {ran5}")

    new, _, ran6 = gate(new_case=True)
    expect("a NEW case runs: t6, listed for the first time, has no pass at its digest",
           new.verdict == "GREEN" and len(ran6) == 2 and HAND in ran6 and any('"t6"' in r for r in ran6), f"{ran6}")

    def compiler_only(host):   # a compiler source changes and every program it emits is byte-identical
        host.blobs["src/WiseOwl.Cobol.Compiler/Emit.cs"] = "f" * 40
    compiler, _, ran7 = gate(change=compiler_only)
    expect("a compiler change whose programs and diagnostics are byte-identical carries them all: the compiler is of the "
           "`program` class, out of the harness key",
           compiler.verdict == "GREEN" and ran7 == [HAND] and compiler.differential.get("carried") == 5, f"{ran7}")

    def harness(host):
        host.blobs["tests/WiseOwl.Cobol.Tests.Conformance/ConformanceSuiteRunner.cs"] = "9" * 40
    harnessed, hh, ran8 = gate(change=harness)
    expect("a HARNESS change runs everything: a new harness key, where no pass stands",
           harnessed.verdict == "GREEN" and ran8 == sorted(hh.POPULATION["Conformance"])
           and harnessed.differential.get("stands") is False, f"{ran8} {harnessed.differential}")

    def runtime(host):
        host.blobs["src/WiseOwl.Cobol.Runtime/Move.cs"] = "8" * 40
    runtimed, hr, ran9 = gate(change=runtime)
    expect("a RUNTIME change runs everything: the runtime is of the `everything` class, in the harness key",
           runtimed.verdict == "GREEN" and ran9 == sorted(hr.POPULATION["Conformance"]), f"{ran9}")

    for f in store.dir.glob("*.txt"):
        f.unlink()
    missing, hm, ran10 = gate()
    expect("a MISSING pass set runs everything (and the green gate records a new one)",
           missing.verdict == "GREEN" and ran10 == sorted(hm.POPULATION["Conformance"])
           and len(list(store.dir.glob("*.txt"))) == 1, f"{ran10}")

    def compiler_again(host):   # a compiler change: every compile must be made again, and the capture fails
        host.blobs["src/WiseOwl.Cobol.Compiler/Emit.cs"] = "6" * 40
    failed, hf, ran11 = gate(plant={"capture-fails"}, change=compiler_again)
    expect("a capture that FAILS carries nothing: everything runs, the verdict is the hosts' (GREEN), and nothing is recorded "
           "at the harness key from it",
           failed.verdict == "GREEN" and ran11 == sorted(hf.POPULATION["Conformance"]) and not failed.differential
           and any("the capture failed" in line for line in sink), f"{ran11} {failed.differential}")

    red, hred, ran12 = gate(plant={"red"}, change=program)
    expect("a carried selection hides no red: the changed case that runs and fails makes the gate RED, and a RED records "
           "no pass set addition for its digests",
           red.verdict == "RED" and t(1) in ran12 and "Conformance" not in red.recorded, red.line)


def case_01_the_classification_refuses_an_unclassified_input(expect):
    """THE DRIFT RULE (kb/Work PB4333): the targets file with one declared input of Conformance left out of its differential
    classes is refused by the graph's one reader, so the BUILD TARGETS audit is RED."""
    isolate()
    d = scratch_dir("gate-differential-graph-")
    data = json.loads(buildgraph.TARGETS_FILE.read_text(encoding="utf-8"))
    del data["targets"]["conformance"]["differential"]["classes"]["tests/differential/"]
    (d / "unclassified.json").write_text(json.dumps(data), encoding="utf-8", newline="\n")
    data = json.loads(buildgraph.TARGETS_FILE.read_text(encoding="utf-8"))
    data["targets"]["conformance"]["differential"]["classes"]["docs/nowhere/"] = ["case", "a path no input holds"]
    (d / "stray.json").write_text(json.dumps(data), encoding="utf-8", newline="\n")
    errors = {}
    for name in ("unclassified", "stray"):
        try:
            buildgraph.load_targets(d / f"{name}.json")
        except buildgraph.GraphError as e:
            errors[name] = str(e)
    graph = buildgraph.load_targets()
    expect("a declared input with no differential class, and a class under no declared input, are each a GraphError; the "
           "real targets file classes every input of Conformance",
           "tests/differential/" in errors.get("unclassified", "") and "docs/nowhere/" in errors.get("stray", "")
           and graph.by_assembly()["Conformance"].differential is not None, str(errors))


def case_02_a_case_the_rule_cannot_name_exactly_runs(expect):
    """Each case the digest cannot stand for exactly has no digest, so it can never be carried."""
    from collections import Counter
    manifest, blobs, oracle = _world()
    manifest["editions"]["2023"][1]["build"] = [{"file": "cases/c/t2.cob", "output": "T2"}]   # a multi-unit test
    del blobs["conformance-suite/cases/c/t3.out"]                                                # a named file untracked
    oracle["suite/2023/t4"] = ("!", "!", "!")                                                     # its compile threw
    oracle["suite/2023/t6"] = ("cs6", "dg6", gate_differential.CLOCK)                         # WHEN-COMPILED
    truncated = NAMES[0].replace('id: "t1")', 'id: "t1"...)')
    clock = NAMES[0].replace('"t1"', '"t6"')
    listed = Counter({NAMES[0]: 1, NAMES[1]: 1, NAMES[2]: 1, NAMES[3]: 1, NAMES[4]: 2, truncated: 1, HAND: 1, clock: 1})
    got = gate_differential.case_digests(gate_differential.case_inputs(listed, manifest, blobs), oracle)
    expect("only t1 has a digest: a multi-unit entry, an untracked named file, a capture that threw, a program that reads "
           "the compile clock, a name listed twice, a truncated display name and a case of no family each have none, by "
           "their reasons",
           sorted(got.by_name) == [NAMES[0]] and got.uncarriable == Counter({
               "an entry the rule does not carry (build steps, servers, a flag test)": 1,
               "a file its entry names is not tracked": 1, "no digest of its compile": 1, "listed more than once": 2,
               "its program reads the compile clock (WHEN-COMPILED)": 1,
               "not a carried family": 2}), f"{sorted(got.by_name)} {dict(got.uncarriable)}")


def case_03_a_set_no_gate_wrote_for_three_weeks_is_pruned(expect):
    """A pass set or capture whose harness or compiler key no tree has any more is deleted when another is written, its
    age measured on the file system's clock (the one that stamped it)."""
    import os
    isolate()
    store = gate_differential.CaseStore(scratch_dir("gate-differential-prune-"))
    old, recent = "a" * 64, "b" * 64
    store.record(old, ["c" * 64])
    store.record(recent, ["d" * 64])
    stamp = (store.dir / f"{recent}.txt").stat().st_mtime
    os.utime(store.dir / f"{old}.txt", (stamp - (gate_differential.PRUNE_DAYS + 1) * 86400,) * 2)
    store.record(recent, ["e" * 64])
    expect("the set not written for PRUNE_DAYS before the one just written is pruned; the written one keeps every digest",
           store.lookup(old) is None and store.lookup(recent) == frozenset({"d" * 64, "e" * 64}), str(store.lookup(recent)))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
