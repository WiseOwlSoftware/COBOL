      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_6-p1
      *> rules: [1985-GR-VI.6.4.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.6 p1 (COMMON OPTIONS AND RULES FOR STATEMENTS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01MULT.
      * 1985 VI.6.4.6 p1: ADD, COMPUTE, DIVIDE, MULTIPLY, SUBTRACT
      * may have multiple results; each receiver gets its own result.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9(3) VALUE 10.
       01  B   PIC 9(3) VALUE 20.
       01  C   PIC 9(3) VALUE 30.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO A B C.
           DISPLAY "ADD " A " " B " " C.
           SUBTRACT 1 FROM A B C.
           DISPLAY "SUB " A " " B " " C.
           MULTIPLY 2 BY A B C.
           DISPLAY "MUL " A " " B " " C.
           DIVIDE 2 INTO A B C.
           DISPLAY "DIV " A " " B " " C.
           DIVIDE 5 INTO 100 GIVING A B C.
           DISPLAY "DVG " A " " B " " C.
           SUBTRACT 1 FROM 50 GIVING A B C.
           DISPLAY "SBG " A " " B " " C.
           COMPUTE A B C = 7 * 3.
           DISPLAY "CMP " A " " B " " C.
           STOP RUN.
