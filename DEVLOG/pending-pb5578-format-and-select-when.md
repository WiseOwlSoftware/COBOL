## PB5578: the FORMAT and SELECT WHEN clauses are implemented (Annex A.4.8 claimed), 28 of 33 2002 rows close

The 2026-11-15 reads showed `FD F FORMAT CHARACTER DATA` refused with COBOL1705 at `--std 2002`, where ISO/IEC 1989:2002 requires both clauses
(§2002:3.1 makes only the communication facility optional). The re-probe on the branch build reproduced it before any change. The root was the A.4.8 decision
itself, so the fix is the facility, not a relaxation of the refusal.

**Mechanism.** One runtime engine, `RecordFormat`, translates a record between the record area and the medium item by item over the SELECTED record description entry:
a numeric item is its sign and its unscaled digits (GR8), a float a fixed-width exponent form, a bit item one character per bit, an alphanumeric item itself or its CODE-SET
alphabet's code, and anything no kind selects crosses as it is (GR7). It is called at exactly the two places a record crosses the medium: `SequentialConnector.ExternalRecord`
at the top of each write entry (so the RECORD clause's bounds, status 44, the line character set and the framing are about the record on the medium) and `InternalRecord` at the one read.
SORT and MERGE USING and GIVING reach it through the connector. No selected entry is I-O status 45. The compiler lays each record description out as steps (`RecordPlans.Of`), registers the
plans, the FORMAT kinds, the alphabet and the SELECT WHEN selector (the clauses' level-88 conditions compiled in order, so no condition is re-implemented in the runtime), and
`ResolveRecordFormatPass` screens the syntax rules (COBOL7342 FORMAT, COBOL7343 SELECT WHEN, COBOL7344 layouts without a plan). CODE-SET with SELECT WHEN converts item by item (SR3
lifts). OPEN I-O of a FORMAT file, the FROM identifier's description and an external connector's FORMAT agreement are screened (COBOL7342, COBOL6847). The clauses are gated at COBOL-85
(`format-clause-2002`, `select-when-clause-2002`, COBOL0900). COBOL1705 is retired.

**Probes before the tests.** Writing and reading back `FORMAT CHARACTER NUMERIC` files with signed display, binary, packed and float items, bit runs, three SELECT WHEN entries with OTHER, status 45
on an unmatched record, EBCDIC bytes (`C1 60 F1 F2` for `A` and `-12`), SORT USING/GIVING over FORMAT files, a pad written in the medium's space (`40`, not `20`), and a bit record whose characters
after the bits were placed by the byte walk (fixed: the bit walk places every item of such a record). A bug found by the probes: the line sequential character set was asked of the
translated record area, so a packed item read from a FORMAT line file drew status 09; it is asked of the line on the medium.

**Tests.** 5 suite tests at 2002/2014/2023 plus their 2002 twins, 5 more 2002 witnesses and WiseOwl determinations (representation, FORMAT-then-alphabet order, no-effect without CODE-SET or FORMAT, status 44,
OPEN rules), 16 refusals (every syntax rule, OPEN I-O, FROM identifier, layouts, external agreement, below 2002), 8 unit tests of the engine. The five a48 witnesses of the declined module are deleted.

**Rows.** 28 of the note's 33 2002 rows close (`record_verdicts --edition 2002`); 36 2023 rows are re-verdicted (CONFORMS, and DOCUMENTED-NON-SUPPORT for GR-13.18.24.4-2 and -3, whose sub-cases are decided
in kb/Work/PB5578/2026-11-15-landing.md); A.1 items 84, 85 and 173 gain their §7 rows. Open under the note: `2002-GR-13.16.22.3-3` (OCCURS DEPENDING ON in a translated record), and the four SORT/MERGE
short-record fill rows (implemented, no witness yet). `work.py`'s `RULE_ID_SHAPE` now accepts an older edition's year-prefixed row id in `closes_rows` (it refused every 2002 row).

**Self-review.** Architecture: one engine, one registration, no C# in a bound node. Code: the cited rules are the ones implemented (each citation checked with cite.py); sibling arms: the write
entries (plain, advancing) share `ExternalRecord`; REWRITE is unreachable (SR9) and translates nothing. Performance: the translation allocates one string per record and only for a file with a format.
Duplication: the fill rule keeps one test (`FileModel.ShortRecordFillNational`) and one runtime sibling for the selected case.

**Review round 1 (three blocking findings).** R1-1: a REWRITE of a CODE-SET + SELECT WHEN file (no FORMAT, so I-O is legal) wrote the record untranslated while READ converted it, leaving a mixed-code file; `SequentialConnector.RewriteRecord` now translates through `ExternalRecord` as WRITE does (golden `select_when_code_set_rewrite`: medium CCSID 37 codes read through a plain FD, then a round trip). R1-2: `BindCodeSetClause` still refused alphabet-name-1 with alphabet-name-2 for any FD with records, ignoring SR3's 'and no SELECT WHEN clauses'; the screen moved to `CheckCodeSetRecordItemsPass` (the entries' SELECT WHEN clauses are bound by then; `FileModel.CodeSetBothAlphabets` travels) and the now-unused `records` parameter left the binder (golden `select_when_code_set_both_alphabets`, refusal `code_set_both_alphabets_without_select_when` COBOL1672). R1-3: statement-level abnormal paths now have goldens: WRITE status 45 (`select_when_write_no_record_selected_status_45`), READ status 30 (`format_read_external_text_not_a_value_status_30`), WRITE 91 and 71 through CODE-SET (`format_code_set_character_without_image_statuses_91_71`).
