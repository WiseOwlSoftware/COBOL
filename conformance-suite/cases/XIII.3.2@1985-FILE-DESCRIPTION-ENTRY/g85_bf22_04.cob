      *>--- conformance-case
      *> id: g85_bf22_04
      *> rules: [1985-SR-XIII.3.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.2.3 SR2 - the clauses after file-name-1 in any order
      *> (report file). RULE (cite.py OK): "The clauses which follow
      *> file-name-1 may appear in any order." The printed order
      *> (§1985:XIII.3.2.2) is BLOCK, RECORD, LABEL, VALUE OF, CODE-SET,
      *> REPORT (REPORT printed last, carrying the period). REP-F writes
      *> them REVERSED: REPORT / LABEL / RECORD / BLOCK, so REPORT is
      *> first and BLOCK ends the entry. The program must translate at
      *> 85 and the REPORT clause must still take effect. DERIVATION:
      *> §1985:XIII.4.4.4 GR1b and GR1c (both cite.py OK): INITIATE sets
      *> "LINE-COUNTER is set to zero." and "PAGE-COUNTER is set to
      *> one." -> PC=01 LC=00 (moved to PIC 99). GENERATE and TERMINATE
      *> then run: DONE. The report file's record structure is the
      *> implementor's (§1985:XIII.3.2.4 GR2, cite.py OK), so the file's
      *> content is not compared.
      *> A leg fails if REPORT first is refused or the report-name it
      *> names is not tied to RPT (INITIATE would not run).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2204.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REP-F ASSIGN TO "G85BF224.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  REP-F
           REPORT IS RPT
           LABEL RECORDS ARE OMITTED
           RECORD CONTAINS 20 CHARACTERS
           BLOCK CONTAINS 1 RECORDS.
       WORKING-STORAGE SECTION.
       01  WS-PC PIC 99.
       01  WS-LC PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT REP-F.
           INITIATE RPT.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "PC=" WS-PC " LC=" WS-LC.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE REP-F.
           DISPLAY "DONE".
           STOP RUN.
