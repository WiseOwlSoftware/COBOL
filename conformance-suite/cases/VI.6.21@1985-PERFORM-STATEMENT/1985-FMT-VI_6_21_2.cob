      *>--- conformance-case
      *> id: 1985-FMT-VI_6_21_2
      *> rules: [1985-FMT-VI.6.21.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.2 (THE PERFORM STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  CNT PIC 9 VALUE 0.
       77  N   PIC 9 VALUE 2.
       77  I   PIC 9 VALUE 0.
       77  J   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM PA.
           PERFORM PA THRU PB.
           PERFORM PB THROUGH PC.
           PERFORM DISPLAY "INLINE" END-PERFORM.
           PERFORM 3 TIMES
               ADD 1 TO CNT
               DISPLAY "PASS " CNT
           END-PERFORM.
           PERFORM PC N TIMES.
           MOVE 0 TO CNT.
           PERFORM PD UNTIL CNT = 2.
           PERFORM PD WITH TEST AFTER UNTIL CNT > 1.
           PERFORM WITH TEST BEFORE UNTIL CNT > 1
               DISPLAY "NEVER"
           END-PERFORM.
           PERFORM PE VARYING I FROM 1 BY 1 UNTIL I > 2
               AFTER J FROM 3 BY -1 UNTIL J < 2.
           PERFORM WITH TEST AFTER VARYING I FROM 5 BY -2 UNTIL I = 1
               DISPLAY "V" I
           END-PERFORM.
           STOP RUN.
       PA.
           DISPLAY "A".
       PB.
           DISPLAY "B".
       PC.
           DISPLAY "C".
       PD.
           ADD 1 TO CNT.
           DISPLAY "D" CNT.
       PE.
           DISPLAY "E" I J.
