      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-12
      *> rules: [1985-GR-VIII.4.5.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR12 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85RD12.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-M ASSIGN TO "r85rd12m.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-M.
       01  RM-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RM-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-M.
       RM-ERR-1.
           DISPLAY "M-USE " FS.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-1.
           CALL "R85RD12S".
           DISPLAY "M0 BACK FROM CALL".
           OPEN OUTPUT REL-M.
           MOVE "AAAA" TO RM-REC.
           WRITE RM-REC.
           MOVE "BBBB" TO RM-REC.
           WRITE RM-REC.
           CLOSE REL-M.
           OPEN INPUT REL-M.
           READ REL-M RECORD AT END DISPLAY "M1 AT END".
           DISPLAY "M1 " FS " " RM-REC.
           READ REL-M RECORD AT END DISPLAY "M2 AT END".
           DISPLAY "M2 " FS " " RM-REC.
           READ REL-M RECORD AT END DISPLAY "M3 AT END " FS.
           MOVE "XX" TO FS.
           READ REL-M RECORD
               AT END DISPLAY "M4 AT END " FS
               NOT AT END DISPLAY "M4 NOT AT END " FS
           END-READ.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R85RD12S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-S ASSIGN TO "r85rd12s.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-S.
       01  RS-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       RS-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-S.
       RS-ERR-1.
           DISPLAY "S-USE " FS.
           EXIT PROGRAM.
       END DECLARATIVES.
       SUB-SEC SECTION.
       SUB-1.
           OPEN OUTPUT REL-S.
           MOVE 1 TO RK.
           MOVE "AAAA" TO RS-REC.
           WRITE RS-REC INVALID KEY DISPLAY "SW1 INVALID".
           MOVE 2 TO RK.
           MOVE "BBBB" TO RS-REC.
           WRITE RS-REC INVALID KEY DISPLAY "SW2 INVALID".
           CLOSE REL-S.
           OPEN INPUT REL-S.
           READ REL-S NEXT RECORD AT END DISPLAY "S1 AT END".
           DISPLAY "S1 " FS " " RS-REC.
           MOVE 9 TO RK.
           READ REL-S RECORD
               INVALID KEY DISPLAY "S2 INVALID KEY " FS
               NOT INVALID KEY DISPLAY "S2 NOT INVALID KEY"
           END-READ.
           MOVE "XX" TO FS.
           READ REL-S NEXT RECORD
               AT END DISPLAY "S3 AT END " FS
               NOT AT END DISPLAY "S3 NOT AT END " RS-REC
           END-READ.
       SUB-EXIT.
           EXIT PROGRAM.
       END PROGRAM R85RD12S.
       END PROGRAM R85RD12.
