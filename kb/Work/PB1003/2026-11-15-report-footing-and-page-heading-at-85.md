**2026-11-15, a 1985 drift of this note's two placement computations: the report footing after the page footing and the page heading after the report heading (the Mythos lane's batch-2 code reads, mechanism 6; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-2.md`).**

**Ruling: UPHELD (a), harm under_rejects and wrong_answer.** The rows are `1985-GR-XIII.3.10.10-2` and `1985-GR-XIII.3.10.7-2`. The report footing prints on line 13 of a 12-line page, and the page heading's last line lands on FIRST DETAIL under the detail line (`§1985:XIII.3.8.4 GR1c`). The 2023 row `GR-13.18.57.4-8` was not read; whether it is wrong too is not decided.

**Mechanism.** 1985 has no PRESENT WHEN, so the page footing always precedes the report footing on its page and the report heading always precedes the page heading on page 1; their placement is fixed by the description. RF: PF LINE 10 (final LINE-COUNTER 10, XIII.3.10.9 GR5c), RF LINE PLUS 3 gives line 13 by XIII.3.10.10 GR3b, past GR2's lower limit (PAGE LIMIT 12), so XIII.3.15.3 SR1 makes the description an error; it compiles and prints RF on line 13 of a 12-line page. PH: RH lines 2 and 3, PH LINE PLUS 1, PLUS 1: the compiler itself places the PH after the RH (lines 4 and 5) and line 5 is FIRST DETAIL, which XIII.3.8.4 GR1c forbids for a page heading; the detail overprints it (L5 `    PHB DE`). The PH finding does not rest on the reconstructed XIII.3.10.7: under the reconstruction (GR2 lower limit, GR3b placement) the description must be refused, and under any other placement the compiler's own output breaks GR1c. `ScreenReportGroupLines` calls `MinimumLastLine` with no seed from the preceding group's final LINE-COUNTER, while the run-time placement seeds from LINE-COUNTER: the compile-time reach screen seeds only from FOOTING/HEADING. This is the drift this note's structural cause (the placement arithmetic written twice, compile time and run time) produces. At 2023 an over-long group is a run-time EC-REPORT-PAGE-LIMIT matter (`§2023:13.18.35.4 GR2`) and PRESENT WHEN makes the preceding group conditional, so the lane rules cause (a); the reader proposed (b) for the RF row, and the 2023 row GR-13.18.57.4-8 was not adjudicated. Related notes (landed): PB5244 and PB1270.

**Where (the checker's reading, a lead, not a site list of this note):** ReportSectionBinder `ScreenReportGroupLines` (`MinimumLastLine(g, model)` with no seed) and `GroupLimits` (edition-blind PH arm); the run-time placement, ReportWriter `FirstLineTarget`, seeds from LINE-COUNTER.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:XIII.3.10.10 GR2`: "The last line number on which the report footing report group can be presented is the line number specified by integer-1 of the PAGE clause"
  `OK  §1985:XIII.3.10.10 GR2  the text is in GR2`
- `§1985:XIII.3.10.10 GR3b`: "the sum of the final LINE-COUNTER setting established by the page footing report group and the integer of the first LINE NUMBER clause"
  `OK  §1985:XIII.3.10.10 GR3b  the text is in GR3b`
- `§1985:XIII.3.15.3 SR1`: "Neither integer-1 nor integer-2 may be specified in such a way as to cause any line of a report group to be presented outside the vertical subdivision of the page"
  `OK  §1985:XIII.3.15.3 SR1  the text is in SR1`
- `§1985:XIII.3.8.4 GR1c`: "Report heading (without NEXT GROUP NEXT PAGE) and page heading report groups may not be presented on or beyond the line number specified by integer-3"
  `OK  §1985:XIII.3.8.4 GR1c  the text is in GR1c`
- `§1985:XIII.3.8.3 SR8b`: "A page heading report group, if defined, must be defined such that it can be presented in the vertical subdivision of the page"
  `OK  §1985:XIII.3.8.3 SR8b  the text is in SR8b`
- `§1985:XIII.3.10.7 GR1` (reconstructed text, cited only as corroboration; kb/Work PB4952):
  `OK  §1985:XIII.3.10.7 GR1  the text is in GR1 [RECONSTRUCTED: editorial text, not the standard's; kb/Work PB4952]`

**Repro 1** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b2/src/rfpf.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RFPF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "RFPF.OUT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 4
           LAST DETAIL 8 FOOTING 9.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DE".
       01 PFG TYPE PF LINE 10.
          02 COLUMN 1 PIC X(2) VALUE "PF".
       01 RFG TYPE RF LINE PLUS 3.
          02 COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
```

**Repro 2** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b2/src/phrel.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PHREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PHREL.OUT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 5
           LAST DETAIL 9 FOOTING 9.
       01 RHG TYPE RH.
          02 LINE 2.
             03 COLUMN 1 PIC X(3) VALUE "RHA".
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "RHB".
       01 PHG TYPE PH.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "PHA".
          02 LINE PLUS 1.
             03 COLUMN 5 PIC X(3) VALUE "PHB".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 9 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
```

**Expected at `--std 85`:** both descriptions refused (`§1985:XIII.3.15.3 SR1` with `XIII.3.10.10 GR2`/`GR3b`, and `XIII.3.8.4 GR1c`).
**Observed at `--std 85`** (exit 0): `rfpf.cob`: DE line 4, PF line 10, RF line 13 (PAGE LIMIT 12); `phrel.cob`: L2 RHA, L3 RHB, L4 PHA, L5 `    PHB DE` (FIRST DETAIL 5).
**At `--std 2023`** (exit 0): the same output for both.
