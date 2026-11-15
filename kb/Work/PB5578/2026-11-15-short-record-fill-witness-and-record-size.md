**2026-11-15, wave 20262045 group A: the short-record fill by SELECT WHEN is witnessed and the RECORD size row closes; one row stays open.**

Re-probe at 2002: a SORT USING a file with several record descriptions and SELECT WHEN clauses fills a national record with
national spaces only when the file carries CODE-SET or FORMAT (probe `E:/COBOL-coord/scratch/w20262045a/p78/sw2.cob`: 000 078 000 032 000 032;
`sw1.cob`, the same file without CODE-SET: 000 078 000 032 032 032). That is the recorded determination D-SMF reading a)
(docs/conformance/3/D-SMF.md): §2002:13.16.49.3 GR1 evaluates the clauses only "when a CODE-SET clause or FORMAT clause is specified
for the file", so the no-clause file takes arm c). No code defect.

Witnesses (values from the 2002 text): `cases/14.8.36@2002-SORT-STATEMENT/pb5578_sort_merge_short_record_select_when_fill` (SORT USING and
GIVING, MERGE USING and GIVING, each arm b); `cases/13.16.41@2002-RECORD-CLAUSE/pb5578_record_size_variable_occurrence_maximum` with its
refusal `pb5578-record-size-variable-occurrence-over` (COBOL2010: an OCCURS 1 TO 4 table counts at its maximum, §2002:13.16.41.3 GR3,
§2002:13.16.41.2 SR2); `pb1277_record_size_in_bytes` (already accepted at 2002) now names 2002-GR-13.16.41.3-1 and -4. Batch
`E:/COBOL-coord/scratch/w20262045a/batch-pb5578.json`: 7 rows CONFORMS, GAP 2479 to 2472.

Still open: `2002-GR-13.16.22.3-3`, FORMAT of a record holding a variable-occurrence or dynamic table (decision 1 of the landing,
COBOL7344). Making it work is a redesign of `RecordPlans.Of` and `RecordFormat`: the plan is a list of fixed-offset steps, and an
OCCURS DEPENDING ON table needs a step group repeated by a count that the WRITE takes from the DEPENDING item (possibly outside the
record) and the READ derives from the external record, whose item lengths differ by FORMAT kind. Not done here.
