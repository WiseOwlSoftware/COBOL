## Wave 20262139 group A: the two predecessor branches merged, their review findings answered

This branch merges the unlanded w20262135 group-D branch (PB3229) and group-A branch (PB3212, PB3214, PB2624, PB2731) and
answers the five findings their branch reviews raised.

**PB3229 R1-1 (a crash).** The FROM-phrase function-class screen of WRITE, REWRITE and RELEASE read a function result's
category from `MoveTable16.SenderPosition`, a Table 16 position that answers `Group` for an alphanumeric group (the
§2023:14.9.25.4 GR4 group move). PB3229 routed the message through `ItemCategory.CategoryName`, which throws for `Group`,
so `WRITE R FROM FUNCTION GRPF` over a function returning a group crashed; main had printed COBOL1914 "of category group".
Both were wrong: §2023:8.4.3.2.4 GR1 gives the function's temporary item its RETURNING item's class and category, and
§2023:8.5.2.1 makes an alphanumeric group's category alphanumeric, so the function is an alphanumeric function and SR4
admits it. The screen now asks `IntrinsicResultType.OperandCategory`, the one operand-category reader. Golden
`14.9.51-WRITE/pb3229-write-from-group-function` (2014, 2023) writes and reads back `[ABCDE12345]`.

**PB3229, second review round (a strongly-typed result).** That reader answers Alphanumeric for any picture-less group,
the §2023:15.3 AR2 reading of a function argument, so a function returning a strongly-typed group was admitted where main
had refused it. §2023:8.5.2.1 makes such a group's class and category its type-name, so the screen asks
`StrongTypeModel.StrongGroupType` first and refuses with "of category T". Refusal goldens
`14.9.51-WRITE/pb3229-write-from-strong-group-function` (2002, 2014, 2023) and
`14.9.35-REWRITE/pb3229-rewrite-from-strong-group-function` (2014, 2023).

**PB3212 R1-1 (test coverage).** Four tests pin what the external-identity change decides: a RELATIVE KEY in a
non-external FD record (COBOL1624), the external record's key at different positions in two programs (COBOL1575), a READ
of an unwritten relative record through the FD-record key (INVALID KEY, status 23, §2023:14.9.30.4 GR29), and a PADDING
CHARACTER item of an external FD record, which §1985:VII.2.7.3 SR3 still bars from the File Section (COBOL6848): the
review's guess that the change admits it was wrong.

**PB3212 R1-2 and R1-3 (duplication).** `DataItem.IsInFileSectionRecord` reads the one section fact, `Section`, so a
record of a repeated FD answers the same to the TYPE/SAME AS screens and the I-O-CONTROL placement rules. Six readers that
scanned `file.Records` for membership ask `FileModel.HoldsRecord` (the record's own `RecordOfFile`), and the ASSIGN USING
own-record walk asks `RecordLayout.IsInRecordOfFile`.

**Not done.** PB2628, PB2655, PB2637 and PB2747 are each a mechanism of their own and stay open for the planner.

**Gate.** Run 000001-20261122T211818Z at b9c0051055: `BUILD-LOCAL GATE: GREEN — Conformance 29,104/29,104 ran · Unit
42,711/42,711 ran · RepoDrift 231/231 ran · Characterization 37/37 ran`. The Linux gate's unit (42,843) and conformance
(29,104) legs were GREEN; its self-test leg failed `scripts/hooks/test_forbidden_commands.py` on two `git push <url>`
cases that pass on Windows, a tooling defect of the Linux clone untouched by this branch.
