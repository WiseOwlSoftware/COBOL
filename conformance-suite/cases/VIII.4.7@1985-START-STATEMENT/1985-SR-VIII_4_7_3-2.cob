      *>--- conformance-case
      *> id: 1985-SR-VIII_4_7_3-2
      *> rules: [1985-SR-VIII.4.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.3 SR2 (THE START STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELSTQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY OF KEY-GROUP.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  KEY-GROUP.
           05  REL-KEY PIC 9(4).
       01  OTHER-GROUP.
           05  REL-KEY PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "REC01" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD KEY 1".
           MOVE "REC02" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD KEY 2".
           MOVE "REC03" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD KEY 3".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 2 TO REL-KEY OF KEY-GROUP.
           START REL-FILE KEY IS EQUAL TO REL-KEY OF KEY-GROUP
               INVALID KEY DISPLAY "START FAILED"
           END-START.
           READ REL-FILE
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY REL-REC
           END-READ.
           DISPLAY REL-KEY OF OTHER-GROUP.
           CLOSE REL-FILE.
           STOP RUN.
