      *>--- conformance-case
      *> id: 1989-GR-A_2_11_1-p1
      *> rules: [1989-GR-A.2.11.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.11.1 p1 (CODASYL COBOL JOURNAL OF DEVELOPMENT 1981).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe current-date-85: CURRENT-DATE: value and type (the control of current-date-type-refused-85)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PCURRENT.
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
           MOVE FUNCTION CURRENT-DATE TO X.
           IF X(1:16) IS NUMERIC
               DISPLAY "OK   positions 1-16 are digits"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL positions 1-16 are digits"
           END-IF.
           IF X(17:1) = "-" OR X(17:1) = "+" OR X(17:1) = "0"
               DISPLAY "OK   position 17 is - + or 0"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL position 17 is - + or 0"
           END-IF.
           IF X(18:4) IS NUMERIC
               DISPLAY "OK   positions 18-21 are digits"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL positions 18-21 are digits"
           END-IF.
           IF X(5:2) >= "01" AND X(5:2) <= "12"
               DISPLAY "OK   month 01-12"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL month 01-12"
           END-IF.
           IF X(22:9) = SPACES
               DISPLAY "OK   nothing past position 21"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL nothing past position 21"
           END-IF.
           COMPUTE N = FUNCTION LENGTH(FUNCTION CURRENT-DATE).
           IF N = 21
               DISPLAY "OK   21 characters"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL 21 characters"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
