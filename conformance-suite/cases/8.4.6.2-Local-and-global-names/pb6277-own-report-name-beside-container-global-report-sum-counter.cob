      *>--- conformance-case
      *> id: pb6277-own-report-name-beside-container-global-report-sum-counter
      *> rules: [GR-8.4.6.2.1-1, GR-8.4.6.2.1-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6277. The container P77O declares the GLOBAL report
      *> R1 (its FD names R1) with the sum counter TOT under CFG. The
      *> contained P77I declares its OWN report R1 (an RD of the same
      *> spelling, with no entry named TOT). 8.4.6.2.1 GR1 (cite.py:
      *> OK): the set of names for a reference in P77I consists of "all
      *> names that are defined in source element B and all global names
      *> that are defined in source element A", and the normal rules for
      *> qualification and any other rules for uniqueness of reference
      *> are applied until one or more items is identified. 13.18.27.4
      *> GR1 (cite.py: OK): "All data-names subordinate to a global name
      *> are global names", so the container's TOT is in the set though
      *> P77I's own R1 hides the container's report-NAME. TOT, and TOT
      *> OF CFG, identify exactly one item, the container's counter
      *> (GR2): it has summed the one DETAIL with WS-N 4, so P77I
      *> displays 04 for both. The container then reads its counter: 04.
      *> A compiler that drops the container report from P77I's names
      *> because the spelling R1 is P77I's own refuses TOT at
      *> translation. 1985:X.1.3.8.2 (GR1, GR2, GR3a) is the same rule
      *> at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P77O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p77o.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF GLOBAL REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0 GLOBAL.
       01 WS-T PIC 99 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R1 GLOBAL CONTROL FINAL.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       01 CFG TYPE CF FINAL LINE PLUS 1.
          02 TOT COLUMN 1 PIC 99 SUM WS-N.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT RPF
           INITIATE R1
           MOVE 4 TO WS-N
           GENERATE DET
           CALL "P77I"
           MOVE TOT TO WS-T
           DISPLAY "O TOT=" WS-T
           TERMINATE R1
           CLOSE RPF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P77I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPG ASSIGN TO "p77i.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPG REPORT IS R1.
       REPORT SECTION.
       RD R1.
       01 DETI TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       I-1.
           MOVE TOT TO WS-T
           DISPLAY "I TOT=" WS-T
           MOVE TOT OF CFG TO WS-T
           DISPLAY "I TOTCFG=" WS-T
           EXIT PROGRAM.
       END PROGRAM P77I.
       END PROGRAM P77O.
