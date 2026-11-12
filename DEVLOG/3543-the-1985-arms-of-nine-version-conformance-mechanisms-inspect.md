## Entry 3543 — 2026-11-12 13:45 PST — The 1985 arms of nine version-conformance mechanisms: INSPECT BEFORE/AFTER, the class condition, PERFORM BY, and six 2002 forms gated below 2002; PB5071, PB5081, PB5088, PB5091, PB5180: seven COBOL-85 restrictions the compiler applied the later editions' way, and duplicate-name messages that pointed at the wrong lines; PB5125: a relative WRITE of a key outside the file's boundaries answers 24 below 2014 and 34 from 2014; Wave 20262006 group A: the relative-connector chain finish (PB5125 review, the 2014 edge; PB5071 PB5081 PB5088 PB5091 PB5180 and the PB5120 branch carried); The data binder's eight edition-dependent rules: RECORD integers, condition-name sizes, SYNCHRONIZED on INDEX, THRU figuratives, KEY between, implicit qualifiers, constant-name uniqueness; The derivation register takes rows of every edition; two review findings of the data-binder branch

### The 1985 arms of nine version-conformance mechanisms: INSPECT BEFORE/AFTER, the class condition, PERFORM BY, and six 2002 forms gated below 2002

Wave 20261998 group C (kb/Work PB5120, PB5122, PB5177, PB5194, PB5179, PB5172, PB5159, PB5116, PB5170). Each note was
re-probed on the branch's build before its fix, and each reproduced as filed.

**PB5120, INSPECT (wrong answer).** At `--std 85` an operand with both BEFORE and AFTER looked for the BEFORE delimiter
only from the AFTER boundary, so AYAAXAA with AFTER "X" BEFORE "Y" tallied 2. §1985:VI.6.18.4 GR8 b, the 2002 text and
the 2014 FCD measure the BEFORE portion from the leftmost position, and the operand participates in the intersection
with the AFTER portion (0 here); §2023:14.9.22.4 GR9 b reworded it with BACKWARD. A behaviour row,
`DialectBehavior.InspectBeforeFromFirstEligiblePosition` (2023, VCR row 7.65), sets `BoundInspect.BeforeFromLeftmost`
below 2023 and `CobolInspect.Region` searches from position 0; TALLYING, REPLACING and CONVERTING share `Region`, so all
three changed. Witness `14.9.22-INSPECT/pb5120_before_after_intersection` (`.out` and `.2023.out`).

**PB5122, the class condition (rejects legal source and under-rejects).** The operand screen applied the 2023 rule at
85: a numeric edited item was refused for ALPHABETIC and class-name tests, and the 1985 NUMERIC bars were not asked.
Each alternative of `ClassConditionModel` now carries its §1985:VI.6.3.1.2 rule set, chosen by
`ClassAlternative.RulesAt`, with three codes: COBOL6916 (ALPHABETIC, class-name over a numeric item), COBOL6917 (NUMERIC
over an alphabetic item or a group holding a signed item) and COBOL6918 (usage DISPLAY, or an alphanumeric function).

**PB5177, PERFORM VARYING BY index-name.** §2023:14.9.28.2 and §1985:VI.6.21.2 both print BY { identifier | literal },
so an index-name in the BY slot is now COBOL2119 at every edition, through one slot-by-kind table (`Admits`).

**Six forms gated below 2002**, each a construct row and one visit in `VersionConformancePass`: a reference-modified
function-identifier as the UNSTRING sender (PB5194, the existing construct now asked of every sender alternative), a
partial expression as an EVALUATE object (PB5179, 2002 F.2 item 48), the CLASS clause's IN phrase (PB5172), REWRITE's
word RECORD (PB5159), the TYPE clause's omitted control operand, ON/FOR word and OR PAGE (PB5116), and a configuration
paragraph's lone second period (PB5170, read through `ParagraphPeriods.LonePeriod`). The migrated 85 case
`a413_declined_phrase_positive_control` wrote `REWRITE F-REC RECORD`, which the 1985 format does not print; it now
writes the 1985 spelling.

