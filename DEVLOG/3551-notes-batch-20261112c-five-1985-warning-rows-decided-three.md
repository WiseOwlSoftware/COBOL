## Entry 3551 — 2026-11-12 15:24 PST — Notes batch 20261112c: five 1985 warning rows decided, three nightly findings filed

The five warning-only rows of the 1985 85n pass (`1985-GR-VII.4.6.4-2`, `1985-GR-VIII.4.8.4-2`, `1985-GR-IX.4.8.4-2`, `1985-GR-XIII.4.8.4-2` and `1985-SR-XV.3.2.3-2`) are witnessed by the flagged form in their clause pass, and none closes without a witness. The 1985 standard says that the results of violating its formats or rules are undefined (§1985:I.1.5.2.2 and §1985:I.1.6 set what an implementation owes), so a compiler need not refuse any of the five, the syntax rule included, and none is an under-rejection. The decision is the dated file `kb/Work/PB4995/2026-11-12-warned-five-decided.md`, which also names the witness each row takes and the work a golden-lane worker owes.

The nightly run 43270832950 was triaged. Its three red jobs each now have a note or a branch: the `images` job is PB5458, the `latest` job has the branch `toolchain-20261112`, and the NIST guard job is PB5460. The bump procedure the `latest` branch exposed is PB5459. Three notes are filed:

- PB5458 (MINOR, tooling): `toolchain.py latest` reads git-linux from the runner-images readme, which lags the deployed image, so the `images` job is red on a version `bump` cannot see.
- PB5459 (MAJOR, tooling/orchestrator): a toolchain bump has no safe procedure while the loop runs, every gate stops `NOT RUN [toolchain]` between the host update and each branch's merge of main, and the refusal text says to reinstall.
- PB5460 (MAJOR, test-harness): the NIST CLI guard and `NistRunner.Normalize` treat a form feed differently, so SQ101M is a false red in the nightly guard since PB2509 and the in-process gate cannot tell a page eject from a line advance.

