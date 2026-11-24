# PB5970 addendum (2026-11-24): five sequential-file NIST cases and the analyzer-once build, one gate

**Evidence:** gate run `000002-20261124T132542Z` of `worktree-wf_c37a519f-ba4-18` (head da01ec0f9c, slot 2 of 3; a merge of
origin/main 91976f1384 that renumbers PB5706's DialectBehavior to 70 and its VCR row to 7.126; it touches no file or
SORT path). The gate was RED with no value compared:

- `NistProgram_MatchesGolden` SQ214A (P4), SQ224A (P0), OBSQ5A (P1), SQ206A (P2) and OBSQ4A (P0): each timed out
  twice at 120000 ms, having used 2.0-2.6 s of processor time while the host was 60-63 % busy. The process was idle,
  not starved: the cause is a wait (a file or lock), not CPU load alone.
- `AnalyzerBuildsOnceDriftTests.OneReleaseBuild_BuildsTheAnalyzerOnce_AndASecondNotAtAll`: the Release build of the
  runtime timed out twice at 360000 ms after 11.3 s and 11.8 s of processor time (host 62 % busy).

Re-run alone at the same build, all five NIST programs passed (SQ214A 1/1 in 2 s; the other four 16/16 in 8 s).

**What this adds to the fix shape:** the processes in this run were blocked, not CPU-starved (2-3 % of one core over
120 s), and every NIST case affected is a sequential-file program (the SQ and OBSQ families, the family PB4387 measured
for sidecar-mutex cost). Fix shape (2) above, a load-derived timeout, would not catch a blocked process. The root cause
to find first is what an SQ program waits on under three concurrent gates (the sidecar lock directory of PB4387, or a
file the run directory shares), read from a dump of a hung process (`dotnet-dump collect -p <pid>` before the kill).