Every fix has a positive at its introducing edition and a refusal below it. PB5171 (the SPECIAL-NAMES clause sequence and
once-only clauses at 85) and PB5198 (the RERUN facility) are left for the next link of the chain: the first needs the
clause-list table to rank repeatable clauses, the second a design decision.

### Gate 000003 (RED) and its roots

The first full gate of the branch ran 40,748 Unit and 24,049 Conformance cases and found eight reds, all mechanical consequences of the fixes. The runtime ABI pin moved because the INSPECT entry points gained the `beforeFromLeftmost` parameter (a changed shipped signature): `abi_pin.py bump --runtime-major` took the runtime to major 26 and measured its hashes. The documented-facts file gained COBOL6916 to COBOL6918. `ConditionBinder` now asks `Place.DenotedItem` instead of a hand-written reference-modification test (PlaceDenotedItemDriftTests). Three older goldens (pb1090, pb1401_computed_numeric_numeric, pb230_class_numeric_image) claimed `accept: [85]` for constructs the 1985 standard refuses (a second period after SPECIAL-NAMES; §1985:VI.6.3.1.2 bars NUMERIC over a non-DISPLAY item and over a numeric function), so they now start at 2002, the edition that introduced them.

### Review R1-1: a level-66 alias escaped the 1985 NUMERIC bar

The branch review found that `IF GS-ALIAS IS NUMERIC`, where `66 GS-ALIAS RENAMES GS` and GS holds `PIC S99`, compiled at `--std 85` while `IF GS IS NUMERIC` drew COBOL6917. The bar read the operand's description from `Place.DenotedItem`, the identity half, and the alias's own entry has no subordinates. A description is an attributes question: §1985:VI.5.11.4 GR2 gives a no-THROUGH alias "all of the data attributes of data-name-2", and GR1 makes a THROUGH alias "a group item which includes all elementary items starting with data-name-2", which failed the same way. The bar now reads `MoveTable16.OperandItem` (the attributes half, null for a reference-modified slice), asks `ItemCategory.IsGroupItem`, and walks the new `DataItem.SubordinatesOf`. That one reader answers the members of a group item for both alias forms (data-name-2's descendants, or the THROUGH alias's `IncludedElementaryItems`). The sweep moved four more binder screens off the identity half or off `IsGroup` over an alias onto the same readers: the INVOKE ACTIVE-CLASS subordinate screen (OoBinder), the SEND/RECEIVE message-area walk (MessagingBinder), `RelationPair.GroupItemOf`, and the relation's variable-length pair check. StatementValidation's private copy of the operand-item reader is deleted. §2023:13.18.45.3 SR8 keeps object, pointer, strongly-typed and variable-length items out of a RENAMES range, so those four could not misfire on a conforming program today; they now ask the question the rule asks. Witnesses: `pb5122-numeric-signed-group-alias-at-85`, `pb5122-numeric-signed-group-thru-alias-at-85` (COBOL6917 at 85) and `pb5122_numeric_alias_operands_2002` (both aliases tested as content from 2002).

### PB5071, PB5081, PB5088, PB5091, PB5180: seven COBOL-85 restrictions the compiler applied the later editions' way, and duplicate-name messages that pointed at the wrong lines

Wave 20262000 group A. Every note was re-probed on the branch's build first, and each reproduced: the five --std 85
programs of PB5071, PB5088 and PB5180 compiled (exit 0), so did the six PB5081 programs, and PB5091's COBOL2122 read
"(paragraph 'P1' at line 9; paragraph 'P1' at line 11)" for paragraph-names on lines 8 and 10.

