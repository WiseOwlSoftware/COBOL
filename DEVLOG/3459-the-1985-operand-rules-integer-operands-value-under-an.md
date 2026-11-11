## Entry 3459 — 2026-11-10 20:44 PST — The 1985 operand rules: integer operands, VALUE under an EXTERNAL record, the target-less GO TO and the segment-number ranges; COBOL-85 mandatory exception phrase and the record-area FROM/INTO rule (PB5076, PB5077); COBOL-85 Report Writer description, entry and GENERATE rules are refused, and three refusals stand at the clause that breaks the rule; The run unit's external file connector rules at every edition, and the obsolete COBOL-85 file description clauses' operands; The COBOL-85 Segmentation module: its flow restrictions and an independent segment's initial state

### The 1985 operand rules: integer operands, VALUE under an EXTERNAL record, the target-less GO TO and the segment-number ranges

Wave 20261973 group B, the version-conformance pass over the 1985 `--std=85` triage notes. Every note was re-probed on
7f4f67c7f1 with this tree's `cobol.exe`; each reproduced.

**PB5074 (landed).** At `--std 85`, `ELT (IDX)` over `PIC 9V9`, `ELT (-1)`, `ELT (1.5)`, `DISPLAY 1.5`, `SET IX UP BY N` over
`PIC 9V9`, `SET IX TO -1` and `SET IX TO 0`, `STOP 1.5`, `STOP -1` and `STOP ALL "A"` all compiled, and `STOP SPACE` and
`STOP ZERO` communicated the words. One mechanism: the 1985 general-format `integer` (§1985:III.2, "neither signed nor zero
unless explicitly allowed by the rules for that format") was never asked, only the operand's shape. It now has one reader,
`Cobol85OperandForms.IsInteger` (Frontend.Parsing, beside `GoToFormats`, so the edition gate and the binders share it with no
Binding-to-Validation edge; the gate's first run refused exactly those edges), which takes the sign each format admits
(`Cobol85IntegerSign`) and reads a sign the parser gave the unary operator when it touches the literal; the literal rule "an
unsigned integer" (DISPLAY SR2, STOP SR3) is `Cobol85OperandForms.IsUnsignedIntegerLiteral`. Where the 2002 text relaxes a rule, the fix is the edition window naming COBOL-2002 (COBOL0900):
the subscript row now covers §1985:IV.4.3.8.2.3 SR5 (the data-name's description, asked in `SubscriptBinder`, which asks the
parse arm's shape test first so one subscript draws one diagnostic) and SR6; the DISPLAY row, renamed
`display-numeric-literal-2002`, covers all of §1985:VI.6.10.3 SR2; the new row `set-arithmetic-expression-2002` covers
§1985:VI.6.23.3 SR2 to SR4 against the 2002 format's arithmetic expressions (VCR row 7.52 (c)). STOP literal is COBOL-85's
alone, so SR1 and SR3 are COBOL6817 there; its keyword figurative constant is now communicated as its character
(`ConcatFolder.FigurativeChar`). `edition_gates.py`'s `HELD_OPEN` loses the subscript row, which now rests on SR5 and SR6.

**PB5072 (landed).** A VALUE on an entry subordinate to `01 EXT-REC EXTERNAL` compiled at 85: the gate asked the VALUE's own
entry. The entries of a section are a flat list in the parse tree, so the walk decides the record once, at its level-01
(or 77) entry, and follows a level-01 REDEFINES to the record it redefines (§1985:VI.5.15.3 SR4's "redefinition"). Review
round 1 (R1-4) found the first shape, a backward scan of the section per VALUE clause, quadratic at every edition (20,000 VALUE
entries compiled in 60 s against 13 s on main); the per-walk state compiles them in 10-11 s, pinned by
`ExternalRecordValueGateTests`.

**PB5087 (landed).** A target-less `GO TO.` beside another statement compiled at 85. §1985:VI.6.15.3 SR3 is stated over the
statement sequence, so it is a row of `PlacementRules` (the funnel's sequence-placement rules, beside the bare EXIT's), with
COBOL6818; a row may now name its statement's construct row and is asked only where that row is available.

**PB5084 (in part; stays open).** The Segmentation module's three range rules are checked at 85 (COBOL6819). The MERGE
output-procedure flow rules and the independent-segment initial-state rules remain; the second contradicts VCR row 7.18's
documented posture and is flagged for an owner decision in the note.

**PB5069, PB5070 (not started).** Both need diagnostic codes beyond this wave's three; they return to the queue.

Goldens: `pb5074_*` (2002 to 2023) and `pb5074-*-at-85`, `pb5072_value_under_external_record_2002` and its refusal,
`pb5087_bare_go_to_single_statement_paragraph` (85) and its refusal, `pb5084_segment_numbers_in_range_85` and three refusals,
and `StopLiteralFigurativeTests` (the operator channel is standard error, which a `.out` does not read).

### COBOL-85 mandatory exception phrase and the record-area FROM/INTO rule (PB5076, PB5077)

Two explicit syntax rules of the X3.23-1985 I-O modules compiled clean at `--std 85`. Both were re-probed on the build first: all 22
repro programs of the two notes (11 each) compiled with exit 0 and no diagnostic, as filed.

**PB5076, the mandatory phrase (COBOL6826, 85 only).** Every 1985 I-O module says READ, WRITE, REWRITE, DELETE and START "must"
carry AT END or INVALID KEY when no applicable USE AFTER STANDARD EXCEPTION procedure is specified. 2002 prints no such rule. The check
is one type, `ExceptionPhraseRequirement`, asked at the five keyed binders and the sequential READ. Which statements need the phrase
is the question FLAG-14 d already asked ("can be specified with an INVALID KEY phrase"), so `FlagConformancePass.AdmitsInvalidKey` moved
to `IoPhraseAdmission` and both passes read it; the sequential READ adds AT END. "Applicable" is §1985:VII.4.6.4 GR5: a declarative
naming the file, or one for an open mode, in this program or, written GLOBAL, a containing one (§1985:X.5.5.4 GR1 b; the branch
review found the first cut counted a container's USE without GLOBAL, which no contained statement selects). My first reading let a mode declarative cover only
the verbs the mode suits (a USE ON OUTPUT not a READ); CCVS-85 SQ148A refuted it: it READs a file open OUTPUT on purpose and the OUTPUT
declarative takes the status 47. A mode declarative now covers every statement, since the rule refuses statements and a program that might
be covered is not refused. A contained program's READ is covered by its container's declarative. The container's
declaratives arrive through a lazy `ContainerUses` that `BinderDriver` sets on each contained unit's binder.

**PB5077, the operand rule (COBOL6827, 85, 2002 and 2014).** READ INTO and RETURN INTO may not receive into the file's record area;
WRITE, REWRITE and RELEASE FROM may not send from it. The notes called it an 85 rule; the 2002 text prints all five (§2002:14.8.29.2 SR2,
14.8.30.2 SR2, 14.8.32.2 SR1, 14.8.33.2 SR1, 14.8.47.2 SR4), so the gate is `Year < 2023`, where §2023:14.6.10 replaces it. The 2014 text
is not in the repository; the lineage records route 1985 to 2023 across it and 2014 keeps the rule (a determination, docs/CONFORMANCE.md
section 3.4). The five phrases already funnel through `MoveBinder` (D23), so the screen sits there once:
`StatementValidation.CheckPhraseOperandIsNotRecordArea`, from `BindFromPhrase` (which now takes the file) and `CheckIntoReceiver`. Its
question, `StorageSharing.IsRecordAreaOf`, is exact where `MayShare` is conservative, because it refuses a statement: a record of the file
or of a SAME RECORD AREA peer, or a subordinate that begins the record and is as long as it. A part of the record, a reference-modified
operand and another file's record stay legal. `RecordAreaRule` holds the edition scope, the "must"/"shall" wording and the one citation
per verb and organization (the 1985 text repeats the rule in three modules); the verb is a new column on `FromPhraseRules` and
`IntoPhraseRules`.

**What the gate found.** The first full gate failed 60 tests, every one COBOL6826: the 1985 corpus was full of programs that
omit the phrase, which no earlier compiler said. CCVS-85 SQ148A was the one that corrected the design (a mode declarative covers
every statement). The rest were our own tests, written when `accept: [85]` meant "compiles at 85". They were migrated, not exempted:
227 statements in 48 conformance programs now carry `AT END CONTINUE`, `INVALID KEY CONTINUE` or the right pair, each with its
`END-` terminator (a bare phrase inside a longer sentence draws COBOL2796); the phrase is `INVALID KEY` for a random read and
`AT END` for a sequential one, because the other draws COBOL1720. `pb3830_phrase_format_selectors` keeps its inner statements
phrase-free, since the association rule is what it tests, and gets a do-nothing `USE` procedure instead. `l1c11_invalid_key_no_phrase_no_use`
pins the no-phrase behavior of 2023:9.1.14 GR4, so it is accepted from 2002 only. Six inline programs in `KeyedIoDifferentialTests`,
`ClauseOperandQualificationSpecTests` and `Ansi85AcceptanceTests` were patched the same way; the four differential goldens are named by
a hash of the source, so they were renamed with it (their expected output did not change). `RecordAreaRule` moved to `Binding.Validation`
(its text-returning members are diagnostic wording, and the backend contract refused them under `Binding.Model`).

**Witnesses.** Conformance: the legal sides `pb5076_sequential_read_without_at_end_covered_by_use`,
`pb5076_keyed_statements_without_phrases_covered_by_use_io` and `pb5077_from_into_outside_the_record_area` (run at all four editions),
the 2023 acceptance `pb5077_from_own_record_area_accepted_2023`, six `pb5076-*-85` refusals (one per verb) and five `pb5077-*` refusals (one per verb, refused at 85, 2002 and 2014). `IoExceptionPhraseRequirementTests` pins the scope:
every USE mode against every verb, the access-mode qualification of DELETE and REWRITE, a contained program, and the record-area boundaries.
Design: D31 of `docs/FILES_DESIGN.md`.

**The 1985 cases main added since.** Four of the 1985 draft-triage cases main placed after this branch was cut WRITE a relative or indexed file with no INVALID KEY phrase and no USE procedure (`1985-GR-IX_4_5_4-18`, `1985-GR-VIII_2_6_3-1`, `1985-GR-VIII_4_5_4-16`, `1985-GR-VIII_4_6_4-5`): the merged tree's gate refused them with COBOL6826, as §1985:VIII.4.9.3 SR3 and §1985:IX.4.9.3 SR3 require. Each WRITE now carries the phrase; no write takes it, so the expected outputs stand.

### COBOL-85 Report Writer description, entry and GENERATE rules are refused, and three refusals stand at the clause that breaks the rule

PB5078 (five rules on the report description and GENERATE), PB5079 (seven on the report group entry) and PB5090 (three positions) share one root: ReportSectionBinder screened the 2023 text at every edition and had no arm for the 1985 text. Every rule was re-probed on the build before the fix and every one compiled at --std 85. Each arm below is gated by `Edition.DialectLevel < 2002`, except the RESET level, which both texts state.

PB5078: CONTROLS asks the resolved data items (1985:XIII.3.7.3 SR2; the written-reference loop is 2002+ only, `ScreenControlItemsDistinct`); a PAGE LIMIT over 999 (XIII.3.8.3 SR2); a detail, control heading or control footing group whose unconditional relative lines reach past its lower limit from FIRST DETAIL cannot be presented on one page (XIII.3.8.3 SR9, with SR8 c and d); a CODE literal that is not two characters, and the identifier form the 1985 format does not print (XIII.3.6.3 SR1); GENERATE report-name over a report with more than one detail group (XIII.3.20.3 SR6, COBOL6823).

PB5079: the first LINE clause of a page footing must be absolute (XIII.3.15.3 SR9); NEXT GROUP needs a LINE clause in the group (XIII.3.16.3 SR1); a SOURCE in a control footing, page heading, page footing or report footing group must not reference a group holding a control data item, an item below one, a redefinition or renaming of part of one, and in the page groups the control data item itself (XIII.3.20.3 SR5, COBOL6825; the USE BEFORE REPORTING half is a walk of the declarative and is left as a lead); the format 2 and format 3 entry rules, SR4 c, SR6 and SR8 (`ScreenReportEntryFormats`, COBOL2247). The RESET level (COBOL6824) is asked at every edition: 1985:XIII.3.19.3 SR4 refuses an operand lower than the footing's own control, and 2023:13.18.54.3 SR8 (same text from 2002) refuses one that is not higher, which the note found unenforced at 2023 as well.

PB5090: COBOL6169 stands at the repeating CD clause or series member, COBOL2046 at the SUM UPON operand (`ReportDetailRef.Ctx`) and COBOL0897 at the repeating file-name (with the mode and report-group duplicates beside it in `DeclBindUse`). The goldens pin line and column in their `expect`. The 2002 or 2023 citations at --std 85 stay with PB4950, except COBOL6169, which now cites 1985:XIV.2.2.3 below 2002.

Goldens: for each 1985 rule one legal side accepted from 2002 (`pb5078-*-legal`, `pb5079-*-legal`) and one refusal at 85, plus `pb5079-reset-higher-control` legal everywhere and `pb5079-reset-lower-control` refused at every edition, and the four positioned refusals `pb5090-*`. No inventory row closed (the traceability inventory has no 1985 rows). New codes COBOL6823 to COBOL6825. Gate verdict: see the commit that follows.

Review round 1: R1-1 (blocking) was right. `SourceControlRelation` decided SR5 c from the shape of the REDEFINES and RENAMES chains, so it refused a SOURCE item that shares no byte with the control and accepted a RENAMES THRU span that holds the control between its ends. It now asks storage overlap through `StorageExtent.OfItem` (the layout's own offsets; a RENAMES item spans From through Thru, 2023:13.18.45.4 GR2), and an extent it cannot prove is not asked. Goldens `pb5079-source-redefinition-disjoint-legal`, `pb5079-source-renames-thru-covers-control` and its legal side. Leads: R1-2 `pb5079-reset-own-control` (legal at 85, refused 2002+) and the corrected legal-side name in `pb5079-reset-lower-control`; R1-3 the CD duplicate-name message now quotes the 1985 words ("must be unique within the/a CD entry").

**Merge with PB5128.** Main's PB5128 rewrote `pb1187_terminate_restore_last_85`'s expected page footing to the 1985 current values (PF 3 4) while this branch moved the case to 2002, 2014 and 2023 (its page footing breaks §1985:XIII.3.15.3 SR9 and §1985:XIII.3.20.3 SR5). Git merged both halves cleanly, so the merged case expected the 1985 value at the editions where §2023:14.9.46.4 GR3 gives the prior 1 1. The case now expects PF 1 1 again, and its comment points to `pb5128_terminate_page_footing_control_values`, which carries the 1985 edge.

### The run unit's external file connector rules at every edition, and the obsolete COBOL-85 file description clauses' operands

Wave 20261979 group A finished the binder-driver group: it replayed three unlanded branches onto main (wave 20261973 B: PB5074,
PB5072, PB5087, part of PB5084; E: PB5076, PB5077; D: PB5078, PB5079, PB5090), answered their three review findings, and fixed
PB5069 and PB5070. The replay's only conflicts were append-only: two kb/Work notes, the diagnostic catalog, the code list of
the documented facts, and docs/CONFORMANCE.md, where main's new 3.4 (MULTIPLE FILE TAPE) made PB5076/PB5077's determination 3.5.

**Review findings.** PB5076 R1-1: `BinderDriver.ContainerUsesOf` counted every declarative of a containing program as
"applicable" to a contained program's READ; §1985:X.5.5.4 GR1 b) selects outside the statement's own program only "The
declarative in which the GLOBAL phrase is specified", as the run time's outward walk does, so it now yields GLOBAL ones only (the
unit test that pinned the wrong reading is replaced by two theories). PB5079 R1-1: the SOURCE control-data rule
(§1985:XIII.3.20.3 SR5 c) skipped every table element, because `StorageExtent.OfItem` has no one extent under OCCURS. A new
`StorageExtent.OfEveryOccurrence` gives the occurrence whose subscripts are all 1 and each OCCURS level's stride and count, with an
exact, pruned overlap test, on the record layout's own walk (`RecordLayout.OffsetInRootWithEveryIndexOne`). R1-2: the
SOURCE rule's private RENAMES span computation is gone; `StorageExtent.OfItem` already places a level-66 alias where the layout
does. Goldens `pb5079-source-table-redefines-control` (refused 85), its legal side, and `pb5079-source-table-disjoint-legal`.

**PB5069.** Re-probe: a PADDING CHARACTER item that is not external, and two programs whose external relative file differs in
access mode and names a non-external RELATIVE KEY, all compiled at `--std 85` (the access mode at 2023 too). The compiler compared
only FILE STATUS and RELATIVE KEY, and only at 2023. `FileEntryIdentity.Segments` is now the one list of what a file control entry
specifies, one segment per GR1 item with PADDING CHARACTER added; `ExternalFileConnectorRules.Check` compares it for the entries of
one compilation group at every edition (COBOL6847), and the run time's EC-EXTERNAL-FILE-MISMATCH compares the same segments joined.
RELATIVE KEY must be the same external item at every edition (§1985:VIII.2.4.4 GR8), PADDING CHARACTER's data-name an external item
at 85 and 2002 (§1985:VII.2.7.4 GR6). `ExternalItemIdentity` moved onto `DataItem` (part of PB2418), so the emitter no longer calls
the binder driver. The three run-time EC-EXTERNAL-FILE-MISMATCH goldens now build each called program as its own unit
(wiseowl/cases); the one golden that asserted a non-external RELATIVE KEY compiles at 2014 is a refusal there now.

**PB5070.** Re-probe: `VALUE OF "F1DATA"`, `LABEL RECORDS ARE IGNORED`, a two-character padding literal or item, an index item or
File Section item in VALUE OF, and a DATA RECORD naming an 02 item all compiled at `--std 85`. The grammar is now the printed
format (LABEL takes STANDARD or OMITTED; VALUE OF is a list of implementor-name IS operand pairs), and the syntax rules are
COBOL6848 (PADDING CHARACTER) and COBOL6849 (DATA RECORDS, VALUE OF). The NIST X-cards XXXXX075-079, the VALUE OF object, became
literals: as data-names they would name nothing the programs describe.

**Not done.** PB5084's remainder (the SORT, MERGE and PERFORM range rules of segmentation and the independent segment's initial
state) has no diagnostic code left in this wave's allocation and its second half needs an owner decision; the file DESCRIPTION
entry's own run-unit agreement rules (LABEL RECORDS, BLOCK CONTAINS, CODE-SET, RECORD, VALUE OF) need an FD identity beside
`FileEntryIdentity`.

### The COBOL-85 Segmentation module: its flow restrictions and an independent segment's initial state

Wave 20261983 group A finished the binder-driver group. It replayed the fifteen unlanded commits of `worktree-wf_82a7f4cd-626-1`
(wave 20261979 A) onto main. Main had meanwhile split `tests/version-matrix/constructs.json` into one file per construct
(kb/Work PB5110), so the two commits that changed construct rows were ported row by row onto the new store and the registry was
regenerated; the other conflicts were append-only (three kb/Work notes, the code list of `compiler-facts.json`, VCR row 7.52 beside
main's new 7.53). The replay gated GREEN at ce1090a2f9 (run 000001-20261110T185145Z: Conformance 23,023, Unit 40,709,
Characterization 37).

Then it finished PB5084, the remainder of the Segmentation module (§1985:XVI), which VCR row 7.18 had recorded as "deliberately NOT
implemented". On the replayed build the note's repro printed INITIAL then ALTERED, the refuter's SORT variant printed FIRST then
LATER, and each flow-rule shape compiled. The module now has one model, `Segmentation`: each section carries its segment-number
(`SectionInfo.Segment`), and

- the restrictions on program flow (§1985:XVI.3.3.1 ALTER, XVI.3.3.2 PERFORM, XVI.3.3.3 MERGE, XVI.3.3.4 SORT) are COBOL6877, asked
  where each statement binds. The first version closed each range over the procedures it performs (the range of §1985:VI.6.21.4
  GR11); it refused the CCVS-85 programs SG102A and SG103A, which perform the fixed-portion report paragraphs from inside an
  independent segment's range, so the rule is asked of the procedures a statement names (docs/CONFORMANCE.md §3.8);
- an independent segment's initial state (§1985:XVI.1.4.3; §1985:VI.6.7.4 GR1) is the dispatcher's: `__SegOf` holds each pc's
  segment, the loop head calls `__EnterSegment` whenever control enters a pc of another segment (which resets that segment's
  alterable GO TOs to their written targets), and `__Dispatch` restores the caller's segment in a `finally`, so the return from a
  PERFORM, SORT or MERGE procedure or declarative leaves the segment in its last-used state (CONTROL_FLOW_DESIGN D4). The
  scaffolding is emitted only for a program with an alterable GO TO in an independent segment;
- under --permissive at a later edition the segment-number keeps its run-time meaning, as ALTER does, without the 1985
  restrictions.

Witnesses: `14.2-Procedure-division-structure/pb5084_independent_segment_initial_state_85` (fall-through, PERFORM, GO TO and SORT
entries, a fixed segment's ALTER kept, an ALTER kept across a PERFORM return; main's compiler printed ALTERED on four re-entries and lost a
SORT record), `pb5084_independent_segment_called_last_used_85` (a second CALL of a program whose first section is independent finds it last-used, §1985:X.5.2.4 GR5, which an earlier draft that started `__seg` at -1 broke) and six COBOL6877 refusals, one per arm. The CCVS-85 segmentation programs SG101A-SG106A and SG201A-SG204A become
asserted NIST rows with their reports as goldens: on the replayed build SG102A, SG103A, SG201A and SG203A failed 1, 3, 23 and 4 tests,
and now none fails (SG201A and SG203A also pass at `--std 2023 --permissive`). One diagnostic code was used (COBOL6877); COBOL6878
and COBOL6879 are returned. PB5084 lands with `closes_rows: []` (a lead that claimed no inventory row).

