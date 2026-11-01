## Entry 2931 — 2026-11-01 01:17 PST — PB4329: CI's tests run as weighted shards, the Guard job leaves the merge group (step 6 of the pipeline redesign PB4323)

The merge group's matrix split the tests by NAME: six Conformance filters per OS, kb/Work A14's, beside one whole Unit +
Characterization job per OS, and the Guard job (the NIST corpus through the `cobol` CLI) beside them. Its critical path
was the Guard job (14 minutes) and Windows Unit (8 minutes of tests); merge-group run 41284754898 took 14.7 minutes. This
entry replaces the split with shards dealt by measured time, and moves Guard to the nightly (owner decision 2,
2026-10-31; the nightly job has run it since PB4332).

**The shard hook.** `GateTestFramework` (`tests/_shared/GateLegs.cs`) reads `WISEOWL_COBOL_SHARD=k/N`. `TestWeights.Deal`
takes every class `tests/_shared/test-weights.json` names, of every assembly, heaviest first to the lightest of N shards
(ties to the lowest), then the run's classes the file does not name at its `default_seconds`, and the host runs the cases
of shard k's classes. Dealing all three assemblies' classes in one pass balances the shards over the three assemblies a
shard job runs. The deal is a function of the file, N and the classes present, so every shard of one run partitions the
population; discovery never sees the variable, so `--list-tests` stays whole. A malformed value, or a shard beside the
local gate's leg handshake, refuses every case. The weights file is embedded in every test assembly by
`tests/Directory.Build.props`, so a host deals by the weights it was built with and reads no path.

**The weights.** `scripts/shard_weights.py generate` reads the newest three successful merge-group runs' trx: per run and
OS the sum of each class's result durations (INV-1's re-run of a NIST class included, since it runs in its class's
shard), per class the median. The first file is 1,088 classes from runs 41284663605, 41284754898 and 41286199098, and
its deal puts 5 and 7 shards within 1 % of the mean. The notes put the hook, the file and `generate` in step 5 (PB4328);
they are built here because the matrix is their one user, and step 5 builds its local orderer from them (PB4328's sites
now say so).

**The workflow.** `linux-shard` (5, Debug, under the inert watch, INV-1-strong a step of each, 15-minute bound) and
`windows-shard` (7, Release, warnings as errors, 20) each build the solution and run Unit, Characterization and
Conformance. `guard`, `inv1-strong-2023`, `conformance-population`, `greenfield-conformance`, `greenfield-unit`,
`windows-conformance` and `windows-build-test` are deleted. The `--built` self-tests became their own job,
`linux-built-self-tests`: as a step their 122 s would have made one shard the longest, and the weights deal only tests.
`ci-gate` folds in the population check: it downloads every `test-results-<OS>-shard-<k>` artifact and runs
`test_population.py check` per OS and assembly against the one listing shard 1 makes per OS (3 listings an OS where the
name split made 12), and the new `check --method NistProgram_MatchesGolden` over INV-1's five trx, so a case dealt to no
shard or two is a named NEVER RAN or RAN TWICE. `WISEOWL_COBOL_SHARD` is a scrubbed variable (`test_population.is_scrubbed`),
so no local `dotnet test` caller can run a part of an assembly through it, and `audit-callers` lets a workflow set it only
where that workflow runs the check.

**Probes.** The Unit assembly run as 7 local shards and checked: `=== POPULATION WiseOwl.Cobol.Tests.Unit.csproj (7 probe
shards): EXACT — 39,119/39,119 cases ran (skipped 0) across 7 trx ===`. Characterization as 3: EXACT 36/36, and the shard
dealt none of its classes exits 0 and writes a trx, so the check's glob always finds three. The nightly's `shard-spread`
report is live from the first nightly after landing (`weighted_shards_in_force`).

**Tests.** `GateLegDriftTests` gains `Shard_*`: the variable's spellings, a hand-worked deal (two assemblies, two unnamed
classes), a planted-case partition at 1, 2, 3 and 7 shards (every case once, discovery order kept, no class split), the
weights reader's eight refusals, and the committed file's deal at 5 and 7 shards within 1.10 of the mean. Each gated
assembly's `GateLegDriftTests` asserts it embeds the weights. `shard_weights.py --self-test` and `test_population.py
--self-test` gain the `generate`, `--method` and shard-scrub arms. `SelfTestDiscoveryDriftTests`, `LinuxGateDriftTests`
(the nightly's Linux jobs are read too: the guard leg reproduces a nightly red) and `FilterPopulationGuardDriftTests` (the
checking STEP is read, since `ci-gate` also counts its `needs`) follow the new jobs.

No inventory row (a process step). LINUX GATE: not part of the per-change gate (PB4327).

