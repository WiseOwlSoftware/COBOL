      *>--- conformance-case
      *> id: 1989-GR-A_2_7_1-p1
      *> rules: [1989-GR-A.2.7.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.7.1 p1 (CODASYL COBOL JOURNAL OF DEVELOPMENT 1970).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe asin-85: ASIN value; type numeric (arithmetic use; INTEGER(...) as integer argument is the control)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PASIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(8)V9(6).
       01 I PIC S9(9).
       01 N PIC S9(9).
       01 X PIC X(30).
       01 A1 PIC A(3) VALUE "ABC".
       01 A2 PIC A(3) VALUE "ABD".
       01 OK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R ROUNDED = FUNCTION ASIN(0.5).
           IF R = 0.523599
               DISPLAY "OK   ASIN value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL ASIN value 1"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION ASIN(1).
           IF R = 1.570796
               DISPLAY "OK   ASIN value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL ASIN value 2"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION ASIN(0.5) * 2 - FUNCTION
               ASIN(0.5).
           IF R = 0.523599
               DISPLAY "OK   ASIN in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL ASIN in arithmetic expression"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION ASIN(1))
               7).
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
