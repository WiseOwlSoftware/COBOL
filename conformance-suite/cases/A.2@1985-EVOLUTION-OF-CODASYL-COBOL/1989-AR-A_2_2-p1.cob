      *>--- conformance-case
      *> id: 1989-AR-A_2_2-p1
      *> rules: [1989-AR-A.2.2-p1, 1989-GR-A.1.2.1-p1, 1989-GR-A.1.2.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 p1; §1989:A.1.2.1 p1; §1989:A.1.2.3 p1 (THE
      *> EVOLUTION OF CODASYL COBOL). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-19.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-19 p18: functions and their arguments at 85 (1989 A.1.2.1
      * p1, A.1.2.3 p1, A.2.2 p1). Zero arguments (CURRENT-DATE),
      * one, a variable number (MAX of 1, 3, 5 arguments); arguments
      * as identifiers, arithmetic expressions and literals; order
      * decides meaning (MOD 11 3 vs MOD 3 11); the value is fixed
      * when the function is referenced (the loop's MOD changes).
      * Each line prints OK or BAD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 99 VALUE 11.
       01  B   PIC 99 VALUE 3.
       01  I   PIC 9.
       01  R   PIC S9(4).
       01  D   PIC X(8).
       01  ACC PIC X(5) VALUE SPACES.
       01  DG  PIC 9.
       PROCEDURE DIVISION.
       M.
           MOVE FUNCTION CURRENT-DATE TO D.
           IF D IS NUMERIC DISPLAY "OK  ZERO ARGS"
           ELSE DISPLAY "BAD ZERO ARGS " D.
           COMPUTE R = FUNCTION MAX (7).
           IF R = 7 DISPLAY "OK  ONE ARG" ELSE DISPLAY "BAD ONE " R.
           COMPUTE R = FUNCTION MAX (A, 4, B + 20).
           IF R = 23 DISPLAY "OK  THREE ARGS EXPR"
           ELSE DISPLAY "BAD THREE " R.
           COMPUTE R = FUNCTION MIN (9 8 A B 5).
           IF R = 3 DISPLAY "OK  FIVE ARGS" ELSE DISPLAY "BAD FIVE " R.
           COMPUTE R = FUNCTION MOD (A, B).
           IF R = 2 DISPLAY "OK  MOD A B" ELSE DISPLAY "BAD MOD A B " R.
           COMPUTE R = FUNCTION MOD (B, A).
           IF R = 3 DISPLAY "OK  MOD B A" ELSE DISPLAY "BAD MOD B A " R.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               COMPUTE R = FUNCTION MOD (I, 3)
               MOVE R TO DG
               MOVE DG TO ACC (I:1)
           END-PERFORM.
           IF ACC = "12012" DISPLAY "OK  AT REFERENCE"
           ELSE DISPLAY "BAD AT REFERENCE " ACC.
           STOP RUN.
