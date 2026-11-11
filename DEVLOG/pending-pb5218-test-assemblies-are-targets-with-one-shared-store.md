## PB5218: the three test assemblies are targets with declared inputs, each with its own key, their passes in one store every worktree shares

The owner approved the build redesign of kb/Work PB5207 on 2026-11-10: "This is precisely what the Unix make and Windows
MSBuild utilities are for. They rerun only what is required due to changes." This is its first stage.

**What changed.** `scripts/buildgraph.py` holds the model. A target names its inputs as path patterns
(`scripts/build_targets.json`, every entry naming its reader). Its key is a digest of its name, its facets (platform,
configuration, toolchain) and the git blob ids of the tracked paths its inputs match. Its passes live in one store under the
git common directory, so every worktree sees them. The gate now asks the store per target: a target with a pass at its key
starts no host, and the others run. PB5099's rule still holds per target: an earlier red run of the same worktree leaves only
its unpassed cases to run. A host that ends green with its population EXACT records its pass. The single population key of
PB5205 is gone, with `populations_green` and the COMPOSED answer; when every target is up to date and no script changed, the
line says `EVERY TARGET UP TO DATE` and names each pass's run and worktree. `--rederive` stores the passes it proves exact,
and its record now states the exact residue (PB5205 review R1-2). A recorded GREEN measured under another toolchain is no
longer REUSED.

**How the inputs were found.** Unit reads nearly every path a compiler test may read, so for now its inputs are all population
inputs; stage 2 splits it. For Conformance, a traced run of the suite's generator showed that every Conformance host reads the
traceability inventory of both editions, both rule catalogs and the ISO text. A new gate audit, BUILD TARGETS, reads every
test source and shared helper each assembly uses and fails on a repository read outside its declared inputs. It found
`tests/doc-facts/`, which no earlier list named.

**What a records-only change costs now.** A change to `tests/version-matrix/inventory*` re-runs Unit and Conformance, and not
Characterization. It cannot skip Conformance, because the suite's generator refuses a test that names a rule the inventory
lacks. Stage 2 can key Conformance on the generator's output instead. A docs, kb or tooling-lane change re-runs no target, in
any worktree. Dry run on real trees: records-85 (e1a0e2f573) and records-85b (ca5bfe1b03) share the characterization key
`811295d200b6…` and differ in unit and conformance.

**Proof.** `gate.py --self-test` passes 109 arms; twelve of the new target arms fail against the pre-stage-1 driver.
`buildgraph.py --self-test` passes 7 arms, and `buildgraph.py check` is GREEN over the three assemblies. The full commands and
verdict lines are in kb/Work PB5218.
