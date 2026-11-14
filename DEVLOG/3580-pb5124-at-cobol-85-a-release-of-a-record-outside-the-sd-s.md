## Entry 3580 — 2026-11-13 19:31 PST — PB5124: at COBOL-85 a RELEASE of a record outside the SD's RECORD IS VARYING range is unsuccessful

**The defect.** At `--std 85`, a RELEASE whose DEPENDING ON item named a size outside integer-2..integer-3 of the SD's
RECORD IS VARYING clause still released the record, and the sort returned it. The note's repro, an SD of 3 to 8
characters, returned `RETURN 02 AA` and `RETURN 09 DDD` beside the two in-range records. §1985:VII.3.8.4 GR10 (the
Amendment 2 paragraph) says "the output statement is unsuccessful and, except during execution of a RELEASE statement,
the associated I-O status is set". kb/Work PB1036's range test in `CobolSort.ReleaseStatement` raised
EC-SORT-MERGE-RELEASE when checking was on and released the record when it was off. A 1985 program has no checking, so
its RELEASE always took the second path.

**Re-probe.** On this branch's Debug build of main `367e05ab5c` the repro printed the four RETURN lines and the read-4
repro `rel.cob` printed `RETURNED 04`. After the fix they print `RETURN 05 BBB` / `RETURN 08 CCC` (the drafted expected
output, line for line) and `RETURNED 01`. `--std 2023` prints what it printed before.

**The fix.** The binder now asks the edition once. `BoundRelease.RangeFailsStatement` is true below 2002, the
editions with no exception checking, where the rule states the outcome itself. `SortEmitter.EmitRelease` passes the
flag through `RuntimeApi.SortReleaseStatement` to `CobolSort.ReleaseStatement`. That method keeps its single range
test: after the raise, which does nothing with checking off, a set flag makes it return false and release nothing.
The false return also skips the SAME RECORD AREA store it gates. From COBOL-2002 on, the result with checking off
stays the implementor's, and the record is still released as before (docs/CONFORMANCE.md §3 D-SMA (c)). The new
determination is D-SORT-RELEASE-SIZE-85, and DESIGN.md's sort-merge paragraph says the same.

**Sibling sweep.** The implicit USING release has no 1985 runtime outcome to follow, because §1985:XI.4.4.3 SR3 is a
syntax rule. The other 1985 sentences that COBOL-2002 turned into EC-SORT-MERGE conditions state no outcome: an open
USING or GIVING file "must not be in the open mode", and an out-of-order MERGE input leaves the results "undefined".
Only data-name-1 can name an out-of-range size, because §1985:VII.3.8.3 SR2 keeps every record description inside the
range.

**Witnesses.** The golden `conformance:cases/VII.3.8@1985-RECORD-CLAUSE/pb5124_release_outside_record_range_85` covers
lengths 2, 9, 0 and 12 (the last by FROM). None is released, and each leaves data-name-1 and the record area
unchanged. Lengths 8 and 3 (by FROM) are released. The unit test
`SortMergeStatementEcTests.ReleaseStatement_RangeFailsStatement_ReleasesNothing_WhenUnchecked` covers the runtime arm.
Inventory row `1985-GR-VII.3.8.4-10` moves from DIVERGES to CONFORMS (batch `pb5124-release-range-85`), and the 1985
GAP drops from 249 to 248.

**The runtime ABI.** `CobolSort.ReleaseStatement` gains a parameter, so its public signature changes and code compiled
against the old runtime can no longer bind to it. That breaks binary compatibility, so the runtime major goes from 27
to 28 (`scripts/abi_pin.py bump --runtime-major`), as PB4772 did when it added the `slots` parameter. The documented
facts file is regenerated to list the new parameter. The first gate (`000001-20261114T030007Z`) was RED on exactly
those two pins, `RuntimeAbiPinDriftTests` and `DocumentedFactsDriftTests`; every other case ran green.

