      *>--- conformance-case
      *> id: pb6075-source-index-cites-1985
      *> rules: [SR-13.18.53.3-2, SR-13.18.60.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6075. §1985:XIII.3.18.3 SR2: "Identifier-1 must be defined such that it conforms to the rules for sending items in the MOVE
      *> statements", and §1985:VI.6.19.3 SR4: "An index data item must not appear as an operand of a MOVE statement".
      *> SOURCE IS IX is refused (COBOL2770) on SR4 alone: §1985:VI.5.14.3 SR5 limits Procedure Division statements
      *> (§1985:IV.4.4.1) and is not cited for a Report Section clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6075S85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB6075S85".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  IX USAGE IS INDEX.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DETAIL LINE PLUS 1.
           10  COLUMN 1 PIC 9(4) SOURCE IS IX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
