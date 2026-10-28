## Entry 2698 — 2026-10-28 11:47 PDT — File PB3996 and PB3997: fuzz night 7's two accepts-illegal-source defects

Fuzz night 7 (`fuzz-20261027-215606`, seed 20261028, judge = main 7f0105fec) produced 6,457 programs and 1,798 survivors in
330 signatures. Thirteen read-only adjudicator slices (`fuzz_triage.py prepare`, Workflow `fuzz-triage`) decided all of
them: 103 signatures WISEOWL-CORRECT, 70 GNUCOBOL-SIDE, 64 LATITUDE, 11 UNINITIALIZED, 9 NOT-COMPARABLE, 11 MODEL-ERROR
and 2 WISEOWL-DEFECT. `cites` checked every citation, and `verify` re-ran both defect repros on the judge.

Both defects let illegal source through:

- **PB3996:** a Format 1 CALL accepts OMITTED. §14.9.4.2 prints OMITTED only in Format 2, and PB130's Format-1 narrowing
  never reached the two OMITTED arms of `CallBinder`.
- **PB3997:** a VALUE literal outside a binary usage's range compiles (BINARY-CHAR VALUE 255, -129, and the like), where
  §13.18.63.3 2) requires a value within the range the USAGE clause indicates. `ValidateNumericValue` checks only the
  picture-digit span, and the stored value then wraps.

The owner ruled (2026-10-28): "Letting illegal source code thru is a fatal defect. Bug and must be fixed." So both are
filed `status: open` for the fix lane, not `status: owner`. The GnuCOBOL-side records go through the independent refuters
before `fuzz_triage.py file` writes any `upstream` note; that filing lands separately.

