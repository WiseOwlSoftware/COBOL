## Nine 1985 grammar and binder defects: ADD GIVING's second addend, the communication operand rules by edition, function-identifiers as CALL, CANCEL and KEY targets, and four 1985 format shapes

Wave 20262001 group B took nine kb/Work notes rooted in `CobolParserCore.g4` and the procedure binders. Every note was
re-probed on this branch's base (e3c9e8551a) and reproduced as filed; one checkpoint per mechanism.

**PB5203 (wrong answer, every edition).** `ADD A GIVING D` compiled and stored A. §2023:14.9.2.2 Format 2 (and
§1985:VI.6.6.2) prints the second addend in braces after the optional word TO, so it is required (§2023:5.2.6.3).
`ArithmeticBinder.BindAdd` now refuses a Format 2 of fewer than two addends with COBOL1689. The sweep found three test
programs that wrote the illegal form (the PB1606 float conversion and PROHIBITED tests, and
`OnSizeErrorDifferentialTests.MultiReceiver`); each now writes `ADD x TO 0 GIVING` with the same expected value, and the
differential golden moved to its new source hash.

**PB5278, PB5289, PB5082 (the communication operand screens).** `CommunicationBinder` applied ISO 2002's class rules
(A.2.5.2 SR2, A.2.6.2 SR2) at 85, where §1985:XIV.3.5.3 has no rule on RECEIVE's identifier-1 and §1985:XIV.3.6.3 SR6
restricts SEND's only as a function. `ClassRules2002` makes them 2002 rows; a loopback-MCS golden sends and receives a
numeric item, a short message into a numeric item (GR3: no space fill, 12999), a numeric-edited and an alphabetic item.
The SEND identifier-2 and ADVANCING identifier-3 screens now read `PicInfo.IsIntegerDescription` (PIC 9P and 9PP are
integers; USAGE INDEX is not), refuse a symbolic-character, constant-name or index-name at identifier-3, and take
`ALL ZERO` as the bound figurative ZERO; a constant-name of an unsigned integer stays integer-1 (`pb4677_send_advancing_constant`). The ENABLE and DISABLE KEY operand is now asked §1985:XIV.3.2.3 SR4 and
§1985:XIV.3.3.3 SR4 ("defined as alphanumeric", COBOL6171). `DESIGN-communication-facility.md` §4 carries the per-edition
operand table.

**PB5240 (half).** `callTarget`, `cancelTarget` and `communicationKeyPhrase` now take the one `sendingOperand` rule, so an
alphanumeric function-identifier is identifier-1 there (§2023:8.4.3.2.3 SR1; §1985:IV.4.3.8.3.3 SR3) at every edition.
`CallBinder` asks SR1's category of such a target (`OperandTargetRefusal`: a returning item through `NameTargetRefusal`, a function through its §2023:15.2 result category; COBOL1681 / COBOL1696). The golden calls and
cancels a contained program through UPPER-CASE and REVERSE, and the callee's call count proves each CANCEL reached it.
The report SOURCE site the note's addendum names stays open: admitting a function as identifier-1 there changes the
report writer's SOURCE model.

**PB5239.** `SourceUnitShape.Screen` refused, at 85, a program with contained programs and no Procedure Division
(COBOL2274). §1985:X.2.3.1 prints `[ procedure-division ]` and `[ source-program ] ...` as separate brackets; the 2002
nesting is now asked from 2002 only.

**PB5298.** At 85 `PROGRAM COLLATING SEQUENCE IS REV-SEQ SEGMENT-LIMIT 49.` was refused: the optional 2002 alphabet-name-2
took the word SEGMENT-LIMIT. The second name now asks `objectComputerClauseAhead`, and `segmentLimitAhead` reads IS or the
integer after the word. One refuter probe (`f7_overturn.cob`) put `IS 5.` past column 72; the corrected source compiles.

**PB5167.** Two construct rows, introduced 2002 (COBOL0900): `file-control-entry-optional-2002` (an INPUT-OUTPUT SECTION
without FILE-CONTROL or a FILE-CONTROL with no entry; §1985:IV.6.2.3.1, §1985:VII.2.2.2) and `repository-paragraph-2002`
(the empty REPOSITORY paragraph; every entry kind already had a row). `l1c16_io_section_paragraphs` is accepted from 2002.

**PB5178.** The ENTER statement at 85: the separator period is a placement row (`EnterEndsSentence`, new code COBOL6973);
routine-name-1 is the new `routineName` rule (`cobolWord | reservedGatedWord`), so FREE, UNLOCK and VALIDATE are
routine-names; and `UserWordKind.RoutineName` is declared through the one funnel, so a routine-name that is also a
paragraph-name is COBOL2692.

No inventory row moved: every row these notes claim is a 1985 rule id, which `work.py check` refuses in `inventory_rows`
(kb/Work PB5094). Codes: COBOL6973 used; COBOL6974 and COBOL6975 returned.

Branch review round 1 found that the new computed CALL-target arm had no program-pointer case (R1-1). `CALL FUNCTION GETP`,
where GETP's RETURNING item is USAGE PROGRAM-POINTER, was refused COBOL1681, although §2023:14.9.4.3 SR1 admits a
program-pointer identifier-1 and §2023:8.4.3.2.4 GR1 gives the result temporary the returning item's category. Both CALL
arms now go through one screen, `CallBinder.Identifier1Screen`. A program-pointer operand of either form activates through
`ProgramRegistry.CallPointer` after the SR14 signature check; any other category goes through `OperandTargetRefusal`.
Golden: `pb5240_call_function_program_pointer` (2002, 2014, 2023; PCFPB counts 1, then 2). The same pass moved the PB3355
summary back onto `NameTargetRefusal` (R1-3).
