## Nine 1985 questions ruled, and the defects they found fixed: LINE-COUNTER after LINE PLUS 0, BY REFERENCE, ADVANCING ZERO held, citations, the LINAGE externality rule

A sweep of the work register for open notes about `--std 85` found nine questions nobody had ruled: analyses and adjudications
filed by the 1985 code-read passes. Three adjudicators took three each against main at 8244f1aa4b, wrote one ruling per note,
and checked every citation. I read the text behind every ruling, re-ran a sample of the probes, and sent the one ruling that
reversed a landed note to an independent refuter.

- **Defects.** PB5853 is a wrong answer: in a report with no PAGE clause, a first group beginning `LINE PLUS 0` presents on
  line 0 by §1985:XIII.3.10.8 rule 4d, and the report writer clamps it to line 1, so LINE-COUNTER reads one too high.
  PB5901: `PROCEDURE DIVISION USING BY REFERENCE` compiles at 85, where the 1985 header has no BY phrase. PB5902: `WRITE ...
  ADVANCING ZERO LINES` compiles at 85, where a figurative constant may stand only where the format says 'literal' and
  integer must be a numeric literal; the refuter upheld it, and it reverses the 85 half of PB5289. PB5933: the refusal of
  DELETE on a sequential file is right but cites a 2023 rule. PB5982, new: a LINAGE data-name of an EXTERNAL file need not be
  external at 85, though §1985:VII.3.7.4 GR11c says it must, and the check ran only at 2023.
- **Not defects.** PB5897 (an out-of-domain literal argument is legal source; the result is undefined only when evaluated),
  PB5896 (the page heading's upper limit is a general rule with undefined results), and PB5164 (both recorded owner
  determinations cover their 1985 rows; D-MEDIUM-85 now names the second row).
- **Two determinations.** PB5817 asked whether an index-name may stand in another dimension's subscript position. The
  adjudicator ruled it a defect on a positional reading of SR3. I overruled it: SR3 is satisfied as written, and §1985:IV.4.3.8.2.4
  GR2 makes what an index's value means the implementor's ("This correspondence is defined by the implementor"), so CLAUDE.md
  rule 1's precedence applies and GnuCOBOL accepts it: D-85-INDEX-IN-ANOTHER-DIMENSION. PB5912 asked whether 85 must compare the
  descriptions of one external file across separately compiled programs. It need not: the rules state no consequence, so a
  violation is undefined, and D-85-EXTERNAL-FD-AGREEMENT records the limit. PB5983 keeps the one question that limit must not
  hide, whether mismatched record lengths can crash the run time.
- PB5981, new: the report heading's lower limit at 85 is borrowed from 2023 and cited to a 1985 rule that states another.

### The 1985 rulings, group A: LINE PLUS 0 on line 0, USING BY REFERENCE refused at 85, COBOL0865's 1985 ground, the report heading's 1985 limit

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
  accepted at the 1985 limit and one refused past it. The first gate found the 2023 witness
  `pb1270-report-heading-after-page-heading` still listing 85 among its refusals. It is now refused from 2002 only.
- **PB5983 (retired).** Two separately compiled programs at 85 that describe one EXTERNAL record and one EXTERNAL file
  record at 10 and 100 characters run to a normal end. The shared storage keeps the first description's length.
  D-85-EXTERNAL-FD-AGREEMENT records this.
- **PB5902 (held).** `WRITE ... ADVANCING ZERO` still compiles at 85. The NIST CCVS-85 program SQ101M writes `BEFORE
  ADVANCING ZERO` (WRT-TEST-GF-25), and the NIST golden run compiles at 85, so the ruling's refusal would turn the NIST
  guard red. The addendum `kb/Work/PB5902/2026-11-20-nist-sq101m-conflict.md` returns it to the ruling with the withdrawn
  fix shape.

Inventory: witnesses added to six 1985 rows and two 2023 rows through `record_verdicts` batches. The GAP is unchanged
(1985 0, 2023 4).

### The LINAGE data-name of an EXTERNAL file must be external at --std 85 (PB5982)

PB5982: at `--std 85`, `FD PFILE IS EXTERNAL LINAGE IS LN LINES` with LN a plain WORKING-STORAGE item compiled, and so did two programs
naming different plain items, though §1985:VII.3.7.4 GR11c requires "The same corresponding external data items referenced by
data-name-1, data-name-2, data-name-3, and data-name-4" in every description of the connector. The PADDING CHARACTER twin was already
refused at 85 (§1985:VII.2.7.4 GR6). `ExternalFileConnectorRules.Check` gated the LINAGE screen on 2023 only, on the premise that the rule
was new there.

Re-probe: both shapes exited 0 at 85 on this branch's build before the change. Fix: the screen runs at 85 as an error citing GR11c
(the PADDING screen's severity), through the same `NotExternal` arm; at 2023 it is unchanged. After: both shapes exit 65 with COBOL1624
naming GR11c; the PB5638 control (`pb5638-external-linage-agree`, external item) still exits 0.

Goldens: `pb5982-external-linage-not-external` and `pb5982-external-linage-two-programs-not-external`, each refused COBOL1624 at 85. The
2023 witness's comment, which said the rule did not exist below 2023, now says it does.

The 2002 and 2014 texts carry the rule too (§2002:13.3.4.3 GR2c2, §2014:13.4.4.3 GR2c2); neither is screened (2002 frozen, 2014 waits),
recorded in the PB5982 addendum. Sibling sweep: the externality arms of `Check` are RELATIVE KEY (every edition), PADDING (85 and 2002)
and FILE STATUS (2023, a clause 1985 does not carry); LINAGE was the one arm missing at 85. No inventory row was claimed.

### PB5902 decided

The implementer held the refusal of `WRITE ... ADVANCING ZERO` at 85 because CCVS-85 program SQ101M exercises it on purpose. The text admits two readings (a figurative constant where the format prints `literal`, against `integer` defined as a numeric literal that SR5 lets be zero), so CLAUDE.md rule 1 sends it to GnuCOBOL, which accepts it: the acceptance stays, recorded as D-85-ADVANCING-ZERO, and PB5902 is retired. The adjudicator and its refuter had read the rule narrowly without the CCVS evidence.
