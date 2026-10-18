## PB1917: the MERGE USING arm of the short-record fill gets its golden (a witness, not a fix)

kb/Work PB1917 (wave 1126, group H, SORT and file I/O campaign PB3353). PB1140 routes the short-record fill of both SORT and
MERGE USING transfers through one emitter arm (`SortEmitter.EmitInputFile`, the fixed-SD call of `CobolSort.FillTo` with
`FileModel.ShortRecordFillNational`), but its golden's only MERGE leg used a VARYING SD, so the fill of a MERGE into a
FIXED-length merge file (§14.9.24.4 GR2) ran in no test.

**Re-probe.** On the w1126h build (base 6e2244f3d) a program with fixed SDs already answers correctly: a USING file whose one
record is N(2), and one whose one record is `PIC 9(2) USAGE NATIONAL`, fill their 4 bytes to the SD's 6 with 00 20 (GR2 a);
an X(4) record fills with 20 20 and so does a USING file of two record descriptions, N(2) and X(4), because the national
record is then not the only description (GR2 c); an OUTPUT PROCEDURE RETURNs the already-filled record. So the note was a
missing witness and nothing in `src` changed.

**Golden.** `conformance-suite/cases/14.9.24-MERGE/pb1917_merge_fixed_sd_short_record_fill` (accept 2002, free form), with the
expected bytes derived from GR2 a) and c) and the national collating order, not from a run. It also covers the exception path of
the same statement (campaign requirement, MANDATORY-PRACTICES I11): `MERGE ... USING UD1 MISS GIVING GX`, where MISS is a
non-optional file that is not there, ends at MISS's implicit OPEN with status 35 (§9.1.13.6 item 5, GR7 a), the program's FILE
STATUS clause lets the run unit continue (DOC-A.1-103), and the GIVING file was never opened (its FILE STATUS still spaces; an
OPEN INPUT of it afterwards is 35). EC-SORT-MERGE-SEQUENCE and -FILE-OPEN were already covered by
`SortMergeStatementEcTests`, and the missing-USING status with a FILE STATUS item or a USE procedure by pb2700. No negative: the test adds
no syntax and no edition gate.

**Trap recorded.** `cobol <file> --run` writes its `.dll`, `.g.cs` and runtimeconfig beside the SOURCE; inside
`conformance-suite/` that makes `generate.py --check` refuse the folder. Compile a copy outside the tree.

Rows: PB1917 claimed none and closes none (`closes_rows_reason` in the note). Self-review: nothing found (no source change).
