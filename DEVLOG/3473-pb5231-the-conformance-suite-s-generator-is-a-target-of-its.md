## Entry 3473 — 2026-11-11 05:35 PST — PB5231: the conformance suite's generator is a target of its own, and Conformance is keyed on the suite it generates

Stage 1 of the build redesign (PB5218) found that every Conformance host first ran `conformance-suite/harness/generate.py`,
which reads the traceability inventory. So a change that only recorded verdicts in the inventory re-ran all ~23,700
Conformance cases, though it almost never changes the suite. This is the second half of stage 2 of kb/Work PB5207.

**What changed.** `scripts/buildgraph.py` now has two kinds of target. A test target records a pass. A generated target runs a
Python command that writes declared outputs and records their digest. A target can depend on a generated target, and the
dependency adds its OUTPUT DIGEST to the dependent's key, never its inputs. The graph is checked when it loads: an unknown
dependency, a dependency on a test target and a cycle are each refused by name. `scripts/build_targets.json` declares
`conformance-suite` (`generate.py --out`), and `conformance` depends on it. The inventory, the rule catalogs, the ISO text
and `scripts/spec/` left Conformance's inputs, because none of its sources reads them; the BUILD TARGETS audit confirmed it.

The gate brings the generated target up to date before it computes any key. "Up to date" means the store holds the output
digest at the target's key AND the outputs on disk have that digest; otherwise the generator runs. It runs under a Python
audit hook, and the gate refuses it by name if it reads a tracked path outside its declared inputs, starts a process,
writes an undeclared output, or fails to reproduce the digest recorded at its key. The gate then hands the output directory
to the Conformance host and its listing in `WISEOWL_COBOL_GATE_SUITE`. `SuiteManifest` reads that directory instead of
generating its own, so the key a pass is recorded under is the digest of the suite the host ran. After the hosts the gate
digests the directory again, and a change makes the gate RED and records no pass. The verdict line names the generation:
`conformance-suite generated in 11.7 s (output 01de7eeef7de, 5 file(s))`.

The first gate of this branch was RED, and rightly. The trace compared the paths the generator opened with the
worktree's path as it was handed. The generator opens canonical paths (`Path(__file__).resolve()`), and under the self-test
runner's junction TEMP the two spellings differ, so no read was judged at all. The trace now accepts both spellings of the
worktree; the pre-fix code fails that arm under a junction TEMP and the fixed code passes.

**Measured.** Of the ten latest landings on main that touched the inventory, stage 1 re-ran Conformance at all ten. Five also
changed `src/` or the suite, so Conformance re-runs for them under this change too. The other five were records-only: #1486
records-85b (446 paths), #1446 classify-85 (25), #1429 (175), #1419 (33) and #1415 (10). The generated suite's digest was
the same before and after each of them, so Conformance is now up to date there. Each saves one Conformance host, which took
867 to 2,616 s in recent green gates, and costs one generation: 8 to 18 s untraced, about 12 s traced.

**Determinism.** Three traced generations at one key gave one digest, `01de7eeef7de…`: this worktree with a random hash seed,
the same worktree with `PYTHONHASHSEED=0` after its outputs were removed, and a second worktree at another path with
`PYTHONHASHSEED=12345`. The generator under Python 3.14 on Linux (WSL) wrote the same digest.

**Proof.** `buildgraph.py --self-test` PASS, 19 arms (7 before). `gate.py --self-test` PASS, 118 arms (109 before); with the
hand-off and the after-host check stubbed out, those two arms FAIL. `buildgraph.py check` GREEN, with SuiteManifest's read of
`generate.py` counted as reached through the dependency.

**Left.** CI is unchanged: its `dotnet test` gets no hand-off and generates as before. When PB5235 makes CI compute keys, its
Conformance shards must take the suite from the graph the same way. A declared time bound for a generated target belongs to
PB5232's per-target seconds.

