      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-11
      *> rules: [1985-GR-XI.4.1.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR11 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT OUTF ASSIGN TO "OUTF"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK.
           SELECT MW ASSIGN TO "MW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(3).
       FD  IN2.
       01  IN2-REC PIC X(3).
       FD  OUTF.
       01  OUT-REC PIC X(3).
       SD  MW.
       01  MW-REC.
           05  MW-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       77  RK PIC 9(4).
       77  EOF-SW PIC X VALUE "N".
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
           CLOSE IN2.
           MERGE MW ON ASCENDING KEY MW-KEY
               USING IN1 IN2 GIVING OUTF.
           DISPLAY RK.
           OPEN INPUT OUTF.
           PERFORM READ-PARA UNTIL EOF-SW = "Y".
           CLOSE OUTF.
           STOP RUN.
       READ-PARA.
           READ OUTF AT END MOVE "Y" TO EOF-SW.
           IF EOF-SW = "N" DISPLAY OUT-REC.
