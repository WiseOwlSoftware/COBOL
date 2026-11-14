- **D-DEBUG-WHOLE-TABLE — a statement that names a whole table** (kb/Work PB4855). SEARCH's identifier-1 (the 1985 text's Table Handling
  module: a reference to a table element indicates its occurrence "except in a USE FOR DEBUGGING statement and SEARCH statement"), a
  Format-2 table SORT's data-name-2 and each key data-name it writes, and INITIALIZE of a whole dynamic-capacity table name a table, or an
  item under its element, without the subscripts of the level they range over. Each is an operand the statement explicitly references
  (USE FOR DEBUGGING general rule 5); SORT and INITIALIZE change its content (rule 6 c), SEARCH does not. **Determinations:** (1) the data
  item such a reference denotes is every CURRENT occurrence of the item under the enclosing occurrence the statement names (an OCCURS
  DEPENDING table's data-name-1, a dynamic-capacity table's current capacity), so DEBUG-CONTENTS (rule 24 c) is their storage images in
  storage order, and the empty image (spaces) at a current count of zero. (2) The enclosing occurrence is the one the statement writes:
  the SORT subject's subscripts, SEARCH identifier-1's, else the first reference to the table in SEARCH's WHEN phrases (ISO/IEC 1989:2023
  §2023:14.9.37.4 GR1 "The subscript that is used to determine the occurrence of each superordinate table to search is specified by the user
  in the WHEN phrases"); DEBUG-SUB-n is that occurrence (rule 14) and spaces for the levels the reference ranges over (rule 11); a nested
  table whose statement names no enclosing occurrence is referenced in every occurrence of the tables around it. (3) Rule 7 runs the
  section once for the operand, so SEARCH's identifier-1 and a WHEN phrase's reference to the same table are one run, after the statement,
  with identifier-1's content. (4) DEBUG-CONTENTS is as wide as every occurrence of each target item at its maximum (rule 15: "large enough
  to contain the data required"; `RecordLayout.AllOccurrencesImageWidth`); a dynamic-capacity table has no maximum (§2023:8.5.1.9.1 2), so
  it counts its expected capacity (TO integer-5), else its initial capacity (FROM integer-4), at least one occurrence, and an image past the
  register's width is truncated by rule 11's alphanumeric move. Witnesses: `conformance:cases/_unassigned/pb4855-debug-whole-table-search`,
  `conformance-test:DebugFacilityTests.TableSortOperands_AreWholeTableReferences`,
  `conformance-test:DebugFacilityTests.InitializeWholeDynamicTable_IsOneWholeTableReference`.
