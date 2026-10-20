## Entry 2306 — 2026-10-20 15:30 PDT — PB3461: PB3455's closes_rows_reason names the notes that close the eleven 9.1.16-9.1.18 rows

PB3455 (landed in PR #325) gave the eleven GAP rows of 9.1.16 to 9.1.18 to the tier-2 chain, but its own
`closes_rows_reason` said the rows close "through PB3337, PB3338 and PB3340" while PB3337's note, changed in the same
landing, claims and closes none of them. The branch review of PB3455 (R1-1) filed the contradiction as PB3461.

Re-probe: on the tree cut from 80ca952ce the contradiction was still there. PB3338 claims SR-9.1.18.6-p1, GR-9.1.18.7-p1
and GR-9.1.18.7-p2; PB3340 claims the other eight; PB3337 claims none. Fix: PB3455's reason now names PB3338 and PB3340
and says PB3337 closes none, agreeing with PB3337's own reason. The sibling sweep found one more stale statement of the
same assignment, `kb/Work/PB284.md` line 187 ("PB3337 and PB3338 expect to close" the rows); it now names PB3338 and
PB3340. PB3338, PB3340 and plan section 0 already agreed.

No source, test or inventory row changed; `closes_rows` is empty with its reason. `python scripts/spec/work.py check`:
2754 work items, all well-formed. Gate: see the report for the verdict line.

Landing finisher: the branch's first landing stopped with the Linux gate RED on one self-test arm,
`conformance-suite/harness/run.py --self-test`: "a kept working directory holds the source under the name limit and the
declared files". The arm found its `--keep` directory by diffing `conformance-kept-*` in the machine-wide temp directory
before and after its run, then removed every new match. Two gates' Linux legs ran the self-tests at the same moment
(wf_6fa84a9d-fb8-3 and -4 finished theirs 0.3 s apart, sharing WSL's /tmp), so the other run took this run's kept
directory for its own and removed it. The defect is on main too; it needs only a second run on the host. Reproduced on
Windows: of four concurrent runs of the old self-test, three FAILED on that arm. Fix: the arm's run now has a temp root
the self-test owns (`tempfile.tempdir` set to a directory inside the self-test's own temporary tree, then restored) and
expects exactly one kept directory there; three concurrent runs of the new self-test all pass. A sweep of the tracked
scripts and C# for another enumeration of the shared temp directory found none. `scripts/temp_names.py` (PB2922's
checker) catches a fixed name joined to the temp root but not a glob over it, which is how this site escaped; that gap
is left to a note of its own.

