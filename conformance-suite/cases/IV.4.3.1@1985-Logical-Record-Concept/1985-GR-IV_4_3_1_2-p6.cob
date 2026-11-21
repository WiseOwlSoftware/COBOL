      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_1_2-p6
      *> rules: [1985-GR-IV.4.3.1.2-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.1.2 p6 (Logical Record Concept). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.3.1.2-p6 (EBCDIC variant): translation on transfer.
      * logical record is transferred to or from the physical unit;
      * the record description clauses have no effect on transfer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CODESEB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CS-FILE ASSIGN TO CSDISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  CS-FILE
           LABEL RECORDS ARE STANDARD
           CODE-SET IS ASC.
       01  CS-REC.
           05  R-TXT         PIC X(5) JUSTIFIED RIGHT.
           05  R-NUM         PIC 9(3) BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT CS-FILE.
           MOVE "AB" TO R-TXT.
           MOVE 0 TO R-NUM.
           WRITE CS-REC.
           MOVE "HELLO" TO R-TXT.
           MOVE 42 TO R-NUM.
           WRITE CS-REC.
           CLOSE CS-FILE.
           OPEN INPUT CS-FILE.
           READ CS-FILE AT END DISPLAY "EOF?".
           DISPLAY "[" CS-REC "]".
           READ CS-FILE AT END DISPLAY "EOF?".
           DISPLAY "[" CS-REC "]".
           CLOSE CS-FILE.
           STOP RUN.
