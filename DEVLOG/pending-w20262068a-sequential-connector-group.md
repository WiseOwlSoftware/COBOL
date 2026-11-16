## Wave 20262068 group A: a print file is not read back, and seven COBOL-85 rules the compiler read through a later edition

This branch continues `worktree-wf_f60b9cb3-27d-1` (wave 20262045, entry `pending-pb5631-diagnostic-catalog-group-c.md`),
merged onto main acffeb97ab. The merge had nine conflicts: `VersionConformancePass.GateReleaseReturnRange` took the branch's
`SortMergeProcedures` walk and main's refused-SORT set (PB5496) as its `Refused` member; two case headers kept main's fixed
form with the branch's added 2002 rules; PB5641 and PB5642 stayed open on rows main had added after the branch's base.

**PB5815 (MAJOR, wrong answer, every edition).** A record sequential file with a LINAGE clause or named by a WRITE ...
ADVANCING carries its print controls between its records, so OPEN INPUT gave 00 and READ returned records never placed
(`prt.cob`: `A1 00 [LF LF A]`, no AT END) and REWRITE overwrote a line end. Of the two choices the note left, the OPEN is
refused: such a file "will not support read operations", so OPEN INPUT and OPEN I-O are '37' (§2023:9.1.13.6 item 6 a) 3.,
§2023:14.9.27.4 GR16; §1985:VII.1.3.5), asked once above the organizations (`FileConnector.SupportsReadOperations`, answered by
`SequentialConnector.IsRecordSequentialPrintFile`). DOC-A.1-146 (c') and FILES_DESIGN record it. Golden
`VII.4.3@1985-OPEN-STATEMENT/pb5815_print_file_not_read_back_85` (37, READ 47, REWRITE 49, EXTEND 00).

**PB5766, decided.** A separately compiled EXTERNAL describer's ADVANCING reaches the shared connector at its activation:
nothing the run unit holds at the OPEN names a program found at its CALL. The determination WRITE-WITHOUT-ADVANCING.md now
states the arm; two-unit witness `wiseowl/cases/VII.4.7@1985-WRITE-STATEMENT/pb5766_external_advancing_separate_unit`.

**PB5780 (every edition).** `OperandText.IsStringVisitor` read a reference-modified numeric item's inner category, so
`N3 (1:3) = 5` compared values; it now asks `RefModPlace.Category` (§1985:IV.4.3.8.4.4 GR5, §2023:8.4.3.3.4 GR6a).

**PB5771.** BLANK WHEN ZERO's all-spaces-is-zero relation rule is §2023:13.18.8.4 GR3 alone (2002, 2014 and 1985 print two
general rules); below 2023 a blanked item compares as characters. `pb509_blank_when_zero_relation` is now `accept: [2023]`.

**PB5814 and PB5767.** ADD and SUBTRACT CORRESPONDING identify their group operands once at 85 too (the amended
§1985:IV.4.3.8 sentence; D-CORR85 corrected, its two per-pair cases re-derived as `pb5814_*_once_85`), and an 85 MOVE
CORRESPONDING re-runs its identification steps before each pair (`BoundCorresponding.PairIdentification`), so a subscript in a
redefined record is read afresh (§1985:VI.6.19.4 GR1, GR2).

**PB5798, PB5784, PB5768.** IF with END-IF and NEXT SENTENCE is refused at 85 (§1985:VI.6.16.3 SR3, new code COBOL7486);
`NOT NOT < C` parses at 85 as the logical NOT of `NOT < C` (§1985:VI.6.3.3 GRL2.1, GRL2.2); a declarative RELEASE or RETURN
is in the 85 range only where the range holds an I-O statement on a file its USE names, or a CALL (§1985:XI.4.4.4 GR7, GR10).

Rows: 1985 GAP 37 to 32 (1985-GR-VII.4.7.4-16, 1985-GR-IV.4.3.8.4.4-5, 1985-GR-VI.6.19.4-1, 1985-SR-XI.4.2.3-2,
1985-SR-XI.4.3.3-3), witnesses added to eleven more. Not done here: PB5804 and PB5795 (the computer-name slot), handed on.
