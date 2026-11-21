      *>--- conformance-case
      *> id: 1985-GR-XI_1_1-p1
      *> rules: [1985-GR-XI.1.1-p1, 1985-GR-XI.1.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.1.1 p1; §1985:XI.1.2.2 p1 (INTRODUCTION OF THE
      *> SORT-MERGE MODULE). An output test: the program must translate
      *> and run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-16.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "x11s.tmp".
           SELECT MF ASSIGN TO "x11m.tmp".
           SELECT I1 ASSIGN TO "x11i1.dat".
           SELECT I2 ASSIGN TO "x11i2.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SF RECORD CONTAINS 2 CHARACTERS DATA RECORD IS SR.
       01 SR.
          05 SK PIC X.
          05 SV PIC X.
       SD MF DATA RECORD IS MR.
       01 MR.
          05 MK PIC X.
          05 MV PIC X.
       FD I1.
       01 I1R PIC XX.
       FD I2.
       01 I2R PIC XX.
       WORKING-STORAGE SECTION.
       01 W-EOF PIC X VALUE "N".
       01 W-OUT PIC X(12) VALUE SPACES.
       01 W-P PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       P-0.
           OPEN OUTPUT I1 I2.
           MOVE "A1" TO I1R. WRITE I1R.
           MOVE "C1" TO I1R. WRITE I1R.
           MOVE "B2" TO I2R. WRITE I2R.
           MOVE "D2" TO I2R. WRITE I2R.
           CLOSE I1 I2.
           SORT SF ON DESCENDING KEY SK
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "SORT=" W-OUT.
           MOVE SPACES TO W-OUT. MOVE 1 TO W-P. MOVE "N" TO W-EOF.
           MERGE MF ON ASCENDING KEY MK USING I1 I2
               OUTPUT PROCEDURE IS MOUT-P.
           DISPLAY "MERGE=" W-OUT.
           STOP RUN.
       IN-P SECTION.
       IN-1.
           MOVE "B1" TO SR. RELEASE SR.
           MOVE "C2" TO SR. RELEASE SR.
           MOVE "A3" TO SR. RELEASE SR.
       OUT-P SECTION.
       OUT-1.
           PERFORM OUT-2 UNTIL W-EOF = "Y".
           GO TO OUT-9.
       OUT-2.
           RETURN SF AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N"
               MOVE SR TO W-OUT (W-P:2) ADD 2 TO W-P.
       OUT-9.
           EXIT.
       MOUT-P SECTION.
       MOUT-1.
           PERFORM MOUT-2 UNTIL W-EOF = "Y".
           GO TO MOUT-9.
       MOUT-2.
           RETURN MF AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N"
               MOVE MR TO W-OUT (W-P:2) ADD 2 TO W-P.
       MOUT-9.
           EXIT.
