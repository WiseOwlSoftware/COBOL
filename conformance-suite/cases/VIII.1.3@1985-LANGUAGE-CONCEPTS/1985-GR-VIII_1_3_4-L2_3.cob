      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_4-L2_3
      *> rules: [1985-GR-VIII.1.3.4-L2.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GRL2.3 (LANGUAGE CONCEPTS.). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELIKY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "REL85M.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS1.
           SELECT OPTIONAL RLO ASSIGN TO "NONE85M.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS2.
           SELECT RLS ASSIGN TO "SEQ85M.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK1
               FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RFR PIC X(4).
       FD  RLO.
       01  ROR PIC X(4).
       FD  RLS.
       01  RSR PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK PIC 99.
       01  RK1 PIC 9.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  FS3 PIC XX.
       01  N PIC 99 VALUE 0.
       01  CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RLF.
           MOVE 2 TO RK.
           MOVE "AAAA" TO RFR.
           WRITE RFR INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "W1 " FS1.
           MOVE "BBBB" TO RFR.
           WRITE RFR INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "W2 " FS1.
           CLOSE RLF.
           OPEN INPUT RLF.
           MOVE 5 TO RK.
           READ RLF INVALID KEY DISPLAY "R1 INVALID".
           DISPLAY "R1 " FS1.
           CLOSE RLF.
           OPEN INPUT RLO.
           DISPLAY "O1 " FS2.
           MOVE 1 TO RK.
           READ RLO INVALID KEY DISPLAY "R2 INVALID".
           DISPLAY "R2 " FS2.
           CLOSE RLO.
           OPEN OUTPUT RLS.
           PERFORM WRITE-RLS 10 TIMES.
           CLOSE RLS.
           DISPLAY "S OK " CNT.
           STOP RUN.
       WRITE-RLS.
           ADD 1 TO N.
           MOVE "CCCC" TO RSR.
           WRITE RSR INVALID KEY DISPLAY "S" N " INVALID " FS3.
           IF FS3 = "00" ADD 1 TO CNT.
