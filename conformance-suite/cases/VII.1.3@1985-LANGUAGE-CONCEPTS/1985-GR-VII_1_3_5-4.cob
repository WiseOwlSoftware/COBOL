      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-4
      *> rules: [1985-GR-VII.1.3.5-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GR4 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOGERR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO SEQDATA
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE.
       01  SEQ-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT.
           02 WS-STAT-1 PIC X.
           02 WS-STAT-2 PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SEQ-FILE.
       ERR-PARA.
           DISPLAY "IN DECLARATIVE " WS-STAT-1.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           CLOSE SEQ-FILE.
           STOP RUN.
