      *>--- conformance-case
      *> id: g85_f29_08
      *> rules: [1985-GR-XIII.4.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.4.4 GR1 - INITIATE performs, for each named
      *> report: "a. All sum counters are set to zero. b. LINE-COUNTER
      *> is set to zero. c. PAGE-COUNTER is set to one."
      *> Each counter is first given a value other than the one
      *> INITIATE must set, then the report is initiated again (after
      *> an intervening TERMINATE, §1985:XIII.4.4.4 GR3). The sum
      *> counter and PAGE-COUNTER are set by MOVE, which the standard
      *> allows (§1985:XIII.3.19.4 GR6, §1985:XIII.3.5.5 SR6);
      *> LINE-COUNTER is moved by presenting lines.
      *> DERIVATION (unpaged report, §1985:XIII.3.8.4 GR3):
      *>   INITIATE: LC 00 PC 01 SC 00.
      *>   GENERATE DE-G (V 5): line 1 (§1985:XIII.3.10.8 GR4d),
      *>     subtotalling SC 5. MOVE 7 TO PAGE-COUNTER.
      *>   TERMINATE: CF FINAL on line 2 (GR4d), LC 2; SC is reset
      *>     after the footing (§1985:XIII.3.20.4 GR10f).
      *>   MOVE 42 TO the sum counter, MOVE 9 TO PAGE-COUNTER:
      *>     LC 02 PC 09 SC 42.
      *>   INITIATE again: LC 00 PC 01 SC 00.
      *> Fails if any of the three is left at its earlier value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2908.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2908.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-V            PIC 9     VALUE 0.
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-PC           PIC 99    VALUE 0.
       01  WS-SC           PIC 99    VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DE-G TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 9 SOURCE WS-V.
       01  CF-G TYPE CF FINAL LINE PLUS 1.
           02  TOT COLUMN 1 PIC 99 SUM WS-V.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           PERFORM SHOW-COUNTERS.
           MOVE 5 TO WS-V.
           GENERATE DE-G.
           PERFORM SHOW-COUNTERS.
           MOVE 7 TO PAGE-COUNTER.
           TERMINATE RPT.
           MOVE 42 TO TOT.
           MOVE 9 TO PAGE-COUNTER.
           PERFORM SHOW-COUNTERS.
           INITIATE RPT.
           PERFORM SHOW-COUNTERS.
           TERMINATE RPT.
           CLOSE PRT.
           STOP RUN.
       SHOW-COUNTERS.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE TOT TO WS-SC.
           DISPLAY "LC " WS-LC " PC " WS-PC " SC " WS-SC.
