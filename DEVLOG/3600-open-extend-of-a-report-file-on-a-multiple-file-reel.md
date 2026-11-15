## Entry 3600 — 2026-11-14 05:57 PST — OPEN EXTEND of a report file on a multiple file reel compiles at --std 85 (PB5479)

PB5479 (wave 20262029, group E). Re-probe on this build: the note's p1a program (a report file alone on the reel, `MULTIPLE FILE TAPE CONTAINS RPT-FILE POSITION 1`, then `OPEN EXTEND RPT-FILE`) was refused COBOL6814 at `--std 85`, as the note said.

Cause: `StatementValidation.CheckOpenModeForFile` applied the Sequential I-O OPEN's SR2 (§1985:VII.4.3.3 SR2, "The EXTEND phrase must not be specified for a multiple file reel") to every file with `MultipleFileReels`, report files included. A report file's OPEN is the Report Writer's (§1985:XIII.4.5.3 SR1 admits OUTPUT or EXTEND), which does not restate that rule. Fix: the EXTEND refusal skips `file.IsReportFile`; GR20 d (COBOL6816, each file of the reel must be sequential) stays, because the Report Writer OPEN restates it. Sibling sweep: the only other reader of `MultipleFileReels` is `SortBinder.ScreenMultipleFileReels` (MERGE SR6), which is not an OPEN rule and does not take report files.

Golden: `conformance-suite/cases/XIII.2.4@1985-I-O-CONTROL-PARAGRAPH/pb5479_extend_report_file_on_multiple_file_reel_85` runs at 85 and displays status 00 after OUTPUT, EXTEND and CLOSE. The sequential-file refusal stays pinned by `1985-SR-VII_4_3_3-2` and `pb5086-open-extend-multiple-file-reel.json`. No refused-below-edition program is added: below 85 the MULTIPLE FILE clause is itself removed (COBOL0902), which its own goldens cover.

Row `1985-GR-XIII.2.4.4-1` re-verdicted PARTIAL to CONFORMS (GAP 207 to 206).

