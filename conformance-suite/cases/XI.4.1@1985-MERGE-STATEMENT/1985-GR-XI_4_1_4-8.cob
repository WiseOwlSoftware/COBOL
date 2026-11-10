      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-8
      *> rules: [1985-GR-XI.4.1.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR8 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO DISK1.
           SELECT IN2 ASSIGN TO DISK2.
           SELECT WRK ASSIGN TO DISK3.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(3).
       FD  IN2.
       01  IN2-REC PIC X(3).
       SD  WRK.
       01  WRK-REC.
           05  WRK-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           OPEN OUTPUT IN1.
           MOVE "AAA" TO IN1-REC.
           WRITE IN1-REC.
           MOVE "CCC" TO IN1-REC.
           WRITE IN1-REC.
           CLOSE IN1.
           OPEN OUTPUT IN2.
           MOVE "BBB" TO IN2-REC.
           WRITE IN2-REC.
           MOVE "DDD" TO IN2-REC.
           WRITE IN2-REC.
           CLOSE IN2.
           MERGE WRK ON ASCENDING KEY WRK-KEY
               USING IN1 IN2
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       OUT-PROC SECTION.
       O1.
           PERFORM GET-REC.
           IF EOF-FLAG = "N"
               DISPLAY WRK-KEY
               GO TO O1.
       HELPERS SECTION.
       GET-REC.
           RETURN WRK AT END MOVE "Y" TO EOF-FLAG.
