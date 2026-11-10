      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-18
      *> rules: [1985-GR-VII.4.3.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR18 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LBLIO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "AAAAA" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN I-O F1.
           DISPLAY WS-ST.
           READ F1 AT END DISPLAY "EOF1".
           MOVE "BBBBB" TO F1-REC.
           REWRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF2".
           DISPLAY F1-REC.
           CLOSE F1.
           STOP RUN.
