**2026-11-15, two leads from the PB5578 review (round 3, low, not blocking).** Source: E:/COBOL-coord/scratch/reports/w20262043b-PB5578-report.md. The round-1
leads (FORMAT with ODO / no record entries refused COBOL7344; the MERGE/SORT short-record fill witness) are already recorded above in this note's landing file
under "Leads (still open under this note)", so they are not refiled.

**R3-1 (doc).** `docs/VERSION_CHANGE_REFERENCE.md:486`, VCR row 7.78 (new in 2f73ede6bf), names as its witnesses
`13.18.51-SELECT-WHEN-clause/select_when_write_no_record_selected_status_45` (2002), which the same commit deleted, and `select_when_record_name_write_names_its_entry`
(2014, 2023), which does not exist. The actual witnesses (checked in this filing, both files exist) are
`13.18.51-SELECT-WHEN-clause/select_when_record_name_write_by_edition` (with its `.2002.out`) and `13.18.13-CODE-SET/select_when_code_set_rewrite_record_name_by_edition`.
The row's witness text is the one `DialectBehavior.SelectWhenOnlyForFilePhraseStatements` is documented by (marker `behavior:select-when-only-for-file-phrase-statements`),
so the fix edits the row and runs the doc-facts / VCR drift checks.

**R3-2 (golden asserts undefined content).** `conformance-suite/cases/13.18.51-SELECT-WHEN-clause/select_when_record_name_write_by_edition.cob:51`: the 2014/2023
output pins the record area after an unsuccessful READ (`READ 1 45 Z` then displays G-B-KEY), whose content `§2023:14.9.30.4 GR18` makes undefined
(`cite.py --check "§2023:14.9.30.4 GR18" "the content of the associated record area is undefined"` OK: "at the completion of any unsuccessful execution of a READ
statement, the content of the associated record area is undefined"). The fix stops the golden DISPLAYing the record area after the status-45 READ (display
the status only) and regenerates both `.out` files.

**Sites:** `docs/VERSION_CHANGE_REFERENCE.md` (row 7.78); `conformance-suite/cases/13.18.51-SELECT-WHEN-clause/select_when_record_name_write_by_edition.cob` and its `.out` / `.2002.out` (no code file).
