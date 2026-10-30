## The orchestrate self-tests quote the run's own reason when a check fails (kb/Work PB4046, step 1; PB4083 filed)

`test_orchestrate_adopt.ps1` failed in CI's Windows self-tests a second time on 2026-10-29: PR #791, merge-group run
40987962746. Both failures said only "got 'False', want 'True'". In each, adopt_main KEPT the old commit for a reason
other than the expected one, and the harness threw away the run's output, where adopt_main prints that reason.

The shared harness, `testdata/orchestrate_test_lib.ps1`, now keeps the newest run's output. A failed check quotes the lines
that match the part's evidence pattern. The adopt part names `adopt main:`, so the next failure prints adopt_main's own
outcome and reason. Six local runs passed 21/21, and the 15 orchestrate parts pass with the change.

kb/Work/PB4046 records the second witness and rules out three causes: automatic git maintenance (self_tests.py's private
global config turns it off for every child git), the impact-map recorder (the scratch checkout has no recorder) and a git
warning read as a modified file (adopt_main adds stderr only when git fails). The note stays open until a failure names its
cause. Ruling out the first exposed a gap that is real for hand runs: the PowerShell self-tests build their repositories
outside `scratch_repo.py`'s fixture, so a run outside the runner starts git maintenance in them. That is filed as PB4083.
