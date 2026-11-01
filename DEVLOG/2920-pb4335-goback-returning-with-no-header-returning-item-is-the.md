## Entry 2920 — 2026-11-01 00:23 PDT — PB4335 — GOBACK RETURNING with no header RETURNING item is the main program's exit status, and a status phrase survives RAISING

**Mechanism 1 (PB4335).** `GOBACK RETURNING x` in a program whose PROCEDURE DIVISION header names no RETURNING item
compiled clean and died at run time with `NotImplementedCobolFeatureException`: PB880 had made the implicit move a bound
node for the receiver case only and left the emitter a loud arm for the rest. Re-probe on the build reproduced it (the
note's literal `GOBACK RETURNING 4` is refused by the grammar, which takes a data reference; a `PIC 9 VALUE 4` item
reproduces). ISO 14.9.18.4 GR2 conditions the activation result on the HEADER's RETURNING phrase, so with none there is
nothing to move into; the extension's vendor meaning (GnuCOBOL moves the operand to RETURN-CODE) is the main program's exit
status, which GR3 + GR10 already route for a status phrase. `BindGoback` now folds such a source into the GOBACK's
termination status (`ControlFlowBinder.ReturningAsStatus`, the same SR6 screen and DOC-A.1-192 rendering), the emitter's
loud arm is deleted, and a called program ignores it (its status is inert, GR2). A written status phrase wins.

**Sibling found in the same function (I9).** `GOBACK RAISING EXCEPTION x WITH ERROR STATUS 3` built its `BoundGoback`
without `p.Status` (the code said the two phrases were mutually exclusive, but 14.9.18.2's choice indicators allow both),
so a main program exited 0 where GR3 ignores RAISING and GR10 passes the status. Fixed, with tests.

**Goldens.** `14.9.18-GOBACK/pb4335_goback_returning_no_header_item` (accept 2002; main plus a called program) and
`pb4335-goback-returning-below-2002` (refused at 85, COBOL0900); `StopGobackExitCodeTests` asserts the exit codes
(extension operand, written phrase wins, RAISING beside the operand, RAISING beside STATUS, a called program's inert use).
Docs: manual migrating page, CONFORMANCE.md extension row, INTERPROGRAM_DESIGN, the extension register text.
No inventory row is claimed (`closes_rows: []`).

