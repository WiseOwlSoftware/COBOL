      *>--- conformance-case
      *> id: 1985-GR-VII_2_3_1-p1
      *> rules: [1985-GR-VII.2.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.3.1 p1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01BSEQALL.
      * 1985 VII.2.3: a sequential file control entry with every
      * clause of its general format, clauses after SELECT in a
      * non-canonical order (SR1 "may appear in any order").
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL SQF
               FILE STATUS IS WS-FS
               ACCESS MODE IS SEQUENTIAL
               RECORD DELIMITER IS STANDARD-1
               PADDING CHARACTER IS "*"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 2 AREAS
               ASSIGN TO "r01bseqall.dat".
       DATA DIVISION.
       FILE SECTION.
       FD SQF RECORD CONTAINS 1 TO 5 CHARACTERS.
       01 SQ-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF.
           MOVE "HELLO" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQF.
           OPEN INPUT SQF.
           READ SQF AT END DISPLAY "EOF".
           DISPLAY "SEQ=" SQ-REC " FS=" WS-FS.
           CLOSE SQF.
           STOP RUN.
