## PB5250 PB5312: the I-O-CONTROL screens read the 1985 text below 2002 (SAME with a report file, SAME RECORD AREA against a sort clause, RERUN on a sort file)

Both notes sit in `FileControlBinder.IoControl.cs`; each was re-probed on the branch build before any edit and each reproduced
(`c3b-samesort.cob` exit 65 COBOL2738; `c_recarea.cob` printed OK; `p17_rerun.cob` printed `P17 RAN`, all at `--std 85`).

**PB5250.** `SameOccurrenceRule` carried the 2023 limits at every edition. §1985:XIII.2.4.3 SR2 lets a report file appear in a
SAME clause without the RECORD phrase, and §1985:XI.2.5.3 SR5b limits only a sort or merge file to one SORT clause, so below 2002
a report file is one SAME AREA, no SAME RECORD AREA, and any number of SORT / SORT-MERGE AREA clauses; §2002:12.3.6.2 SR5 and
§2023:12.4.6.4.3 SR5 bar the sort-merge-area format. The table now returns a `SameLimit` (limit, citation, words) and is asked
the edition on its report row only; the other rows are the same words at every edition. The note's addendum: §1985:XI.2.5.3 SR5c
says "that SAME clause", which in 1985 is `SAME [ RECORD ] AREA` (§1985:VII.2.13.2), so below 2002 the SR10 loop is run over the
record-area clauses as well as the file-area ones (2002+ asks only the file-area format, §2002:12.3.6.2 SR10). The SR9 loop was
split from the SR10 loop it was nested in, and SR10 now asks "a file other than a sort or merge file" literally.

**PB5312.** `IsSequentiallyOrganized` (one predicate for RERUN SR1 and SR2) excludes a sort-merge file: it has no organization,
its file control entry admits only ASSIGN (§1985:XI.2.3.3 SR2). `ON SRTF` and `END OF REEL OF SRTF` are now COBOL6529.

**Goldens.** `pb5250_same_sort_area_report_file_85` (accept 85, prints `PB5250A OK`) and its refused twin
`pb5250-same-sort-area-report-file` (2002-2023); `pb5250-same-record-area-partly-in-sort-area-85` (refused at 85) and its
accepted twin `pb5250_same_record_area_partly_in_sort_area` (2002-2023); `pb5312-rerun-on-sort-file` and
`pb5312-rerun-end-of-reel-of-sort-file` (COBOL6529). `generate.py --check` GREEN. Rows: none closed (the 1985 rule ids wait on
kb/Work PB5094); both notes landed with `closes_rows: []` and a reason. `docs/FILES_DESIGN.md` carries the 1985 arm.

**Sibling sweep.** `FileModel.IsSequential` readers: the OPEN screen for MULTIPLE FILE TAPE members reads it but an SD is never
opened, so no program reaches it; left as is.
