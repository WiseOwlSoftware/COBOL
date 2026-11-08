## PB4855: a statement that names a whole table references it, and DEBUG-CONTENTS is every current occurrence

kb/Work PB4855's last open family, operands that name a table, or an item under its element, without the subscripts of the level they
range over: SEARCH's identifier-1, a Format-2 table SORT's data-name-2 and its key data-names, and INITIALIZE of a whole OCCURS DYNAMIC
table. Each was held as a `DataItem` (or not at all: `BoundSearch` carried only its index), so the debug reach walk never saw it.

**Re-probe (6e542f8204, before the change).** `SEARCH T` at `--std 85` ran the `ALL REFERENCES OF T` section only for the WHEN phrase's
`T (TX)`, with that occurrence's content and DEBUG-SUB-1 2; `SORT T ASCENDING` under `USE FOR DEBUGGING ON T` (`--std 2002 --permissive`)
ran nothing; `INITIALIZE T` of a dynamic table under `USE FOR DEBUGGING ON T` (`--std 2023 --permissive`) ran nothing. The SEARCH leg is a
1985 defect in a strict compile: the 1985 Table Handling module lets SEARCH name a table without subscripts, and rule 5 d runs the section
after "any other COBOL statement that explicitly references identifier-1".

**The fix: no new place kind.** The earlier waves' notes proposed a `WholeTablePlace` with its own image arms in every renderer and pass.
The table(ALL) enumeration already is that place: ISO/IEC 1989:2023 §2023:15.3 states it as "as if each table element associated with that
subscript position were specified", with each level's range the current count (fixed, OCCURS DEPENDING, current capacity) and a nested
dynamic table's capacity read per outer occurrence. One builder, `PlaceBuilder.WholeTable(item, outer)`, makes the `TableAllPlace` whose
fixed subscripts are the enclosing occurrences the statement names and whose ALL levels are the rest, with its own method local
(`StatementLocalRole.WholeTableIndices`, `__whole<uid>`). Each statement holds it as its written operand: `BoundSearch.Table`,
`BoundTableSort.Written`, `BoundTableSortKey.Written` (only a key data-name the statement writes; GR23's element key is data-name-2 and GR21's
keys are the table's) and `BoundInitialize.Written` on the whole-dynamic-table arm, whose element stores thereby become implicit (rule 9).
`BoundReferences` reads them structurally with no change; `BoundStores` classifies the table SORT as a `ReadWrite` of its table and written
keys (§2023:14.9.40.4 GR24 places the sorted elements back), so rule 6 c fires. SEARCH's enclosing occurrence is identifier-1's subscripts,
now read for every nested table, else the WHEN phrases' first reference (§2023:14.9.37.4 GR1), now captured for every nested table.

**The image.** DEBUG-CONTENTS of a whole table is `CobolTable.AllImage`: the occurrences' storage images in storage order, the same
row-major walk `AllArgs` uses (now one private `EnumerateOccurrences`), empty at a count of zero where `AllArgs` raises
EC-ARGUMENT-FUNCTION. The count lambdas are one renderer, `PlaceRenderer.AllCountLambdas`, which the intrinsic argument list now uses too.
DEBUG-SUB-n is the enclosing occurrence and spaces for the levels the place ranges over. Rule 15 sizes DEBUG-CONTENTS by every occurrence of
each target at its maximum (`RecordLayout.AllOccurrencesImageWidth`); a dynamic table, with no maximum, counts TO integer-5, else FROM
integer-4, else one. Determinations: docs/CONFORMANCE.md D-DEBUG-WHOLE-TABLE; design: docs/CONTROL_FLOW_DESIGN.md D20 (c).

**Goldens.** `conformance:cases/_unassigned/pb4855-debug-whole-table-search` (85: SEARCH ALL over an OCCURS DEPENDING table, a SEARCH whose
WHEN never names the table, a nested table whose WHEN names ROW (2), a data-name target SEARCH does not change, a 40-position table read past
position 30); `DebugFacilityTests.TableSortOperands_AreWholeTableReferences` (2002 permissive: `SORT E (I) DESCENDING K` over a nested table,
the written key, the subscript I, a REDEFINES-class table, a member the statement does not write) and
`DebugFacilityTests.InitializeWholeDynamicTable_IsOneWholeTableReference` (2023 permissive, both triggers). No diagnostic code was used;
COBOL6556-COBOL6558 are returned.

**Rows.** None: the debug module has no ISO 2023 rule and no inventory row (VCR Table 7 row 7.17).
