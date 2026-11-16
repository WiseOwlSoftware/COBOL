## Entry 3736 — 2026-11-16 00:27 PST — A refused SORT or MERGE no longer reports a cascade COBOL7081 on its procedure's RELEASE or RETURN

PB5496 (from the 1985 code read 4 refuter's side note). A MERGE refused with COBOL6815 (two USING files on one multiple file reel) with an
OUTPUT PROCEDURE also reported COBOL7081 on the legal RETURN in that procedure. Re-probe (`--std 85`, the refuter's `w3.cob`) reproduced it.
Cause: a statement refused after its SD resolved binds to `BoundRejected`, so the ranges of its INPUT/OUTPUT PROCEDURE phrases never reach
`GateReleaseReturnRange` (§1985:XI.4.2.3 SR2, §1985:XI.4.3.3 SR3). The same held for a refused SORT's RELEASE. Fix: `BoundRejected` records the
sort-merge file (`SortBinder.MarkRefusedSortMerge`, one place for both verbs) and the gate leaves a RELEASE/RETURN of that file undecided; a
RETURN of another file is still judged. Tests: `RefusedSortMergeReleaseReturnTests` (3). The note closes no inventory row (`closes_rows: []`).
Self-review: no second copy of the rule, no hot-path allocation (one set per program with a use), the refused statement keeps its single error.

