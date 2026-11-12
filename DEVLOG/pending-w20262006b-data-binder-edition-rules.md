## The data binder's eight edition-dependent rules: RECORD integers, condition-name sizes, SYNCHRONIZED on INDEX, THRU figuratives, KEY between, implicit qualifiers, constant-name uniqueness

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
