## The NIST run environment is one file both legs read, and both read the exit status before the report

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
is test-harness process work only.
