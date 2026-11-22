## Entry 3924 — 2026-11-22 13:40 PST — The landing refuses a change that moves the code a COMPLETE edition's code reads name (PB6056)

Twice on 2026-11-22 a landing turned an edition that read COMPLETE NOT COMPLETE with every check green: PB2635 deleted
`SelectMin`/`SelectMax` (five 1985 rows) and PR #1946 moved `OccScaled` and `RefModPosition` (two rows). The gate's CODE READS audit calls
a read whose code moved "information, never red" (a read is evidence at its pin, PB5863), while `edition_status.py` counts the same row in
its CODE READS part.

Re-probe: at 9bfe9e1c44 `code_reads.py --check 1985` printed "2 read(s) owed a re-read (information, never red)" and GREEN, and
`edition_status.py --edition 1985` NOT COMPLETE, 2 code reads: the mechanism reproduced.

Fix at the one place that knows both trees. `code_reads.moved_by(ctx, records, head, base)` is the predicate (an entry of a decided
read's `code-location` that resolves in the base and not in the head). `scripts/spec/code_read_landing.py` asks it as a landing check,
run by `enqueue.py checks` from the merged head's own tree (exit 6), and asks main's status (main's own `edition_status.py --json` in a
detached worktree of origin/main) only when a read's code moved, so the common landing pays nothing. It refuses an edition that reads
COMPLETE on main, naming the rows and the remedy (re-read on the branch, `import --replace`); a branch that carries the re-read passes,
an edition not COMPLETE on main is not refused, and an entry already unresolved on main stays `moved_since`'s. The gate's audit stays
information: it holds one tree and cannot know what main reads.

Verified read-only on the two unlanded PB1754 branches that move pinned members: `worktree-wf_ef90b3a1-3e3-1` (3 rows of 1985) and
`-3e3-5` (1 row), each refused with main's status stubbed COMPLETE; with main's real status (1985 NOT COMPLETE: open notes) the rule
correctly does not apply.

Sweep of `edition_status.py`'s other terms: the inventory GAP, the open notes and the prose holes change by a verdict, a filed note or an
edited transcription, each in the landing's own diff; stale, withdrawn and owed reads change by a catalog or store edit. Moved code is the
one term an unrelated landing turns, so it is the one refused (recorded in kb/Work/PB6056.md).

Self-tests: `case_19` to `case_21` of `scripts/selftests/spec/code_reads/test_code_reads.py` (the predicate; refused; carried re-read,
open edition, no move, a revision that is no commit), and enqueue's own self-test (six checks in order, exit 6, the worktree's own script).
Docs: MANDATORY-PRACTICES L1, DESIGN-test-build-ci, DESIGN-spec-conformance-review 10.6.1, DOC_INDEX, enqueue.py's docstring.

