      *>--- conformance-case
      *> id: pb5426-four-hierarchic-levels
      *> rules: [SR-13.15.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5426 - 1985:XIII.3.9.3 SR4: "A description of a report group may consist of one, two, or three hierarchic
      *> levels" and b. "Both format 2 and format 3 entries may be immediately subordinate to a format 1 entry." (cite.py: OK).
      *> `03  G2` has no PICTURE clause and none of the clauses of a format 3 entry, so it is a format 2 entry, immediately
      *> subordinate to `02  G1`, another format 2 entry: four levels. COBOL-2002 and later allow any depth (13.15.3 SR3).
      *> conformance:pb5426-four-hierarchic-levels-legal is the legal side from COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5426FL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5426FL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE IS DETAIL.
           02  G1 LINE PLUS 1.
               03  G2.
                   04  COLUMN 1 PIC X VALUE "A".
               03  COLUMN 5 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
