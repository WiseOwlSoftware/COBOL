      *>--- conformance-case
      *> id: 1985-FMT-VII_2_3_2
      *> rules: [1985-FMT-VII.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.3.2 (THE FILE CONTROL ENTRY). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQFC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "SEQFC.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT OPTIONAL OPT-FILE
               FILE STATUS IS WS-OPT
               ACCESS SEQUENTIAL
               PADDING CHARACTER IS WS-PAD
               SEQUENTIAL
               RESERVE 2 AREAS
               ASSIGN TO "SEQFCOPT.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(5).
       FD  OPT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OPT-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-OPT  PIC XX.
       01  WS-PAD  PIC X VALUE "*".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           DISPLAY WS-STAT.
           MOVE "HELLO" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC " " WS-STAT.
           READ SEQ-FILE AT END DISPLAY "EOF " WS-STAT.
           CLOSE SEQ-FILE.
           OPEN INPUT OPT-FILE.
           DISPLAY WS-OPT.
           READ OPT-FILE AT END DISPLAY "OPTIONAL " WS-OPT.
           CLOSE OPT-FILE.
           STOP RUN.
