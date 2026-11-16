      *>--- conformance-case
      *> id: g85_bf17_02
      *> rules: [1985-SR-VI.6.21.3-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.3 SR12 - at least six AFTER phrases are
      *> permitted in format 4; this PERFORM writes six.
      * A parse-only witness would pass a sixth AFTER that nests
      * or resets wrongly, so the program pins the sequence
      * §1985:VI.6.21.4 GR10d gives: each AFTER item goes through
      * a complete cycle each time the item of the preceding
      * phrase is augmented. Seven items, each 1 BY 1 UNTIL > 2:
      *   COUNT 128 = 2 ** 7 executions of the set.
      *   SNAP2 1111112: the last AFTER item varies fastest, so
      *   the second execution sees only I7 augmented.
      *   FINAL 3111111: at termination each AFTER item holds its
      *   FROM value and the VARYING item exceeds its last used
      *   setting by one increment (GR10d).
      * Out-of-line, because SR2 forbids AFTER in an in-line
      * format 4 PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1702.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I1  PIC 9 VALUE 0.
       01 I2  PIC 9 VALUE 0.
       01 I3  PIC 9 VALUE 0.
       01 I4  PIC 9 VALUE 0.
       01 I5  PIC 9 VALUE 0.
       01 I6  PIC 9 VALUE 0.
       01 I7  PIC 9 VALUE 0.
       01 CNT PIC 9(3) VALUE 0.
       01 SNAP2.
          05 P1 PIC 9 VALUE 0.
          05 P2 PIC 9 VALUE 0.
          05 P3 PIC 9 VALUE 0.
          05 P4 PIC 9 VALUE 0.
          05 P5 PIC 9 VALUE 0.
          05 P6 PIC 9 VALUE 0.
          05 P7 PIC 9 VALUE 0.
       01 FINALT.
          05 Q1 PIC 9 VALUE 0.
          05 Q2 PIC 9 VALUE 0.
          05 Q3 PIC 9 VALUE 0.
          05 Q4 PIC 9 VALUE 0.
          05 Q5 PIC 9 VALUE 0.
          05 Q6 PIC 9 VALUE 0.
          05 Q7 PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM BODY-P
                   VARYING I1 FROM 1 BY 1 UNTIL I1 > 2
                   AFTER I2 FROM 1 BY 1 UNTIL I2 > 2
                   AFTER I3 FROM 1 BY 1 UNTIL I3 > 2
                   AFTER I4 FROM 1 BY 1 UNTIL I4 > 2
                   AFTER I5 FROM 1 BY 1 UNTIL I5 > 2
                   AFTER I6 FROM 1 BY 1 UNTIL I6 > 2
                   AFTER I7 FROM 1 BY 1 UNTIL I7 > 2.
           MOVE I1 TO Q1.
           MOVE I2 TO Q2.
           MOVE I3 TO Q3.
           MOVE I4 TO Q4.
           MOVE I5 TO Q5.
           MOVE I6 TO Q6.
           MOVE I7 TO Q7.
           DISPLAY "SR12-COUNT=" CNT.
           DISPLAY "SR12-SNAP2=" SNAP2.
           DISPLAY "SR12-FINAL=" FINALT.
           STOP RUN.
       BODY-P.
           ADD 1 TO CNT.
           IF CNT = 2
               MOVE I1 TO P1
               MOVE I2 TO P2
               MOVE I3 TO P3
               MOVE I4 TO P4
               MOVE I5 TO P5
               MOVE I6 TO P6
               MOVE I7 TO P7.
