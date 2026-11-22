      *>--- conformance-case
      *> id: g85_bf18_01
      *> rules: [1985-GR-VII.1.3.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GR1 - "Successful Completion. The input-output
      *> statement was successfully executed." Every OPEN, WRITE, CLOSE,
      *> READ and REWRITE below on a sequential file succeeds, so each
      *> reports the Successful Completion value 00 (§1985:VII.1.3.5
      *> GRL2.1 item a), and by the clause's own text no USE AFTER
      *> STANDARD EXCEPTION procedure runs for it: "If one of the
      *> conditions listed under the heading Successful Completion ...
      *> results, no such procedure will be executed" (§1985:VII.1.3.5).
      *> USE-COUNT stays 0 through every successful statement. The last
      *> READ hits the at end condition with no AT END phrase, which is
      *> NOT successful (§1985:VII.4.4.4 GR10c, so the procedure runs
      *> once: that leg proves the declarative is live, so the 0 cannot
      *> pass by a procedure that never fires. Each leg fails if a
      *> successful statement reports anything but 00, runs the USE
      *> procedure, or loses a record written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1801.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "G85BF1801.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQ-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  SQ-ST PIC XX.
       01  USE-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ.
       SQ-ERR-PARA.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE RAN " SQ-ST.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT SQ.
           DISPLAY "OPEN-OUTPUT " SQ-ST.
           MOVE "REC001" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "WRITE-1 " SQ-ST.
           MOVE "REC002" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "WRITE-2 " SQ-ST.
           CLOSE SQ.
           DISPLAY "CLOSE-1 " SQ-ST.
           OPEN EXTEND SQ.
           DISPLAY "OPEN-EXTEND " SQ-ST.
           MOVE "REC003" TO SQ-REC.
           WRITE SQ-REC.
           DISPLAY "WRITE-3 " SQ-ST.
           CLOSE SQ.
           DISPLAY "CLOSE-2 " SQ-ST.
           OPEN I-O SQ.
           DISPLAY "OPEN-I-O " SQ-ST.
           READ SQ.
           DISPLAY "READ-1 " SQ-ST " " SQ-REC.
           MOVE "REC01X" TO SQ-REC.
           REWRITE SQ-REC.
           DISPLAY "REWRITE " SQ-ST.
           CLOSE SQ.
           DISPLAY "CLOSE-3 " SQ-ST.
           OPEN INPUT SQ.
           DISPLAY "OPEN-INPUT " SQ-ST.
           READ SQ.
           DISPLAY "READ-A " SQ-ST " " SQ-REC.
           READ SQ.
           DISPLAY "READ-B " SQ-ST " " SQ-REC.
           READ SQ.
           DISPLAY "READ-C " SQ-ST " " SQ-REC.
           DISPLAY "USE-COUNT " USE-COUNT.
           READ SQ.
           DISPLAY "AT-END " SQ-ST.
           DISPLAY "USE-COUNT " USE-COUNT.
           CLOSE SQ.
           DISPLAY "CLOSE-4 " SQ-ST.
           STOP RUN.
