## The 1985 rulings, group A: LINE PLUS 0 on line 0, USING BY REFERENCE refused at 85, COBOL0865's 1985 ground, the report heading's 1985 limit

This branch carries the rulings commit for the nine 1985 questions and fixes four of the six notes it filed or turned into
defects. One is retired after a probe. One is held, because the evidence the ruling did not weigh would turn the NIST
guard red.

- **PB5853 (wrong answer).** In a report with no PAGE clause, a first group beginning `LINE PLUS 0` set LINE-COUNTER to 3,
  where §1985:XIII.3.10.8 GR4d and GR6d give 2 (the probe `lc0.cob` printed LC 0/3/5). `ReportWriter.PresentLine`
  clamped every computed line below 1 to 1, because `_physLine == 0` also meant "nothing printed on this page".
  The empty page is now `_physLine == null`, and the clamp is gone. Lines 0 and 1 rest on the page's first physical line,
  and every later line travels by its difference from the line before it. The report file is byte-identical, and only
  LINE-COUNTER changes: LC 0/2/4. The 2002, 2014 and 2023 texts state the same arithmetic (§2023:13.18.35.4 GR5c), so
  every edition changes. In a paged report a computed line 0 is above HEADING and stays refused (COBOL2199). Goldens:
  `pb5853_unpaged_first_line_zero_85` and `pb5853_unpaged_report_heading_line_zero_85` at 85, and
  `pb5853_unpaged_first_line_zero` at 2002, 2014 and 2023, each pinning its output file. REPORT_WRITER_DESIGN section 1
  is updated.
- **PB5901 (under-rejects).** `PROCEDURE DIVISION USING BY REFERENCE L1` compiled at 85, where the 1985 header is `USING
  {data-name-1} ...` (§1985:X.5.1). A new construct row, `pd-header-by-reference-2002` (COBOL0900), twin of the BY VALUE
  row, is checked by `VisitUsingByReference` on recognition. Goldens: two refusals at 85 (with BY, and the bare
  `REFERENCE`) and an acceptance at 2002.
- **PB5933 (misleading diagnosis).** COBOL0865 cited §2023:14.9.10.3 SR1 at 85. The 1985 ground is that the sequential
  I-O module has no DELETE statement (§1985:VII.1.2), and DELETE belongs to the relative and indexed modules. `No1985Rule`
  is dropped, so `EditionCitationNet` now requires the 1985 citation. Golden `pb5933-delete-sequential-file-85`.
- **PB5981 (misleading diagnosis, rejects legal source).** At 85 a report heading's lower limit was the 2002+ shape, the
  page heading's first absolute line minus 1, so RH `LINE 4` under a page heading at `LINE 3` was refused with limit 2
  "per §1985:XIII.3.8.3 SR8". SR8a gives FIRST DETAIL minus 1. `GroupLimits` now asks the edition. Goldens: one program
  accepted at the 1985 limit and one refused past it.
- **PB5983 (retired).** Two separately compiled programs at 85 that describe one EXTERNAL record and one EXTERNAL file
  record at 10 and 100 characters run to a normal end. The shared storage keeps the first description's length.
  D-85-EXTERNAL-FD-AGREEMENT records this.
- **PB5902 (held).** `WRITE ... ADVANCING ZERO` still compiles at 85. The NIST CCVS-85 program SQ101M writes `BEFORE
  ADVANCING ZERO` (WRT-TEST-GF-25), and the NIST golden run compiles at 85, so the ruling's refusal would turn the NIST
  guard red. The addendum `kb/Work/PB5902/2026-11-20-nist-sq101m-conflict.md` returns it to the ruling with the withdrawn
  fix shape.

Inventory: witnesses added to six 1985 rows and two 2023 rows through `record_verdicts` batches. The GAP is unchanged
(1985 0, 2023 4).
