## PB5605, PB5762: a Tier-B window's subscript is range-tested and ODO-tested like every table reference

Two notes, one root: `PlaceBuilder.WindowScopeOf`. A data item in a REDEFINES class (every record after the first under an
FD, every CD record, a table whose element address is taken for a BY REFERENCE argument) is reached through a byte window
onto the class's one backing. The window's start is arithmetic, `ClassOffset + sum((index - 1) * stride)`, so it never went
through `CobolTable.At`, the one place a table reference tests its subscript (§2023:8.4.2.3.4 GR2, EC-BOUND-SUBSCRIPT) and
the OCCURS DEPENDING data-name (§2023:13.18.38.4 GR7, EC-BOUND-ODO).

**Re-probe (Debug build of main 102f152cc7).** PB5605's CD program printed `1 V` and `NOT TERMINATED`; the FD second-record
form printed `1  ` and `NOT TERMINATED` at 2002 and 2023. PB5762's c4 program printed `M1 END`, `S RAN [    ]`, `M2 END`
at both editions, the callee running on four bytes past the table.

**Fix.** `OffsetTerm` gains an optional `Bound` (`OccurrenceBound(Highest, OdoReferenceCheck?)`). `WindowScopeOf` and
`FixedLevelTerm` set it on every fixed level of the window (the byte, bit and cell-ordinal terms alike, and the ADDRESS OF
displacement), taking the ODO check from the same `OdoReferenceCheckFor` that `BuildAccessPath` gives a `FixedTableSegment`.
`PositionRenderer` renders a bounded term's index through the new `CobolTable.Subscript(occurrence, highest[, depending,
min, max])`, which calls the same `Permissible`, `SubscriptOutOfRange` and `TestOdoBounds` the `At` accessor calls, so there
is one test and one raise. With checking off the reference continues as before. The window's offset is hoisted whole by the
statement-start identification, so the tests run where the table accessor's would.

**Siblings swept.** `PlaceCursor` builds its own `OffsetTerm`s from enumeration locals that range over the current
occurrences (in range by construction) and keeps no bound. The explicit REDEFINES of an ODO group and the `BuildTablePath`
cell arm go through `WindowScopeOf`. No other producer of a window offset exists (`grep "new OffsetTerm\|FixedLevelTerm"`).

**Goldens.** `pb5605-fd-second-record-odo-bound-2002` (13.16.36@2002-OCCURS-CLAUSE) and `-2023` (13.18.38-OCCURS):
three legs, two raised EC-BOUND-ODO and one in range. `pb5762-call-by-reference-subscript-bound-2002` and `-2023`
(14.8.4@2002-CALL-STATEMENT, 14.9.4-CALL): MOVE and CALL at subscript 4 of 3 raise and the callee is not called, and the
in-range CALL runs it. Refused at 85 (`>>TURN`, COBOL0900): `pb5605-fd-second-record-odo-bound-below-2002`,
`pb5762-call-by-reference-subscript-below-2002`. Rows re-verdicted CONFORMS: `2002-GR-13.16.36.3-7`, `2002-GR-14.8.4.3-3`
(GAP 1159 to 1157).

**Self-review.** Architecture: the checks ride the bound-tree term as data and the backend renders them; no C# text in the
model. Duplication: no second copy of the range or ODO test. Performance: one extra call per subscripted window reference,
in the same position the table accessor's call sits for a canonical reference.
