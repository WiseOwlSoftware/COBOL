# Golden lane 1 (cloud) — LANDING

Branch `claude/golden-lane-1`, cut from origin/main 138a872. Lands via the owner's local orchestrator (push-main).
Rows in scope: 393 CONFORMS-with-empty-test-ref + 4 DOCUMENTED-NON-SUPPORT owing a witness = 397, split into 39 input
files (`scratch/in/`, built by `scratch/build_inputs.py` = the template builder with Linux paths and a hard MAX 12).

Pipeline: writer agent per input file (`scratch/WRITER.md`) derives expected output from the spec, then runs the built
compiler; one refuter per file (`scratch/REFUTER.md`) re-derives. Only refuter-upheld, passing goldens land
(`scratch/integrate.py`). `scratch/` is the raw record (drafts, held drafts, reports) — NOT a test location.

Baseline gate on 138a872 (this VM): Conformance filter Corpus|Negative|Intrinsic|VersionMatrix Passed 5446/5446;
Unit filter SpecTraceabilityInventory|DefectiveRowCoverage|Manifest|AnnexA1Register Passed 25/25.

Owner budget notice (2026-09-25): waves 1 and 2 only; no wave 3.

## Status
- Snapshot pushed: wave 1 (misc-p1..p6) writers done, refuters partially done; wave 2 (misc-p7..p12) writers running.

## Wave 1 (misc-p1..p6, 67 rows) — landed

GAP 1627 → 1580 (+47 rows closed; witness-only records, `scratch/` batches). Goldens: 85 +17, 2002 +22, 2014 +3, 2023 +2, negative +21.

### misc-p1: landed 8, overturned 2, suspected defect 0, not-closable 0
- overturned SR-13.18.2.3-4 (convention): COBOLNET1542 is SHARED by the SR2, SR3 and SR4 placement checks (DataBinder.AnyLengthValidateUnit), so the bare-code .err files do not assert THIS rule's violation. Concretely, for the -unreferenced leg a regression that
- overturned SR-8.8.4.12.3-2 (does-not-exercise-rule): `IF A = B AND NOT NOT C` is rejected by the §8.8.4.12.2 GENERAL FORMAT before any implied insertion happens. After the logical operator, the format admits exactly ONE slot {NOT | simple-relational-operator | extended-rel
### misc-p2: landed 6, overturned 2, suspected defect 0, not-closable 4
- overturned DOC-A.1-16 (does-not-exercise-rule): The documented determination (CONFORMANCE.md DOC-A.1-16) is specifically 'not located -> EC-PROGRAM-NOT-FOUND (GR3b)'. The golden observes only that ON EXCEPTION is taken, but §14.9.4.4 GR3h 1) sends EVERY EC-PROGRAM con
- overturned SR-11.3.3-4 (convention): COBOLNET0820 is a SHARED OO structural code (duplicate class, END CLASS/METHOD mismatch, INHERITS cycle), so the .err 'COBOLNET0820' is satisfied by a regression that rejects this group for a different 0820 reason; the .
- not-closable GR-13.18.10.4-3: Nothing observable. The CHARACTERS arm is at least compiled/run by l1c02_block_contains_forms (FB, FC), but that pins the format, not the unit.
- not-closable GR-13.18.10.4-4: Nothing to DISPLAY. The TO arm compiles in l1c02_block_contains_forms (FC, FD2).
- not-closable GR-7.3.9.3-3: Implementor latitude with nothing to DISPLAY.
- not-closable GR-8.1.3.2-5: No observable; documenting 'substitute graphics: none' belongs to DOC-A.1-25.
### misc-p3: landed 9, overturned 3, suspected defect 0, not-closable 0
- overturned DOC-A.1-26 (editions): Output (UTF-16-LE / UTF-16-BE / UTF-32-BE / UTF-8) re-derived from the decoded .cpy bytes and the CONFORMANCE.md DOC-A.1-26 choice and is correct; citations OK (§8.1.3.2 2), §A.1 26)). But the program uses the COPY liter
- overturned SR-12.4.5.7.3-5 (does-not-exercise-rule): PRIME-CODE is defined NOWHERE in the program, so the source is rejected as an undefined reference whichever Format-2 reading is taken; it breaks SR4 (data-name-1 reading) just as much as SR5. An implementation that never
- overturned SR-12.4.5.7.3-6 (does-not-exercise-rule): KF-KEY is not subject to any OCCURS clause, so KF-KEY(1) is already illegal by §8.4.2.3.3 3) ('the number of subscripts shall equal the number of OCCURS clauses', cite.py OK) regardless of SR6, so SR6 is not the only rea
### misc-p4: landed 8, overturned 2, suspected defect 0, not-closable 2
- overturned GR-13.18.16.4-6 (does-not-exercise-rule): The program is illegal on its own under §13.18.16.3 SR5 (checked: 'The entry specified by data-name-1 shall not have an occurs-depending table subordinate to it' -> OK §13.18.16.3 5)). CT OCCURS 0 TO 5 DEPENDING ON NN is
- overturned GR-7.2.3.4-11 (editions): The 85 golden writes 'COPY l1c04_copy_spacing'. Its text-name is a user-defined word that contains UNDERSCORES. The underscore is a COBOL-2002 word character: constructs.json user-word-underscore-2002 (§8.3.2.1) says the
- not-closable GR-7.2.3.4-4: A golden showing COPY ... SUPPRESS still copies the text would pin GR6, not GR4. The adjudicator's separate finding still stands and was not re-probed here: a REPLACING phrase after SUPPRESS is silent
- not-closable GR-7.2.3.4-5: Pure listing semantics. Nothing can be DISPLAYed.
### misc-p5: landed 9, overturned 2, suspected defect 0, not-closable 1
- overturned DOC-A.1-29 (editions): The mode is implementor-defined only from 2023. Annex E.2 item 6 says: 'The mode of arithmetic used in evaluating compile-time arithmetic expressions and the handling of intermediate results is now explicitly implementor
- overturned SR-7.3.3-9 (does-not-exercise-rule): SR9 only reserves the word IMP for the implementor and makes an IMP directive's syntax implementor-defined. Nothing in it requires '>>IMP SOMETHING' to be rejected; an implementation that defines IMP conforms and would a
- not-closable GR-7.3.4-4: Vacuous for this implementation; Annex A.1 item 30 is owed only upon support for IMP.
### misc-p6: landed 7, overturned 1, suspected defect 1, not-closable 2
- overturned GR-11.9.7.4-4 (does-not-exercise-rule): Drop conformance:2014/l1c06_entry_convention_oo from GR-11.9.7.4-4 and leave the row a GAP (test-needed, vacuous while COBOL is the only convention, PB298). GR4 a) and b) say a no-clause class or interface INHERITS its c
- not-closable GR-7.3.12.4-4: No observable: COBOL.NET produces no source listing, and per GR2 its documented disposition (CONFORMANCE.md §7.3 recognized-and-ignored set) consumes >>DISPLAY with no transfer at all (probe: '>>DISPL
- not-closable GR-7.3.12.4-6: No observable: COBOL.NET produces no source listing, and per GR2 its documented disposition (CONFORMANCE.md §7.3 recognized-and-ignored set) consumes >>DISPLAY with no transfer at all (probe: '>>DISPL
- suspected defect SR-8.4.3.11.3-5 → kb/Work/PB1547

Overturned rows stay CONFORMS-but-untested; the refuter's exact correction is in `scratch/reports/<slug>.refute.json` (a follow-up writer can apply it). Not-closable rows stay GAP and need a registrar decision (derivation / DNS), not a golden.
