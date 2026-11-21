      *>--- conformance-case
      *> id: 1989-GR-A_2_9_1-p1
      *> rules: [1989-GR-A.2.9.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.9.1 p1 (CODASYL COBOL JOURNAL OF DEVELOPMENT 1976).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe char-85: CHAR: value and type (the control of char-type-refused-85)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PCHAR.
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
           MOVE FUNCTION CHAR(66) TO X.
           IF X = "A"
               DISPLAY "OK   CHAR(66) is A in the native sequence"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL CHAR(66) is A in the native sequence"
           END-IF.
           COMPUTE I = FUNCTION LENGTH(FUNCTION CHAR(66)).
           IF I = 1
               DISPLAY "OK   CHAR is one character"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL CHAR is one character"
           END-IF.
           MOVE FUNCTION CHAR(66) TO X(2:1).
           IF X(1:2) = "AA"
               DISPLAY "OK   CHAR is an alphanumeric sending item"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL CHAR is an alphanumeric sending item"
           END-IF.
           COMPUTE N = FUNCTION ORD(FUNCTION CHAR(66)).
           IF N = 66
               DISPLAY "OK   ORD of CHAR"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL ORD of CHAR"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
