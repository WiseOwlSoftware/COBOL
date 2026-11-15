## Notes of the Mythos lane, 2026-11-15 (second): PB5728, the week's quota exception, PB5680 answered

Filed: PB5728 (`kb/Work/PB5728.md`, defect, process only, area tests/gate, severity MINOR). A Linux gate removed its directory and ended while its self-test's process tree (`filter_population.py --self-test` and its `dotnet test --list-tests` child) lived on for 43 hours; both processes were ended by hand, and the note asks whether the EXIT path ends the legs' process trees as `on_signal` does and why the `--list-tests` call has no time bound. Evidence: the process listing of 2026-11-15 14:26 PST quoted in the note, from `E:/COBOL-coord/scratch/my-20261115/process-findings-20261115-b.md`.

Filed: `kb/Work/PB5357/2026-11-15-spend-ahead-to-90-both-accounts.md`. The owner lets both accounts spend ahead of the day line for the current week and pause at 90 % of the week; the file quotes his two statements (mail `20261115-103422`; the reply of 2026-11-15 14:39 PST) and marks the scope as the sessions' reading. `git ls-tree origin/main kb/Work/PB5357/` showed no 2026-11-15 file before this.

Filed: `kb/Work/PB5680/2026-11-15-answered-by-the-pb5692-sample.md`, and PB5680 is now `status: landed` with no rows closed. The PB5692 sample (60 rows drawn from PB5680's population, seed 20261115, run wf_f47dc191-edf, 6 of 60 not standing) is PB5680's measurement, and PB5692's ruling decides that every 1985 KEPT row is adjudicated at 1985. `kb/Work/PB5692/` is not on main yet, so the files are cited by their scratch paths (`E:/COBOL-coord/scratch/sample-shared/RESULT.md`, `E:/COBOL-coord/scratch/my-20261115/pb5692/2026-11-15-adjudication-of-the-sample.md`).

Plan section 0 changed in three bullets only: WHO RUNS WHAT now names the loop on account 2 since 2026-11-14 17:49 PST, the operator's 2002 lanes and the Mythos session's 1985 work; ONE OWN-EDITION RULE FOR EVERY OLDER EDITION gains the PB5692 sentence (PB5683, PB5682); PRIORITY gains the week's spend-ahead sentence. No figure changed.

None of these four changes closes an inventory row, and no code, script or inventory record was touched.
