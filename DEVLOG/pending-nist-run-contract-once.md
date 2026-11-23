## The NIST run environment is one file both legs read; the shared test harness is one namespace per folder

kb/Work PB3199 (cluster PB1754, an R2 duplication finding). The NIST corpus runs on two legs: in process
(`NistRunner`, the Conformance assembly) and over the `cobol` CLI (`scripts/guard-run-group.sh`, CI's guard). The note
found four places where the two had drifted. Two were already closed: PB804 made the report one rule (`<test>.txt`,
else stdout) and PB5460 made the acceptance basis one rule held to `tests/nist/normalization-cases.json`. Two were
still open, and the re-probe confirmed both on the pre-change runner, through two new `guard-verify.sh --self-test`
witnesses:

- **The environment.** `CutRunner.RunExit` pins `COBOL_USER_LOCALE` and `COBOL_SYSTEM_LOCALE` to `INVARIANT` and sets
  `WISEOWL_COBOL_MCS` to the run directory for every program. The shell exported `COBOL_SWITCH_1=ON` and nothing
  else, so a CLI-leg program ran in the host's default locale. The witness showed it: "did not see:
  COBOL_USER_LOCALE=INVARIANT COBOL_SYSTEM_LOCALE=INVARIANT WISEOWL_COBOL_MCS=…".
- **The exit status.** `NistRunner.Run` fails a non-zero exit before it reads the report. `guard_output_verdict`
  compared first and scored a matching report `MATCH` whatever the exit status was. A correct report from a run that
  exited 1, with or without a diagnostic, or that timed out, was green on the CLI leg and red in process.

The fix:

- `tests/nist/run-environment.tsv` holds the run environment as data. Each line is `NAME<TAB>VALUE`, and
  `{run-directory}` stands for the program's own run directory. Two readers use it: `NistRunEnvironment` in C#, which
  `NistRunner.Run` passes to every predecessor and to the program, and `guard_run_environment` in shell, which the
  runner calls for each program. Both readers refuse a malformed line or a repeated name, and say why.
  `NistRunEnvironmentTests` checks three things: the file declares every variable `CutRunner.RunExit` sets, the shell
  runner exports no variable of its own, and the parser refuses what it should. The shell witness checks that a
  program the CLI leg runs sees every declared variable, and that a malformed file stops the runner without scoring
  anything. `lane_classification.py` lists the file as a tooling read.
- In `guard_output_verdict`, a matched report counts as `MATCH` only when the run exited zero. Otherwise the RUN arm
  decides. With a diagnostic the verdict is `RUN FAILED — REGRESSION! (exit N: …; the report matched its golden)`.
  With a timeout, a signal or no diagnostic it is a RUN NO-VERDICT. `guard-nist-audit.sh` recognizes the new word.
  Three witnesses cover the rule, and all three were red on the pre-change code.

DESIGN-test-build-ci §1.2 now describes the shared environment and the exit-status rule. No inventory row changes: this
is test-harness process work only. The branch's gate after this half, run at the cherry-pick onto main:
`=== BUILD-LOCAL GATE: GREEN — Conformance 29,090/29,090 ran · Unit 42,693/42,693 ran · RepoDrift 232/232 ran · …`
(run 000001-20261123T012250Z).

kb/Work PB3200 (cluster PB1754, an R2 architecture finding). `tests/Directory.Build.props` links every file of
`tests/_shared/` into every test project, so DESIGN-architecture-review §8.2 rule 1 ("the project's root namespace plus
its folder") named a different namespace for each project, and nothing measured the folder: the census inventories
`src/` alone. The re-probe found the folder in two namespaces (`WiseOwl.Cobol.Gate` for `GateLegs.cs` and
`GateLegAudit.cs`), `GateLegs.cs` declaring 11 top-level types and named for none of them, the partition audit inside the
mechanism's file, the slot ladder's 33 public types in `TestPartitioning.cs`, `FileOrg` public beside `FileHarness`, and
`ProcessObserver` in a file named for its result record. The note's own type list was stale (PB4328 and PB4329 had
deleted GatePlan, GateHandshake and their kin).

The fix gives the linked harness its own root, stated in DESIGN-architecture-review section 8.2 (Tests): the folder is `WiseOwl.Cobol.Tests.Shared`, each
subfolder extends it, and the props file links it recursively under its repository path. The gate's framework moved to
`tests/_shared/Gate/` (`WiseOwl.Cobol.Tests.Shared.Gate`), one file per type or closed family: GateTestFramework.cs (the
framework, its executor and runner), GateShard.cs, GateOnly.cs, TestWeights.cs, GateOrderer.cs, GateOrder.cs and
GateLegAudit.cs. TestPartitionAudit, the slot ladder (ITestPartitionSlot.cs) and FileOrg each got their own file, and
ProcessObservation.cs is ProcessObserver.cs. Every caller changed in the same change: the framework attribute and the
weights resource name in the props, four GateLegDriftTests (RepoDrift's arm 4 now builds its expected attribute from
`typeof(GateTestFramework).FullName`), two drift exemptions, the two projects that drop the shared folder, and every
script, workflow and design document that named a moved file. `scripts/buildgraph.py` read only the folder's top level
when it traced which shared files a target uses, so the move would have hidden `Gate/` from the BUILD TARGETS audit; it
now reads the whole folder.

`SharedHarnessLayoutDriftTests` (RepoDrift) holds rules 1-3 over every committed file under `tests/_shared/`. Each rule is
first driven by a planted break, and a closed family (a root and the public types that name it as their base) is the one
exception to rule 2, as that section states it. The census half of the note is not built: rule 6's per-type columns do not exist
for `src/` either, and that section now records that they resolve a linked document by its repository path when they reach the
reader projects. No `src/` file changed, and no inventory row changes.
