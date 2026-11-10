      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-2
      *> rules: [1985-GR-IV.6.4.2.1.1.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR2 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDINV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "RECORD-ONE" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED"
           END-WRITE.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 2 TO WS-KEY.
           READ REL-FILE
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "FOUND"
           END-READ.
           MOVE 1 TO WS-KEY.
           READ REL-FILE
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "FOUND"
           END-READ.
           CLOSE REL-FILE.
           STOP RUN.
