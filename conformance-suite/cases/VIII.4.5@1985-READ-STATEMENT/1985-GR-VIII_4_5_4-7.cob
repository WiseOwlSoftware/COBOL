      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-7
      *> rules: [1985-GR-VIII.4.5.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR7 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
           SELECT NUM-FILE ASSIGN TO NUMFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(10).
       01  REC-B.
           05  REC-B-1 PIC X(5).
           05  REC-B-2 PIC X(5).
       FD  NUM-FILE
           LABEL RECORDS ARE STANDARD.
       01  NUM-REC PIC 9(4).
       WORKING-STORAGE SECTION.
       01  WS-AREA.
           05  WS-1 PIC X(5).
           05  WS-2 PIC X(5).
       01  WS-NUM PIC 9(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "HELLOWORLD" TO REC-A.
           WRITE REC-A
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE INTO WS-AREA
               AT END DISPLAY "EOF".
           DISPLAY WS-1.
           DISPLAY WS-2.
           CLOSE REL-FILE.
           OPEN OUTPUT NUM-FILE.
           MOVE 1234 TO NUM-REC.
           WRITE NUM-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE NUM-FILE.
           OPEN INPUT NUM-FILE.
           MOVE ZERO TO WS-NUM.
           READ NUM-FILE INTO WS-NUM
               AT END DISPLAY "EOF".
           DISPLAY WS-NUM.
           CLOSE NUM-FILE.
           STOP RUN.
