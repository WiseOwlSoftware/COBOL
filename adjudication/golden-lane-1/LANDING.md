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

### Wave 1 gate (this VM, after integration)
- Conformance `Corpus|Negative|Intrinsic|VersionMatrix`: Passed 5511/5511 (base 5446); all 65 new goldens named in the trx, 0 failed outcomes.
- Unit `SpecTraceabilityInventory|DefectiveRowCoverage|Manifest|AnnexA1Register`: first run 1 red —
  `AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names` (the new DOC-A.1-6/8/21/32/43/44 witnesses
  were not yet in docs/CONFORMANCE.md §7 `Pinned by`); fixed by naming them there → Passed 25/25.

## Wave 2 (misc-p7..p12) — landing incrementally

### misc-p7: landed 10, overturned 1 (GAP 1580 → 1570)
- overturned SR-7.3.13.3-8 (expected-value): Program is not conforming source, so no stdout is defined for it. The omitted outer >>WHEN OTHER text-2 holds `>>EVALUATE 9` / `>>WHEN OTHER` / `>>END-EVALUATE` with NO `>>WHEN operand-2` phrase; §7.3.13.2 Format 1 encloses `>> WHEN ... [text-1]` in braces with an ellipsis (required, one or more tim
- gate: Conformance filter 5518/5518 (7 new goldens ran by name), Unit filter 25/25.

### misc-p8: landed 6, overturned 2, not-closable 1 (GAP 1570 → 1564)
- overturned GR-8.4.3.6.4-1 (not-spec-derived): The program is not conforming source, so under the spec there is no expected stdout. `INVOKE L1C08EOC "NEW" RETURNING E` violates §14.9.23.3 SR3 ("The value of literal-1 shall be the name of a method defined in the factory interface of object-class-name-1"). `
- overturned GR-8.4.3.6.4-2 (not-spec-derived): Same program as GR1 and the same defect: `INVOKE L1C08EOC "NEW"` on a class with no INHERITS FROM BASE breaks §14.9.23.3 SR3, so the source is illegal and no stdout can be derived from the spec. Once the class is `INHERITS FROM BASE` and the compiler supports 
  Lead for the registrar: the refuter reports the compiler refuses `INHERITS FROM BASE` (COBOLNET0821, BASE not provided) and accepts `INVOKE <class> "NEW"` on a class with no INHERITS (also in existing goldens `l1_exit_raising_object_sending`, `oo_ec_raise_object`) — not verified or filed here.
- not-closable DOC-A.1-65: Needs a non-COBOL activating element (a .NET host calling ProgramRegistry.CallProgram). The conformance runner only compiles and runs COBOL sources, so it cannot drive this. A unit/integration test wi
- gate: Conformance filter 5526/5526, Unit filter 25/25.
- misc-p12 suspected defect GR-13.18.27.4-3 = existing kb/Work/PB1523 (GLOBAL REDEFINES backend crash); repro appended there, no new note.

### misc-p9 / misc-p10 / misc-p12: landed 25 rows (GAP 1564 → 1538)
- misc-p9: landed 8, overturned 3, suspected defect 0, not-closable 1
  - overturned GR-14.6.3-3 (editions): (1) Subprogram L1C09V has a PROCEDURE DIVISION with no paragraph at all. That is not legal 1985 source: the ANSI X3.23-1985 procedure division format requires at least one section or paragraph ({paragraph-name. [sentence
  - overturned GR-14.6.4-3 (convention): The program is not conforming source. 'INVOKE L1C09C "NEW" RETURNING S' breaks §14.9.23.3 SR3: 'The value of literal-1 shall be the name of a method defined in the factory interface of object-class-name-1'. L1C09C has no
  - overturned GR-14.6.4-5 (convention): Same program as GR-14.6.4-3, with the same defect: 'INVOKE L1C09C "NEW"' on a class with no INHERITS FROM BASE breaks §14.9.23.3 SR3, because NEW belongs to the factory interface of BASE only (§16.2.1). Fix: INHERITS FRO
  - not-closable GR-14.6.10-1: Undefined-result rule, the same treatment as DRV-GR-14.9.20.4-9 in CONFORMANCE.md §8. CONFORMS-but-untested is the correct end state.
- misc-p10: landed 8, overturned 1, suspected defect 0, not-closable 3
  - overturned DOC-A.1-69 (does-not-exercise-rule): The expected stdout is correct, but it does not pin the documented determination. Lines 1-5 (EC-BOUND-SUBSCRIPT ... EC-ORDER-NOT-SUPPORTED) run with checking ENABLED. §14.6.13.1.3 5)/7) and the unnumbered paragraph after
  - not-closable GR-7.3.14.4-2: Not closable by a conformance golden: the rule's only observable is a compile-time WARNING (COBOLNET1620). CorpusRunnerTests positive goldens assert compile suc
  - not-closable GR-7.3.14.4-3: Not closable by a conformance golden: the rule's only observable is a compile-time WARNING (COBOLNET1620). CorpusRunnerTests positive goldens assert compile suc
  - not-closable GR-7.3.14.4-5: Not closable by a conformance golden: the rule's only observable is a compile-time WARNING (COBOLNET1620). CorpusRunnerTests positive goldens assert compile suc
- misc-p12: landed 10, overturned 1, suspected defect 1, not-closable 0
  - overturned SR-14.9.16.3-2 (convention): The .err pins COBOLNET0899, which is the 'recognized but not implemented' code (DiagnosticCatalog: ReportGenerateNeedsControl is declared under NotImplemented). That code means a compiler limitation, not an SR2 violation
- SR-11.4.3-2: the refuter overturned `negative/l1c09-factory-implements-missing-method` (a second reason to reject: INVOKE NEW without INHERITS FROM BASE) and upheld `-nonconforming-method`; the row's witness is only the upheld negative and the overturned one was removed from the tree (integrate.py keyed verdicts by rule-id; fixed by hand).
- misc-p12 GR-13.18.27.4-3 → existing kb/Work/PB1523 (see above).

## Totals at stop (owner STOP, budget)
- Rows witnessed: 47 (wave 1) + 10 (p7) + 6 (p8) + 25 (p9/p10/p12) = **88**. GAP 1627 → **1538**.
- Defects: **kb/Work/PB1547** filed (ADDRESS OF receiving operand → COBOLNET0901, not SR5); **PB1523** extended with a new repro. Unfiled leads for the registrar: INVOKE "NEW" accepted on a class without INHERITS FROM BASE and `INHERITS FROM BASE` refused (COBOLNET0821) — misc-p8/p9 refuters; national STRING receiver crashes at run time with NotImplementedCobolFeatureException (`StringEmitter.cs:216`, repro `scratch/held/misc-p11/side/`) — misc-p11 writer; re-INITIATE after TERMINATE joins lines (`scratch/held/misc-p12/incidental/`) — unverified; the Pinned-by gap noted by writers for existing 2002/directive_expressions (directives in column 7).

## Drafted but NOT integrated (finish from the pushed snapshot `scratch/`)
- **misc-p11** (12 rows, writer reports all 13 positives + 2 negatives passing): `scratch/reports/misc-p11.json`, drafts `scratch/out/misc-p11/`. Its refuter was stopped by the owner STOP — it needs a refuter, then `scratch/integrate.py wave3 misc-p11`.
- **Overturned rows from waves 1–2** (about 23 rows): each refuter correction is exact in `scratch/reports/<slug>.refute.json`; most are mechanical (a more specific `.err`, a different edition directory, `INHERITS FROM BASE`). Apply the correction, re-run, re-refute.
- **Not started:** misc-p13 … misc-p38 and dns-witness (inputs in `scratch/in/`, 250+ rows).

Process: after a refuter, run `python3 scratch/integrate.py <wave> <slug…>` (paths are /tmp/gl; adjust) → strip CR → `record_verdicts.py --dry-run` then apply → gate (`scratch/gate.sh`) → name any new DOC-A.1 witness in CONFORMANCE.md §7 `Pinned by` (AnnexA1RegisterDriftTests). ⚠ integrate.py keys refuter verdicts by rule-id; a row with several goldens and split verdicts must be checked by hand.
- gate (p9/p10/p12): Conformance filter 5548/5548 with all 21 new goldens run by name; Unit filter 25/25 after naming DOC-A.1-72/74/95/97 witnesses in CONFORMANCE.md §7 Pinned by.

## Wave 3 (owner-authorized, small)

### misc-p11: landed 10, overturned 2 (GAP 1538 → 1528)
- overturned SR-13.4.5.3-2 (convention): The expected output (OPEN=00 LC=0001 / MAIN-W=00 LC=0002 / SUB-W=00 LC=0003 / AFTER-SUB LC=0003) is correct, and the clause numbers 13.18.34.4 7) d) and 7) c) 3. are the PRINTED ones. But header lines 16 and 20 record th
- overturned GR-13.4.5.4-2 (does-not-exercise-rule): GR2 only constrains PROGRAMS: every FD of one external connector 'shall obey' a)-e). The writer concedes it has no mandated detection (§4.2.2). Everything the golden prints comes from §9.1.5 1) (one external connector: D
- gate: Conformance filter 5560/5560 (all 13 new goldens ran by name), Unit filter 25/25 after naming DOC-A.1-76/78 in CONFORMANCE.md §7 Pinned by.

### misc-p13 + misc-p14: landed 8 (GAP 1528 → 1520)
- misc-p13: landed 5, overturned/withheld 1, not-closable 1
  - GR-14.9.21.4-5 (editions): The only construct that forces the 2023 directory is ORGANIZATION IS LINE SEQUENTIAL on F-BACK. Line sequential organization is new in 2023 (the ISO 2023 new-features list at specs/ISO_COBOL.md:1105 h
- misc-p14: landed 3, overturned/withheld 8, not-closable 1
  - GR-14.9.22.4-1 (does-not-exercise-rule): CNT, the DEPENDING ON object, is OUTSIDE group G, so §13.18.38.4 GR8a governs G's length. GR8a gives the length from the current CNT whether G is a sending or a receiving operand. Only GR8b, with the 
  - GR-14.9.22.4-L2.1 (split): all refs overturned
  - GR-14.9.22.4-L3.2 (split): all refs overturned
  - GR-14.9.22.4-5 (does-not-exercise-rule): GR5 maps literal-1..5 onto identifier-3..7. The golden pins identifier-3 (I1), identifier-4 (I3) and identifier-5 (I2). It never uses identifier-6 or identifier-7, the CONVERTING operands that GR20 re
  - GR-14.9.22.4-7 (split): all refs overturned
  - GR-14.9.22.4-9 (does-not-exercise-rule): GR9a has two arms: 'neither BEFORE nor AFTER' and 'identifier-4 references a zero-length item'. The writer's own notes say the zero-length identifier-4 arm (2014+) is not exercised, so the 2014 and 20
  - GR-14.9.22.4-11 (split): all refs overturned
  - GR-14.9.22.4-16 (does-not-exercise-rule): GR16 makes ALL, FIRST and LEADING transitive. R1-R4 pin FIRST and ALL, and the ALL/FIRST ending of each other's reach, but no operand follows a LEADING. An implementation where a bare operand after LE
- ⚠ Conservative withholding: when the refuter overturned `85/l1c14_inspect_comparison_cycle` for NOT pinning GR5/GR16 (does-not-exercise-rule), integrate.py withheld that golden from every row, including the rows the refuter UPHELD on it (L2.1, L3.2, GR7, GR11 — the 'split' entries above). Those four rows can land from the snapshot with no rewrite: land the golden as it stands for the upheld rows only. `85/l1c14_inspect_before_after_regions` was withheld the same way.
- gate: Conformance filter 5567/5567 (all 7 new goldens ran by name), Unit filter 25/25.

## Final state at owner stop (wave 3 done)
- **Rows witnessed: 106**: wave 1 = 47, wave 2 = 41, wave 3 = 18. **GAP 1627 → 1520.**
- **Defects:** kb/Work/PB1547 filed; kb/Work/PB1523 extended. The unverified leads are listed under "Totals at stop" above.
- **Input files NOT started (25 files, 241 rows):** dns-witness, misc-p15, misc-p16, misc-p17, misc-p18, misc-p19, misc-p20, misc-p21, misc-p22, misc-p23, misc-p24, misc-p25, misc-p26, misc-p27, misc-p28, misc-p29, misc-p30, misc-p31, misc-p32, misc-p33, misc-p34, misc-p35, misc-p36, misc-p37, misc-p38. Inputs are in `scratch/in/`.
- **Drafted but not landed:** every overturned or withheld row in waves 1–3. Its correction is in `scratch/reports/<slug>.refute.json` and its draft is in `scratch/out/<slug>/`.

## Wave 4 (micro; misc-p15): landed 5 (GAP 1520 → 1515)
- Landed: GR-14.9.22.4-23 (INSPECT CONVERTING duplicate, first occurrence wins), SR-8.4.3.1.3-11 (LINAGE-COUNTER qualified),
  GR-14.6.2.3.2-2 (INITIAL resets nested programs), SR-11.6.3-2 and SR-11.6.3-3 (interface INHERITS negatives).
- Not-closable: GR-14.9.22.4-18, GR-14.9.22.4-21 (the rule only makes the result undefined).
- Overturned: 5 OO rows (GR-11.6.4-2, GR-14.9.23.4-3, SR-14.9.23.3-20, SR-14.9.23.3-22, DOC-A.1-101). DOC-A.1-101's
  `.out` line 3 needs the 31-character EXCEPTION-STATUS width. The rest use `INVOKE <class> "NEW"` on a class with no
  `INHERITS FROM BASE`, which is non-conforming source (§14.9.23.3 SR3; New is defined only in BASE).
- ⚠ **Highest-value lead from this lane, still unfiled:** the refusal of `INHERITS FROM BASE` (COBOLNET0821) blocked 11 rows across misc-p6/p8/p9/p15.
  The compiler also accepts `INVOKE … "NEW"` on a class with no BASE (existing goldens `oo_hello`,
  `oo_ec_raise_object`, `l1_exit_raising_object_sending` depend on that). It needs a registrar note and a sweep.
- Undone input files after wave 4: dns-witness and misc-p16 … misc-p38 (24 files, about 229 rows), in `scratch/in/`.
- **Lane total: 111 rows witnessed; GAP 1627 → 1515.**
- gate: Conformance filter 5572/5572 (all 5 new goldens ran by name), Unit filter 25/25.
