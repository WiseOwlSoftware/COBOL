      *>--- conformance-case
      *> id: 1985-GR-VIII_4_4_4-19
      *> rules: [1985-GR-VIII.4.4.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR19 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W85C8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA-FILE ASSIGN TO "w85c8-a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS A-STATUS.
           SELECT RB-FILE ASSIGN TO "w85c8-b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS B-KEY
               FILE STATUS IS B-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  RA-FILE.
       01  RA-REC PIC X(8).
       FD  RB-FILE.
       01  RB-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  A-STATUS PIC XX VALUE "??".
       01  B-STATUS PIC XX VALUE "??".
       01  B-KEY PIC 9(4) VALUE 1.
       01  USE-COUNT PIC 9 VALUE ZERO.
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RA-FILE.
       A-ERR-1.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE ON RA-FILE STATUS " A-STATUS.
       IO-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON I-O.
       IO-ERR-1.
           DISPLAY "USE ON I-O STATUS " B-STATUS.
       IN-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       IN-ERR-1.
           DISPLAY "USE ON INPUT STATUS " B-STATUS.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-ABSENT.
           MOVE "??" TO A-STATUS.
           OPEN INPUT RA-FILE.
           DISPLAY "OPEN INPUT RA ABSENT STATUS " A-STATUS.
           MOVE "??" TO A-STATUS.
           OPEN I-O RA-FILE.
           DISPLAY "OPEN I-O RA ABSENT STATUS " A-STATUS.
           MOVE "??" TO A-STATUS.
           OPEN INPUT RA-FILE.
           DISPLAY "OPEN INPUT RA AGAIN STATUS " A-STATUS.
           MOVE "??" TO B-STATUS.
           OPEN I-O RB-FILE.
           DISPLAY "OPEN I-O RB ABSENT STATUS " B-STATUS.
           MOVE "??" TO B-STATUS.
           OPEN INPUT RB-FILE.
           DISPLAY "OPEN INPUT RB ABSENT STATUS " B-STATUS.
       P-CREATE.
           OPEN OUTPUT RA-FILE.
           DISPLAY "OPEN OUTPUT RA STATUS " A-STATUS.
           MOVE "REC-0001" TO RA-REC.
           WRITE RA-REC.
           MOVE "REC-0002" TO RA-REC.
           WRITE RA-REC.
           CLOSE RA-FILE.
       P-OPEN-TWICE.
           OPEN INPUT RA-FILE.
           DISPLAY "OPEN INPUT RA STATUS " A-STATUS.
           MOVE "??" TO A-STATUS.
           OPEN OUTPUT RA-FILE.
           DISPLAY "OPEN OUTPUT RA WHILE OPEN STATUS " A-STATUS.
           READ RA-FILE.
           DISPLAY "READ 1 " RA-REC " STATUS " A-STATUS.
           READ RA-FILE.
           DISPLAY "READ 2 " RA-REC " STATUS " A-STATUS.
       P-LOCKED.
           CLOSE RA-FILE WITH LOCK.
           DISPLAY "CLOSE WITH LOCK STATUS " A-STATUS.
           MOVE "??" TO A-STATUS.
           OPEN INPUT RA-FILE.
           DISPLAY "OPEN INPUT RA LOCKED STATUS " A-STATUS.
           DISPLAY "USE ON RA-FILE RAN " USE-COUNT " TIMES".
           STOP RUN.
