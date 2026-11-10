## Entry 3417 — 2026-11-10 00:21 PST — PB5083: the USE FOR DEBUGGING procedure triggers run as X3.23-1985 general rules 3 to 22 say

The 1985 `--std=85` triage found six rules of USE FOR DEBUGGING (§1985:XV.3.2.4) giving wrong results. Each was re-probed on this build
before the change (`cobol.exe --std 85 --run`, the triage's own programs) and all six reproduced: a section subject printed DEBUG-NAME
`MAIN-PARA` and, entered at P2, ran the section; the ALTER printed no trigger; a PERFORM printed spaces for its first execution; a USE
procedure printed `ERR-PARA` with `START PROGRAM` then `FALL THROUGH`; a SORT input or output procedure subject was refused COBOL1571.

**The root.** A procedure-name subject was resolved to a pc range and every pc of the range became a trigger, named by the paragraph.
That is right for a paragraph and wrong for a section (GR3 a, GR9, GR13, GR16). The model is now one procedure-name, one trigger point:
`BoundDebugSubject` carries `IsSection`, a section subject sits at the section's first pc under the section's name
(`ResolvedProcedure.IsSection` tells a one-paragraph section from its paragraph, which cover the same pcs), and several subjects share a
pc, so `DispatchEmitter` emits one `__RunDebug` per subject, section first. ALL PROCEDURES became what GR4 says, every procedure-name
outside a debugging section: sections and paragraphs, and the declarative sections that are not debugging sections (CCVS DB105A's name
list has the section and its first paragraph as two entries). `__RunDebug` now restores the cause and line after the section runs, as the
section's own PERFORMs overwrite them and the next subject at the pc reads them.

**The causes.** GR20: an out-of-line PERFORM sets `PerformLoop` on every execution of the range, the first included (the per-PERFORM
first-iteration flag is gone; DB101A's SIMPLE-PERFORM-TEST expects it). GR19: `SortEmitter` sets `SortInput`, `SortOutput` and
`MergeOutput` where it enters the procedure, with the statement's line; the COBOL1571 refusal and its `SortMergeProcedureRanges` walk are
deleted. GR21: `__RunUse` sets `UseProcedure` for a declarative id, and the statement line comes from a new `BoundStatement.DebugLine`
the funnel stamps only in a program that debugs a USE procedure. GR17: `AlterSwitchEmitter` runs the section of each altered
paragraph's subject after the ALTER through `__RunDebugRef`, DEBUG-CONTENTS the TO phrase's name as written, DEBUG-CONTENTS widened to
the longest such name (GR15). Found on the way and fixed (GR13): a procedure subject written `P OF S` is named with its qualifier.

**Goldens.** `pb5083-debug-section-subject` (GR3 a, GR9, GR13, GR16, GR20, GR22), `-alter`, `-sort-merge`, `-perform-loop`,
`-use-procedure`, each accepted at 85 with values derived from the rules, and the refusal `pb5083-debug-section-subject-removed-2002`
(COBOL0902 at 2002 and later). Four existing tests changed their expected output: the ALL PROCEDURES ones now see the section as a
trigger and PERFORM LOOP on the first iteration; the COBOL1571 SORT test became a run test of the SORT causes. `kb/Work/PB5083.md` is
landed with `closes_rows: []` (the inventory has no 1985 rows). `docs/CONFORMANCE.md` D-DEBUG-PROCEDURE and `docs/CONTROL_FLOW_DESIGN.md`
D20 (e) state the determinations.

**Latitude.** Where a section and its first paragraph are both subjects the text gives the paragraph no cause of its own; it shares the
transfer's, START PROGRAM included (recorded as a determination).

