      *>--- conformance-case
      *> id: 1985-GR-X_1_3_2-p1
      *> rules: [1985-GR-X.1.3.2-p1, 1985-GR-X.1.3.4-p1, 1985-GR-X.1.3.4-p2, 1985-GR-X.1.3.4-p3, 1985-GR-X.1.3.4-p5, 1985-GR-X.1.3.4-p6, 1985-GR-X.1.3.4-p7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.2 p1; §1985:X.1.3.4 p1; §1985:X.1.3.4 p2;
      *> §1985:X.1.3.4 p3; §1985:X.1.3.4 p5; §1985:X.1.3.4 p6;
      *> §1985:X.1.3.4 p7 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 2: external and internal objects, at 85.
      * X.1.3.4: an EXTERNAL record or file connector is one object
      * for the run unit, shared by every program that describes it
      * (separate descriptions, same object); data subordinate to an
      * external record or to an EXTERNAL FD is external; data with no
      * EXTERNAL clause is internal to the program describing it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15EXA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "S15XF.DAT" ORGANIZATION IS SEQUENTIAL.
           SELECT IF1 ASSIGN TO "S15IA.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  XF EXTERNAL.
       01  XREC.
           05  XREC-A  PIC X(4).
       FD  IF1.
       01  IREC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  XD EXTERNAL.
           05  XD-A    PIC X(4).
           05  XD-B    PIC 9(4).
       01  INT1        PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ONE " TO XD-A.
           MOVE 1 TO XD-B.
           OPEN OUTPUT XF.
           MOVE "SHAR" TO XREC-A.
           MOVE "AAAA" TO IREC.
           CALL "S15EXB".
           DISPLAY "A-XD=" XD-A XD-B " A-INT1=" INT1 " A-IREC=" IREC.
           MOVE "AREC" TO XREC-A.
           WRITE XREC.
           CLOSE XF.
           OPEN INPUT XF.
           READ XF AT END DISPLAY "A-EOF".
           DISPLAY "A-READ1=" XREC-A.
           READ XF AT END DISPLAY "A-EOF".
           DISPLAY "A-READ2=" XREC-A.
           CLOSE XF.
           CALL "S15EXC".
           STOP RUN.
       END PROGRAM S15EXA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15EXB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "S15XF.DAT" ORGANIZATION IS SEQUENTIAL.
           SELECT IF1 ASSIGN TO "S15IB.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  XF EXTERNAL.
       01  XREC        PIC X(4).
       FD  IF1.
       01  IREC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  XD EXTERNAL PIC X(8).
       01  INT1        PIC X(4) VALUE "BBBB".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "B-XD=" XD " B-XREC=" XREC.
           MOVE "ZZZZ" TO IREC.
           MOVE "TWO 0002" TO XD.
           MOVE "ZZZZ" TO INT1.
           MOVE "BREC" TO XREC.
           WRITE XREC.
           EXIT PROGRAM.
       END PROGRAM S15EXB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15EXC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XD EXTERNAL.
           05  XD-ALL  PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "C-XD=" XD-ALL.
           CALL "S15EXD".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15EXD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XD EXTERNAL PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "D-XD=" XD.
           EXIT PROGRAM.
       END PROGRAM S15EXD.
       END PROGRAM S15EXC.
