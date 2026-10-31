## Entry 2880 — 2026-10-31 14:06 PDT — PB4196, PB4113, PB4199: a nearer report entry hides a container's GLOBAL name, a COMP-5 VALUE takes its range from the container, a contained FILE STATUS names a GLOBAL item

Three notes of the data binder (wave 20261785, group A), fixed at their roots in harm order, on top of the PB3557 branch
(`worktree-wf_34371f8b-c21-12`, merged first with `merge_drivers.py merge --keep`; it had already landed on main through PR #891).

**PB4196 (silent wrong answer).** A contained program's report entry `PV` did not hide its container's single GLOBAL data item
`PV`: `MOVE PV TO W` and `FILE STATUS IS PV` in the contained program bound the container's item, where §8.4.6.2.1 3) a) ("If the name
is declared in source element B, the item in source element B is the referenced item") makes the report entry the referenced item
and §13.8.6.2.3 refuses the reference (COBOL5092). Re-probe: the note's repro compiled (COBOL2455 only because its `PV` was numeric);
`MOVE PV TO W` alone and a `PIC XX` file status compiled clean. Root: `QualifiedCandidates` and `TryResolveUnqualified` narrowed a
survivor set to the nearest declaring element over data items alone, and `ReportEntryReferenceRefused` was consulted only when no
unique data item survived (PB3767 had added the same rule for the several-items case, as a special order inside the refusal).
Fix: `SymbolTable.NearestDeclaringNamed` is the one narrowing for data-names and condition-names; it leaves no survivor when
`DataBinder.NearerReportEntryHides` finds a report entry the written qualifiers reach in a nearer source element, so every caller
meets an unidentified reference and its existing report-item refusal names the entry. The PB3767 special order is deleted
(`DataItemsAmbiguousBeforeReportEntry`; several surviving items are simply the ambiguity). Siblings found and fixed in the same
mechanism: condition-names (`IF CN`, with `ConditionBinder.ReportMisqualifiedCondition` asking the refusal), index-names and
capacity registers (`SymbolTable.NearerDataNameHides`), constants (`DataBinder.DeclaredNearerThan`). Not changed: the sum counter's
own earlier path; a qualifier that reaches only the container's item (`PV OF G1`) and a nearer data item against a farther GLOBAL
report entry keep their answers. A drift test (`GlobalNameClassDriftTests.ADataNameNarrowing_RanksTheReportEntriesToo`) holds every
narrowing to the one method and the other-class predicates to the entries. Goldens: `13.8.6.2.3-Report-Items/pb4196-*` (six
refusals: statement, FILE STATUS, condition-name, index-name, and (branch review R1-1) a GLOBAL constant and a GLOBAL report sum counter, each against a nearer report entry; one run-pass: a nearer data item against a GLOBAL
report entry) and `8.4.6.2-Local-and-global-names/pb4196-contained-qualified-reference-...`. Design: REPORT_WRITER_DESIGN.md.

**PB4113 (rejected legal source).** `PIC 9(4) COMP-5 VALUE 65535` (and `S9(4)` -32768, `9(2)` 255, `S99V99` -327.68, the same
on a level-88 and a table value) drew COBOL1625 because `ValidateNumericValue` asked the PICTURE's digit span on the high side
before its USAGE-band test, which was unreachable for a leading-side excess. Fix: for a `NumericTruncation.BinaryCapacity` subject
the leading side is `AlgebraicRanges.Of`'s container band (the same statement the store bound and HIGHEST-ALGEBRAIC read), and the
PICTURE test keeps the trailing side (`S99V99 COMP-5 VALUE 1.234` stays refused). The `<remarks>` claim that the digit test never
false-rejects is corrected; DOC-A.1-208 carries the VALUE determination (GnuCOBOL 3.2.0 and IBM agree). Goldens
`wiseowl/cases/13.18.63-VALUE/pb4113_*` (run, 2002+; 85 limits a literal to 18 digits) and two refusals. Row SR-13.18.63.3-2 stays
CONFORMS, restated, in `closes_rows`.

**PB4199.** Re-probed: no longer reproduces. The PB3557 branch's `342645815` (`InheritContainerGlobalData`, PB3766's data-item
sibling) already inherits the container's GLOBAL data before `ResolveFiles` runs, so FILE STATUS, RELATIVE KEY, ASSIGN USING,
RECORD DEPENDING ON and LINAGE naming container GLOBAL items compile and run. This landing adds the sibling golden
`13.18.27-GLOBAL/pb4199-contained-file-clauses-name-global-data-items` (`GS=35`) and marks the note landed.

**Self-review.** Architecture (one narrowing, the refusal stays the callers'), full code (message and rule agree; sibling arms
swept), performance (the report-entry test is free for a unit that inherits no global data or has no report, and depth-0 items
return before any entry scan), duplication (the PB3767 special order removed, not kept beside the new rule).

