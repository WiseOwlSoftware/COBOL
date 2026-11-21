      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_3-p2
      *> rules: [1985-GR-IV.4.3.3-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.3 p2 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.3-p2, 1985-GR-IV.4.3.4-p3: a function is an
      * elementary item of category alphanumeric or numeric; an
      * alphanumeric function's size is its definition's; integer
      * and numeric functions are used in arithmetic expressions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FUNCAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC               PIC X(5) VALUE "abcde".
       01  OUT-X             PIC X(8).
       01  N                 PIC 9(4).
       01  R                 PIC 9V9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UPPER-CASE(SRC) TO OUT-X.
           DISPLAY "UPPER=[" OUT-X "]".
           MOVE FUNCTION REVERSE(SRC)(2:3) TO OUT-X.
           DISPLAY "REFMOD=[" OUT-X "]".
           COMPUTE N = FUNCTION LENGTH(FUNCTION REVERSE(SRC)).
           DISPLAY "LEN-ALNUM-FN=" N.
           COMPUTE N = FUNCTION LENGTH(SRC) * 2 + 1.
           DISPLAY "INT-FN-IN-EXPR=" N.
           COMPUTE R = FUNCTION SQRT(2).
           DISPLAY "NUM-FN=" R.
           IF FUNCTION UPPER-CASE(SRC) = "ABCDE"
               DISPLAY "ALNUM-FN-COMPARE OK".
           STOP RUN.
