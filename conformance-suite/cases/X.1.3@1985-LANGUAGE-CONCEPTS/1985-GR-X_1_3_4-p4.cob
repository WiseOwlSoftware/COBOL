      *>--- conformance-case
      *> id: 1985-GR-X_1_3_4-p4
      *> rules: [1985-GR-X.1.3.4-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.4 p4 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 7: X.1.3.4 p4 at 85: external objects may have
      * global or local names. XG is EXTERNAL GLOBAL (a contained
      * program uses it undescribed); XL is EXTERNAL only (local: the
      * contained program must describe it to reach it); the FD XGF is
      * EXTERNAL GLOBAL. Refusal twin: x01_external_local_name.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15XG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XGF ASSIGN TO "S15XGF.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  XGF EXTERNAL GLOBAL.
       01  XGREC       PIC X(4).
       WORKING-STORAGE SECTION.
       01  XG EXTERNAL GLOBAL PIC X(4).
       01  XL EXTERNAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "GGGG" TO XG.
           MOVE "LLLL" TO XL.
           CALL "S15XGI".
           DISPLAY "OUT-XG=" XG " OUT-XL=" XL.
           OPEN INPUT XGF.
           READ XGF AT END DISPLAY "OUT-EOF".
           DISPLAY "OUT-XGREC=" XGREC.
           CLOSE XGF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15XGI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XL EXTERNAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "IN-XG=" XG " IN-XL=" XL.
           MOVE "gggg" TO XG.
           MOVE "llll" TO XL.
           OPEN OUTPUT XGF.
           MOVE "WREC" TO XGREC.
           WRITE XGREC.
           CLOSE XGF.
           EXIT PROGRAM.
       END PROGRAM S15XGI.
       END PROGRAM S15XG.