**PB5071, the file section.** 1985 requires record description entries after every FD (§1985:VII.3.2.3 SR3, and
the same rule in VIII and IX), and allows only ASSIGN after a sort-merge file's name in FILE-CONTROL (§1985:XI.2.3.3
SR2). The compiler applied 2002's rules at 85. `FileControlBinder.MaterializeImpliedRecord` now refuses a record-less
FD below 2002 with COBOL1837, before it asks for a RECORD clause, so the COBOL1836 fix (add a RECORD clause, which
1985 does not allow) is no longer suggested at 85. The file control entry now captures the first thing written after
SELECT and ASSIGN (`FileModel.BeyondAssign`, OPTIONAL included), and `FileControlKeyRules.Screen` refuses it for an
SD file below 2002 with COBOL1900, replacing the 2023 sort-merge rows there. The golden
`pb345_record_less_fd_area_85` had pinned the opposite reading ("carry no edition gate"), so it is now the 85 refusal.
The pb345 COBOL1836 refusals now run from 2002. FILES_DESIGN D18 says so.

**PB5088 and PB5180, INITIALIZE.** Both are COBOL-85 restrictions that the 2002 text lifts, so each is a construct row
(COBOL0900, VCR row 7.52 d and e). `initialize-variable-occurrence-2002` covers §1985:VI.6.17.3 SR4, which forbids
INITIALIZE of an item whose description, or a subordinate's, has OCCURS DEPENDING ON. The 2002 syntax rules 14.8.19.2
drop it. It is asked once for each resolved identifier-1, on the bound arm of the version-conformance pass
(`VersionConformancePass.GateCobol85VariableOccurrence`), because only the resolved item shows the OCCURS DEPENDING.
`initialize-replacing-category-set-2002` covers §1985:VI.6.17.2, which prints a REPLACING item's category words in
plain braces, so an item names one category (§1985:IV.2.1.4). The 2002 format 14.8.19.1 adds choice indicators. The
gate reports at the item's second word (`VersionConformancePass.VisitInitializeReplacingItem`). That construct rests
on general formats alone, so it joins `edition_gates.py`'s FORMAT_ONLY held-open list. `pb415_initialize_multi_category_85`
had pinned the acceptance and is now the refusal. `pb393_initialize_odo_group` now runs from 2002. The INITIALIZE leg
of `pb4854-debug-odo-count-is-implicit` became MOVE ZEROS TO TB, which tests the same reach rule with the same output.

**PB5081, the debugging facility.** Whether a section is a debugging section is now stored on the section
(`SectionInfo.IsDebugging`, one writer). It replaces the `_pendingDebug` scan behind `IsDebuggingSection`, which is
deleted, and its callers read the stored value. Six §1985:XV.3.2.3 rules are now enforced:
- SR1: a debugging section that follows another declarative section is COBOL6948.
- SR3: a reference to a debugging procedure from outside the debugging sections is COBOL6946, in
  `CheckDeclarativesBoundary`, PERFORM included.
- SR5: a debugging procedure named as a subject is COBOL6946.
- SR6: a procedure-name named twice is COBOL6335. `AddDebugSubject` used to drop the repeat without a word.
- SR7: ALL PROCEDURES written twice is COBOL6947.
- SR8: a procedure-name subject in a program with ALL PROCEDURES is COBOL6947.

`pb4811-debug-file-name-failures` had its debugging sections after its USE sections, which SR1 forbids. They now come
first, with the same output. docs/CONFORMANCE.md D-DEBUG-PROGRAM-RULES.

**PB5091.** `AtLine` printed `_paraLine`, which holds each paragraph's first-sentence line for DEBUG-LINE. Paragraph-names
now have their own lines (`_paraHeaderLine`), and so do sections (`SectionInfo.HeaderLine`), so COBOL2122 and COBOL2121
give the lines where the names are declared, including empty paragraphs and sections.

**Rows.** 1985-FMT-IV.6.3.2.2.1 and 1985-FMT-VI.6.17.2 were PARTIAL and are now CONFORMS (record_verdicts, 1985 GAP 606
to 604). The notes claim no rows: PB5094 still blocks 1985 ids in a note.

