      *>--- conformance-case
      *> id: 1985-GR-IX_4_4_4-19
      *> rules: [1985-GR-IX.4.4.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR19 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXOPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-A ASSIGN TO "IXOPEN.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS A-KEY
               FILE STATUS IS WS-A.
           SELECT OPTIONAL IX-B ASSIGN TO "IXOPEN.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS B-KEY
               FILE STATUS IS WS-B.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-A.
       01  A-REC.
           05 A-KEY   PIC X(4).
           05 A-DATA  PIC X(6).
       FD  IX-B.
       01  B-REC.
           05 B-KEY   PIC X(4).
           05 B-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-A       PIC XX VALUE "ZZ".
       01  WS-B       PIC XX VALUE "ZZ".
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX-A.
       A-ERR-PARA.
           DISPLAY "USE PROCEDURE, STATUS " WS-A.
           OPEN INPUT IX-B.
           DISPLAY "OPTIONAL OPEN OF THE SAME FILE " WS-B.
           CLOSE IX-B.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           DISPLAY "BEFORE OPEN, STATUS " WS-A.
           OPEN INPUT IX-A.
           STOP RUN.
