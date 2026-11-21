      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_1_2-p6
      *> rules: [1985-GR-IV.4.3.1.2-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.1.2 p6 (Logical Record Concept). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Its
      *> expected outcome rests on the implementor's determination
      *> DOC-A.1-8 (a native ordinal is the code unit plus one) and the
      *> EBCDIC implementor-name's code table, not on the rule alone.
      *> Placed from written and run by the PB5453 rows pass
      *> (w20262131i: review lead R1-2, the written bytes read back
      *> through a second file control entry with no CODE-SET) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.1.2-p6 (EBCDIC variant): translation on transfer.
      * The record is written through a CODE-SET EBCDIC description and
      * read back twice: through that description (the program's own
      * characters again) and through a second file control entry for
      * the same file with no CODE-SET, whose bytes are the EBCDIC ones:
      * a space is X'40' (ordinal 65), A X'C1' (194), B X'C2' (195),
      * 0 X'F0' (241). EBCDIC is an implementor-name of this
      * implementation and a native ordinal is the code unit plus one
      * (DOC-A.1-8), so these values are WiseOwl's. A compiler that did
      * not translate would show 33, 66, 67 and 49.
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
           SELECT RAW-FILE ASSIGN TO CSDISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  CS-FILE
           LABEL RECORDS ARE STANDARD
           CODE-SET IS ASC.
       01  CS-REC.
           05  R-TXT         PIC X(5) JUSTIFIED RIGHT.
           05  R-NUM         PIC 9(3) BLANK WHEN ZERO.
       FD  RAW-FILE
           LABEL RECORDS ARE STANDARD.
       01  RAW-REC           PIC X(8).
       WORKING-STORAGE SECTION.
       01  N                 PIC 999.
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
           OPEN INPUT RAW-FILE.
           READ RAW-FILE AT END DISPLAY "EOF?".
           COMPUTE N = FUNCTION ORD (RAW-REC (1:1)).
           DISPLAY "SPACE " N.
           COMPUTE N = FUNCTION ORD (RAW-REC (4:1)).
           DISPLAY "A " N.
           COMPUTE N = FUNCTION ORD (RAW-REC (5:1)).
           DISPLAY "B " N.
           READ RAW-FILE AT END DISPLAY "EOF?".
           COMPUTE N = FUNCTION ORD (RAW-REC (6:1)).
           DISPLAY "0 " N.
           CLOSE RAW-FILE.
           STOP RUN.
