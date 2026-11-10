      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-6
      *> rules: [1985-GR-XI.4.1.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR6 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO IN1FILE.
           SELECT IN2 ASSIGN TO IN2FILE.
           SELECT OUTF ASSIGN TO OUTFILE.
           SELECT WK ASSIGN TO WKFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(3).
       FD  IN2.
       01  IN2-REC PIC X(3).
       FD  OUTF.
       01  OUT-REC PIC X(3).
       SD  WK.
       01  WK-REC.
           05  WK-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
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
           MERGE WK ON ASCENDING KEY WK-KEY
               USING IN1 IN2
               GIVING OUTF.
           OPEN INPUT OUTF.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE OUTF.
           STOP RUN.
       READ-PARA.
           READ OUTF AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               DISPLAY OUT-REC.
