      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-7
      *> rules: [1985-GR-XI.4.1.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR7 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO DISK1.
           SELECT IN2 ASSIGN TO DISK2.
           SELECT OUTF ASSIGN TO DISK3.
           SELECT MWORK ASSIGN TO DISK4.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1
           LABEL RECORDS ARE STANDARD.
       01  IN1-REC PIC X(2).
       FD  IN2
           LABEL RECORDS ARE STANDARD.
       01  IN2-REC PIC X(2).
       FD  OUTF
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(2).
       SD  MWORK.
       01  M-REC.
           05 M-KEY PIC X.
           05 M-TAG PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IN1.
           MOVE "1A" TO IN1-REC.
           WRITE IN1-REC.
           MOVE "3A" TO IN1-REC.
           WRITE IN1-REC.
           CLOSE IN1.
           OPEN OUTPUT IN2.
           MOVE "2B" TO IN2-REC.
           WRITE IN2-REC.
           MOVE "4B" TO IN2-REC.
           WRITE IN2-REC.
           CLOSE IN2.
           MERGE MWORK ON ASCENDING KEY M-KEY
               USING IN1 IN2 GIVING OUTF.
           OPEN INPUT OUTF.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE OUTF.
           STOP RUN.
       READ-PARA.
           READ OUTF AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY OUT-REC.