New codes COBOL6946 to COBOL6948. The gate's verdict is in the branch's report.

### PB5125: a relative WRITE of a key outside the file's boundaries answers 24 below 2014 and 34 from 2014

Re-probe on the Debug build (random WRITE of RK = 9999999999 into a dynamic-access relative file): `--std 85`, 2002, 2014 and 2023 all printed `WRITE BEYOND STATUS 34` with neither INVALID KEY phrase run. §1985:VIII.1.3.4 item (3) c) and §1985:VIII.1.3.5 require 24 and the invalid key condition at 85. The 2002 transcription (§2002:14.8.47.3 GR25 b) and GR29 b)) is the same: the '34' sentence of §2023:14.9.51.4 GR29 b) ("less than 1 or greater than the highest relative record number permitted") is absent, so the note's 85-only framing was too narrow and 2002 was wrong too. The first implementer read the 2014 edge from the FCD 1.0 draft and put it at 2023; the branch review (R1-1) found the sentence in the published 2014 text (§2014:14.9.47.3 GR26 b), `cite.py --check` OK), so the edge is 2014. The note's citation §1985:VIII.4.9.4 GR14 b) is the sequential-file rule; the relative rule is §1985:VIII.1.3.4 item (3) c) (checked with `cite.py`).

Fix: a new `DialectBehavior.RelativeWriteKeyOutOfRangeIsPermanentError` (changed in 2014, VCR row 7.64, ordinal 14 after main's 8-13, `compiler-facts.json`, PublicAPI). `RelativeConnector.Write` asks it for the one key-range test it already had: from 2014 `FileStatusCode.PermanentBoundary`, below it `FileStatusCode.BoundaryViolation`. Key 0, for which 1985 states no result, takes the same arm (the boundaries DOC-A.1-107 defines exclude it); `docs/CONFORMANCE.md` DOC-A.1-107, FILES_DESIGN and DESIGN-runtime-library say so. Sibling arms checked: `FileStatusCode.PermanentBoundary` has three producers (this one, `FileRegistry.ReadExceedsRecordMaximum` for an ODO READ, `ForWriteFailure` for a sequential medium boundary) and only this one is the relative key range; REWRITE and DELETE of an out-of-range key find no record and answer 23.

Goldens: `14.9.51-WRITE/pb5125_relative_write_key_range_pre2014` (85 2002: 24, INVALID KEY runs, the file holds record 3 only), `pb5125_relative_write_key_range` (2014 2023: 34, neither phrase runs), `pb5125_relative_write_key_range_declarative` (all four editions, no INVALID KEY phrase: the USE procedure runs for 24 and 22 below 2014 and nothing stays in effect; from 2014 it runs for each 34 the permanent error replays and for the READ's 23) and `pb5125_relative_write_key_range_file_status_2002` (2002, neither phrase nor USE: the FILE STATUS item alone carries 24 and execution continues, §2002:9.1.13 item 4); `pb1192_relative_write_boundary_85` moved from 34 to 24 at 85 (its expectation was the defect); unit `FileBoundaryTests.RelativeWrite_KeyOutOfRange_BelowTheFirstEditionWithGR29b_Is24`. No inventory rows closed (the inventory has no 1985 rows, PB5094); the 2023 row GR-14.9.51.4-29 stays CONFORMS.

The first gate (run 000001-20261111T105051Z) was RED on three cases the fix legitimately moved, each now corrected: NIST RL118A at `--std 85` now PASSes REL-TEST-2 (the 100th WRITE key lands as key 0, which is 24 at 85, so the duplicate-key WRITE answers 22 as the CCVS expects). That retires the CCVS-DEFECT reading of the re-baseline of 2026-09-30 (R68 item 6): the defect existed only because 34, a permanent error that stays in effect, was applied below 2023. The row is now `green` with `BEHAVIOR relative-write-key-out-of-range-permanent-error`; `tests/nist/valid/RL118A.txt` is the original PASS golden (restored from 3a62e528b0) and the FAIL one moved to `tests/nist/valid-editions/2014/RL118A.txt` (2023 at first; it moved with the edge), where INV-1-strong reads it. `pb1098_organization_permanence_85` expected `RL-KEY0=34` at 85 and now expects the INVALID KEY branch and 24. `compiler-facts.json` was regenerated (registry order).

The re-baseline's gate (run 000003-20261111T132017Z) was RED on one case, `CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects`: the new RL118A row note still said "CCVS-DEFECT at 2023", and that token declares a `valid/` golden carrying a CCVS failure, which `valid/RL118A.txt` no longer does. A per-edition golden's failure is allowed by its `BEHAVIOR` declaration (the NC201A form), so the note now says REL-TEST-2 asserts the pre-2014 '22' and FAILs from 2014, with no marker; `DESIGN-conformance-suite.md` no longer calls RL118A a CCVS defect.

### Wave 20262006 group A: the relative-connector chain finish (PB5125 review, the 2014 edge; PB5071 PB5081 PB5088 PB5091 PB5180 and the PB5120 branch carried)

This branch carries three unlanded branches, merged in this order: PB5125 (worktree-wf_7e916c7a-be9-1, reviewed FINDINGS 2), PB5071/PB5081/PB5088/PB5091/PB5180 (worktree-wf_07022f79-76e-1, which went SPLIT at the fleet stop with its gate GREEN at b62a6d13df), and the 1985 arms with PB5120 (worktree-wf_c902d12a-c75-3, reviewed CLEAN). Their entries `pending-pb5125-…`, `pending-w20262000a-…` and `pending-w20261998c-…` sit beside this one, and the landing's stamp folds all four into one entry.

**The merges.** While these branches were in flight, main took `DialectBehavior` ordinals 8 to 13 and VCR rows 7.58 to 7.63 (PB5248, PB5245, PB5246, PB5130, PB5161, PB5197). Both carried branches had also used ordinal 8 and row 7.58. `RelativeWriteKeyOutOfRangeIsPermanentError` is now ordinal 14 with row 7.64, and `InspectBeforeFromFirstEligiblePosition` is ordinal 15 with row 7.65. The enum, the table, `PublicAPI.Unshipped.txt`, `compiler-facts.json`, the VCR, DOC-A.1-107, the kb/Work addenda and the DEVLOG entries were renumbered together. VCR row 7.47 keeps main's reworded item 50 (PB5272) and gains the branch's item 48 (PB5179). Row 7.52 merges both sides' items as (a) to (i): (d) and (e) are the INITIALIZE items, (f) to (h) the CLASS IN, REWRITE RECORD and lone-period items, and (i) the record-less FD and sort-merge entry binder rules. `VersionConformancePass` keeps both new overrides (`VisitInitializeReplacingItem`, `VisitRewriteStatement`).

**Re-probes on this build.** Every carried fix still refuses at `--std 85` and compiles at 2002. PB5071 gives COBOL1837 and COBOL1900. PB5088 and PB5180 give COBOL0900. PB5081 gives COBOL6948, 6946, 6946, 6335, 6947 and 6947. PB5091's COBOL2122 names lines 8 and 10.

**Review R1-1 (PB5125): the edge is 2014, not 2023.** The published 2014 standard already carries the '34' sentence: §2014:14.9.47.3 GR26b reads "the execution of the WRITE statement is unsuccessful, and the I O status for the write file connector is set to 34" (`cite.py --check` OK). Only 2002 (§2002:14.8.47.3 GR25b) and 1985 lack it. The first implementer had read the 2014 edge from the FCD 1.0 draft. The behaviour row's `ChangedIn` is now 2014. The RL118A FAIL golden moved to `tests/nist/valid-editions/2014`. `pb5125_relative_write_key_range_pre2023` was renamed `…_pre2014` and accepts 85 and 2002, and `pb5125_relative_write_key_range` accepts 2014 and 2023. The 34 unit test runs at 2014 and 2023, the 24 test at 85 and 2002. Every doc that named the 2023 edge now names 2014: the VCR, DOC-A.1-107, FILES_DESIGN, DESIGN-runtime-library, DESIGN-conformance-suite and the corpus row. Before the fix, `--std 2014` answered `ABOVE=24 INVALID`. After it, `--std 2014` answers `ABOVE=34 NONE`.

**Review R1-2 (PB5125): the paths without an INVALID KEY phrase.** `pb5125_relative_write_key_range_declarative` (all four editions, with per-edition outputs) writes no INVALID KEY phrase and runs under a USE procedure. Below 2014 the procedure runs for 24, 22 and 24, and the WRITE of key 5 between them answers 00, so nothing stays in effect. From 2014 the procedure runs for each 34 that the permanent error replays and for the READ's 23 after the CLOSE. `pb5125_relative_write_key_range_file_status_2002` covers the case with neither the phrase nor a USE procedure: §2002:9.1.13 item 4 passes control to the end of the statement, and the FILE STATUS item alone holds the 24. COBOL-85 has no such path, because §1985:VIII.1.3.5 says a USE procedure "must be associated".

**Targeted tests, then the fix gate.** Before the gate: Unit (FileBoundaryTests, the DialectBehavior registry, DocumentedFacts, RuntimeAbiPin) 42/42 passed, and Conformance (the pb5125 cases, RL118A, CorpusManifestTests, pb1192, pb1098, EditionGateArmDriftTests) 188/188 passed. The fix gate at 24dd745674 printed `=== BUILD-LOCAL GATE: GREEN — Conformance 24,384/24,384 ran · Unit 40,805/40,805 ran · Characterization 37/37 ran (skipped 0) … (no class first; run 000001-20261112T183723Z) ===`, and its Conformance.trx records each new golden at each edition as `outcome="Passed"`. In the Linux gate (`--legs unit,conformance`) both legs were GREEN: Unit 40,914/40,914 and Conformance 24,384/24,384. Its selftests leg was RED on `scripts/hooks/test_forbidden_commands.py`, in two `git push https://github.com/...` cases that expected PASS. No file of this branch's group reaches that test, so it is registered for the orchestrator as a lead. `semgrep/verify.py`: PASS, with no rule's count raised.

### The data binder's eight edition-dependent rules: RECORD integers, condition-name sizes, SYNCHRONIZED on INDEX, THRU figuratives, KEY between, implicit qualifiers, constant-name uniqueness

Wave 20262006 group B took eight notes that share one root, `Binding/DataBinder.cs` and its passes, and one failure shape: a rule written
at one edition's strength and applied at every edition. Each note was re-probed first on the base (f08a6e1a5b) and all eight reproduced.

**PB5196 (RECORD clause, MAJOR, rejected legal source).** `RECORD CONTAINS 5 TO 5` was refused and `0 TO 5` / `FROM 0` accepted at `--std 85`.
§1985:VII.3.8 prints no ordering rule for Format 3 (its GR14 makes the integers the smallest and largest record size) and §1985:III.2
bars a zero integer; both rules first appear in §2002:13.16.41.2 (SR6, SR7, SR8). `RecordClauseRule` gained a `Since` column
(`AppliesAt`), `IntegerSlot` gained `ZeroFrom` / `At`, `IntegerOperandRules.Classify` takes the edition and COBOL2386 cites §1985:III.2
below 2002. A determination reversed: docs/CONFORMANCE.md and FILES_DESIGN had claimed a zero-minimum record "at every supported edition",
reasoning that the repository could not speak for the 85 to 2002 delta; the 1985 text is in the repository now. `pb721_zero_length_record_85`
became `_2002`, rows SR-13.18.43.3-7, -8, -9 now start at 2002, and `RecordClauseRuleDriftTests` re-derives the column from the 2002 and 1985 texts.

**PB5190 (condition-name VALUE size, MAJOR).** §1985:VI.5.15.3 SR3 bounds a nonnumeric literal "of an item" by the PICTURE, and a condition-name
has its conditional variable's characteristics (VI.5.15.5 GR1); the 2002 and later sentence says "elementary item" and "explicit PICTURE".
`ValueSubject.ForConditionName(pic, dialectLevel)` carries the picture's size below 2002 (alphanumeric and numeric-edited categories); five call
sites pass both. `pb598_condition_name_value_size`, accepted at 85 by the migrated default, is accepted from 2002.

**PB5215 (group's condition-name).** `88 G-N VALUE -1` under an ordinary group was never screened (a group has no operand picture, and while its 88 is read
a group cannot be told from a picture-less item). `CheckValueLiteralClassesPass.ScreenGroupConditionName` runs after composition through the same
`ValidateValueCategory` funnel and the same `GroupSubjectPic` the group's own VALUE uses: COBOL1657 at every edition.

**PB5189 (SYNC on USAGE INDEX).** `DataBinder.SynchronizedViolation` joins the BLANK WHEN ZERO and JUSTIFIED subject rules, below 2002 only (§1985:VI.5.14.3 SR6).

**PB5181 (THRU with figuratives).** `ConditionValueOf`, the one value reader of SR26 and SR27, gives a figurative constant, an `ALL` literal and a symbolic-character
its fill character (HIGH-VALUE and LOW-VALUE the sequence's extremes) repeated to the comparison's width, so `HIGH-VALUE THRU LOW-VALUE`, `SPACE THRU SPACE` and
`ALL "Z" THRU ALL "A"` are COBOL2961 at every edition; SR27's literal-4 test now weighs figuratives too.

**PB5192 (OCCURS between key and subject).** Below 2002 `OccursEntryBetween` asks every entry in source order (§1985:VI.5.8.3 SR12); the hierarchy-only walk is the 2002 narrowing.

**PB5165 (implicit qualifiers).** One edition test, `ClauseOperandsTakeImplicitQualifiers`, asked by `EntryClauseCandidates` (DEPENDING ON) and the new
`RegionClauseCandidates` (KEY, RENAMES): below 2002 the whole scope's candidates are counted, so a name in two records is COBOL1639. Two corpus goldens
that were not legal 1985 source (`pb966`, `1985-GR-VI_6_4_3-4`) now qualify their RENAMES operands.

**PB5301 (constant-name and data item of one spelling).** `ConstantEntryBinder.ConstantOfIdentifierPosition` counts the constant with the data items of its spelling in the
literal-alias resolution and the report SOURCE/SUM operand: COBOL1639 instead of silently taking the constant. `KK OF G` still names the data item.

Goldens: 27 new programs (refused below or from the introducing edition, accepted at the other), ten inventory rows restated or witnessed through `record_verdicts` batches, GAP
0 to 0 (the 1985 rows are not in the inventory, PB5094). Codes COBOL7021-7023 were not needed: every refusal reuses an existing code.
Found and left: `FD F LINAGE IS KK` with a constant and a data item KK still takes the constant; RECORD KEY IS K with a second K in working-storage is accepted at every
edition (`FileControlBinder.ResolveFiles` prefers the record's candidate).

Self-review: the four lenses found one shape defect in my own diff, `RecordClauseRule.Since = 1985` against `DialectLevel` 85 (eleven refusal goldens went red at the
first gate), and the placement of the constant reading on `ConstantEntryBinder` (a new reader of the binder's members; it moved to `DataBinder`). Gate (run 000005-20261112T192543Z
at 209a4a03d9): GREEN, Conformance 24,330/24,330, Unit 40,803/40,803, Characterization 37/37; every new golden passed in Conformance.trx.

Review round 1 (R1-1): `ConstantOfIdentifierPosition` counted a container's GLOBAL data item against a constant the contained program declares itself, refusing a legal nested program with COBOL1639. §2023:8.4.6.2.1 rule 3 a) makes the own-element declaration the referenced item, so a locally declared constant now counts only the data items of declaration depth 0. Golden `8.4.2.2-Qualification/pb5301_constant_nearer_than_container_global_data` (DISPLAY KK shows 5). R1-2: the ConditionValueOf comment now quotes §2023:8.3.3.6.4 GR2 as printed (cite.py --check OK).

### The derivation register takes rows of every edition; two review findings of the data-binder branch

Wave 20262008 group B finished the w20262006 b branch (PB5165 and its data-binder siblings) and its review, and built
kb/Work PB5374.

**Review R1-1 (blocking): an inherited GLOBAL constant against a farther GLOBAL data item.** The finisher's fix to
`DataBinder.ConstantOfIdentifierPosition` counted only depth-0 data items for a constant the element declares itself
and every data item for a constant inherited from a container. Re-probed (`n3.cob`: OUTERP `01 KK PIC 9 VALUE 1
GLOBAL`, MIDP `01 KK CONSTANT IS GLOBAL AS 5`, INNERP `DISPLAY KK`): COBOL1639. §2023:8.4.6.2.1 GR3b.1 makes the nearer
declaration the referenced item, so the count is now the data items at the constant's own tier (0, or
`InheritedDepthOf`); a nearer data item has already dropped the constant (`InheritStep.DropShadowed`). After: `5`, and a
container declaring both a GLOBAL constant and a GLOBAL data item KK still draws COBOL1639. Goldens
`8.4.2.2-Qualification/pb5301_global_constant_nearer_than_farther_global_data` and
`pb5301-global-constant-and-data-same-container`.

**Review R1-2: the citation of the 1985 condition-name size refusal.** COBOL1740 on `88 XXC VALUE "ABC"` under `PIC XX`
at `--std 85` cited §2023:13.18.63.3 SR4, which does not reach a condition-name. `ValueSubject.CharacterSizeRule` now
names the rule per subject kind (§1985:VI.5.15.3 SR3 for a condition-name), read by both character-size arms of
`ValidateValueCategory`; pinned in `ConditionNameValueSizeTests` and the pb5190 diagnostic expectation.

**PB5374: the derivation register served the 2023 inventory only.** A fifth arm, `undefined-in-rule` (mechanical: the
row's own catalog text declares the result undefined, the quotation checked inside it; refused where Annex A.2 lists
the rule); `signers` with the deciding session's `claude: <date>` keys mapped to a decision note, and an argued arm so
signed standing only on an UPHELD refuter record named in §8's new `Refuted` column; every check reads the row's own
edition's catalog (`Schema.catalog`); the audit, the drift test and the population guard read every held edition and
refuse a §8 row no row claims. Both engines and the parity fixture (28 cases) changed together. No row is recorded:
PB5373's adjudication records the 1985 class 1 and 2 rows.

Branch review R1-1 found the refuter record file a §8 row names missing from both hand lists of the files a commit's
view reads ahead (`inventory_at`, `gen_ledger.measure_commits`): a `Tree` with `files` read it as absent, so a
claude-signed argued row would have computed GAP at a commit while the live view computed OK. Fixed at the shape: one
list (`Schema.view_files` plus `refuter_files` of the commit's register, read in two stages by `read_view_ahead`;
`measure_commits` reads the same two stages for every commit, two more git processes only when a register names a
refuter file), and a read-ahead tree now raises `NotReadAhead` on any path it was not asked instead of answering
"absent", so the next input a list misses fails by name. The sibling lists `STAMP_PATHS` and `missed_points`'
pathspec read the same `VIEW_INPUTS`. Self-tests: `inventory_view` (the argued row's refusals at the read-ahead tree are
none; the schema-only stage raises naming the file) and `gen_ledger` (the commit's view reads the refuter record UPHELD).

