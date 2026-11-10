      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_4_3-3
      *> rules: [1985-SR-IV.4.3.8.4.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.3 SR3 (Reference Modification). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFANY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1 PIC X(8) VALUE "ABCDEFGH".
       01  G1.
           02  E1 OCCURS 3 TIMES PIC X(4).
       01  G2.
           02  F2 PIC X(4) VALUE "wxyz".
       01  G3.
           02  F2 PIC X(4) VALUE "QRST".
       01  W1 PIC X(8) VALUE SPACE.
       01  I1 PIC 9 VALUE 2.
       01  CNT PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-1.
           MOVE A1 (2:3) TO W1.
           DISPLAY W1 "|".
           MOVE A1 (6:) TO W1.
           DISPLAY W1 "|".
           DISPLAY A1 (I1 + 1:I1 * 2).
           MOVE "ABCDWXYZMNOP" TO G1.
           DISPLAY E1 (2) (2:2) "-" F2 OF G3 (3:2).
           IF A1 (3:2) = "CD"
               DISPLAY "EQUAL"
           ELSE
               DISPLAY "UNEQUAL".
           IF A1 (1:4) IS ALPHABETIC
               DISPLAY "ALPHABETIC"
           ELSE
               DISPLAY "NOT ALPHABETIC".
           MOVE "12" TO A1 (4:2).
           DISPLAY A1.
           IF A1 (4:2) IS NUMERIC
               DISPLAY "NUMERIC"
           ELSE
               DISPLAY "NOT NUMERIC".
           MOVE "AXBXCXDX" TO A1.
           INSPECT A1 (1:4) TALLYING CNT FOR ALL "X".
           DISPLAY CNT.
           INSPECT A1 (5:4) REPLACING ALL "X" BY "-".
           DISPLAY A1.
           MOVE SPACE TO W1.
           STRING A1 (1:2) A1 (7:2) DELIMITED BY SIZE INTO W1.
           DISPLAY W1 "|".
           DISPLAY FUNCTION UPPER-CASE (F2 OF G2 (2:2)) (2:1).
           STOP RUN.
