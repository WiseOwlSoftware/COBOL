      *>--- conformance-case
      *> id: pb5624-present-when-ancestor-names-subordinate-counter
      *> rules: [SR-13.18.64.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5624 - "This definition of data-name-1 may be
      *> referenced only within the current entry or a subordinate
      *> entry." (§2023:13.18.64.3 SR2; cite.py OK). The PRESENT WHEN of
      *> the LINE entry is written on an entry ABOVE the one that
      *> defines R, so it names nothing: COBOL1639. (The line is
      *> composed with its subordinate entries' conditions, but each
      *> condition sees only the counters of the entry it is written on
      *> and of the entries enclosing that one.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5624NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5624ng.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VR.
       REPORT SECTION.
       RD  R-VR PAGE LIMIT 35 LINES.
       01  DET-V TYPE DE.
           02  LINE PLUS 1 PRESENT WHEN R NOT = 2.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 2
                   VARYING R SOURCE R.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VR.
           GENERATE DET-V.
           TERMINATE R-VR.
           CLOSE PRT.
           STOP RUN.
