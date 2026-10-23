      *>--- conformance-case
      *> id: pb261_commit_facility_witness
      *> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB261, PB3338 - COMMIT and ROLLBACK in a program that
      *> declares no APPLY COMMIT clause. ISO 14.9.7.4 GR1 and
      *> 14.9.36.4 GR1: "If this statement is executed when there is no
      *> active APPLY COMMIT clause, then it has the same effect as a
      *> CONTINUE statement with no additional phrases." Control flows
      *> through both statements and nothing is saved or restored, so
      *> WS-N reaches 3. The statements under an ACTIVE clause are
      *> cases/14.9.36-ROLLBACK/pb3338_apply_commit_rollback.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CMTW01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BEFORE".
           ADD 1 TO WS-N.
           COMMIT.
           ADD 1 TO WS-N.
           ROLLBACK.
           ADD 1 TO WS-N.
           DISPLAY "N=" WS-N.
           DISPLAY "AFTER".
           STOP RUN.
