      *>--- conformance-case
      *> id: 1989-GR-A_2_4-p1
      *> rules: [1989-GR-A.2.4-p1, 1989-GR-A.2.4-p2, 1989-GR-A.2.4-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.4 p1; §1989:A.2.4 p2; §1989:A.2.4 p3 (THE EVOLUTION
      *> OF CODASYL COBOL). An output test: the program must translate
      *> and run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      *  g3-fn-table: §1989:A.2.4 p1-p3. Every function Table 1 lists
      *  is available at 85, each called with arguments of the type
      *  its Arguments column gives (A alphabetic, I integer,
      *  N numeric, X alphanumeric). Each check that holds adds 1 to
      *  WS-OK; the program DISPLAYs FN-TABLE OK when all 42 hold.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G3FNTAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-OK   PIC 99 VALUE 0.
       01  WS-A    PIC A(3) VALUE "abc".
       01  WS-X    PIC X(3) VALUE "aBc".
       01  WS-I    PIC 9(4) VALUE 3.
       01  WS-N    PIC 9V9  VALUE 0.5.
       01  WS-R    PIC S9(8)V9(6).
       01  WS-S    PIC X(21).
       PROCEDURE DIVISION.
       P1.
           COMPUTE WS-R = FUNCTION ACOS(1).
           IF WS-R = 0 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION ANNUITY(0, 4).
           IF WS-R = 0.25 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION ASIN(0).
           IF WS-R = 0 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION ATAN(0).
           IF WS-R = 0 ADD 1 TO WS-OK.
           IF FUNCTION CHAR(66) = "A" ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION COS(0).
           IF WS-R = 1 ADD 1 TO WS-OK.
           MOVE FUNCTION CURRENT-DATE TO WS-S.
           IF WS-S(1:8) IS NUMERIC ADD 1 TO WS-OK.
           IF FUNCTION DATE-OF-INTEGER(1) = 16010101 ADD 1 TO WS-OK.
           IF FUNCTION DAY-OF-INTEGER(1) = 1601001 ADD 1 TO WS-OK.
           IF FUNCTION FACTORIAL(WS-I) = 6 ADD 1 TO WS-OK.
           IF FUNCTION INTEGER(-1.5) = -2 ADD 1 TO WS-OK.
           IF FUNCTION INTEGER-OF-DATE(16010102) = 2 ADD 1 TO WS-OK.
           IF FUNCTION INTEGER-OF-DAY(1601002) = 2 ADD 1 TO WS-OK.
           IF FUNCTION INTEGER-PART(-1.5) = -1 ADD 1 TO WS-OK.
           IF FUNCTION LENGTH(WS-A) = 3 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION LOG(1).
           IF WS-R = 0 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION LOG10(100).
           IF WS-R = 2 ADD 1 TO WS-OK.
           IF FUNCTION LOWER-CASE(WS-X) = "abc" ADD 1 TO WS-OK.
           IF FUNCTION MAX(1, WS-I, 2) = 3 ADD 1 TO WS-OK.
           IF FUNCTION MEAN(1, 2, 3) = 2 ADD 1 TO WS-OK.
           IF FUNCTION MEDIAN(1, 9, 3) = 3 ADD 1 TO WS-OK.
           IF FUNCTION MIDRANGE(1, 9, 3) = 5 ADD 1 TO WS-OK.
           IF FUNCTION MIN(WS-A, "abd") = "abc" ADD 1 TO WS-OK.
           IF FUNCTION MOD(-7, 3) = 2 ADD 1 TO WS-OK.
           IF FUNCTION NUMVAL(" 12.5 ") = 12.5 ADD 1 TO WS-OK.
           IF FUNCTION NUMVAL-C("$1,234", "$") = 1234 ADD 1 TO WS-OK.
           IF FUNCTION ORD("A") = 66 ADD 1 TO WS-OK.
           IF FUNCTION ORD-MAX(1, 9, 3) = 2 ADD 1 TO WS-OK.
           IF FUNCTION ORD-MIN(4, 9, 3) = 3 ADD 1 TO WS-OK.
           IF FUNCTION PRESENT-VALUE(0, 1, 2) = 3 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION RANDOM(1).
           IF WS-R >= 0 AND WS-R < 1 ADD 1 TO WS-OK.
           IF FUNCTION RANGE(1, 9, 3) = 8 ADD 1 TO WS-OK.
           IF FUNCTION REM(-7, 3) = -1 ADD 1 TO WS-OK.
           IF FUNCTION REVERSE(WS-X) = "cBa" ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION SIN(0).
           IF WS-R = 0 ADD 1 TO WS-OK.
           IF FUNCTION SQRT(4) = 2 ADD 1 TO WS-OK.
           IF FUNCTION STANDARD-DEVIATION(2, 2) = 0 ADD 1 TO WS-OK.
           IF FUNCTION SUM(1, WS-N, 2) = 3.5 ADD 1 TO WS-OK.
           COMPUTE WS-R = FUNCTION TAN(0).
           IF WS-R = 0 ADD 1 TO WS-OK.
           IF FUNCTION UPPER-CASE(WS-A) = "ABC" ADD 1 TO WS-OK.
           IF FUNCTION VARIANCE(1, 3) = 1 ADD 1 TO WS-OK.
           MOVE FUNCTION WHEN-COMPILED TO WS-S.
           IF WS-S(1:8) IS NUMERIC ADD 1 TO WS-OK.
           IF WS-OK = 42
               DISPLAY "FN-TABLE OK"
           ELSE
               DISPLAY "FN-TABLE FAIL " WS-OK
           END-IF.
           STOP RUN.
