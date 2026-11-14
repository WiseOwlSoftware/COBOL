      *>--- conformance-case
      *> id: pb5499-padding-table-element
      *> rules: [1985-SR-VII.2.7.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5499. §1985:VII.2.7.3 SR2: "Data-name-1 may be qualified."
      *> The clause names no subscript, and "Each table element reference must
      *> be subscripted" (§1985:IV.4.3.8.2.3 SR4), so a data-name-1 that is a
      *> table element (PT, under OCCURS) has no legal reading: refused
      *> (COBOL2024).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5499.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5499.dat"
               ORGANIZATION SEQUENTIAL
               PADDING CHARACTER IS PT.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  PTAB.
           05  PT PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P.
           DISPLAY "DONE"
           STOP RUN.
