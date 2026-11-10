      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-4
      *> rules: [1985-GR-IV.6.4.2.1.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR4 (Conditional Statements and Sentences).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDDEL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO DISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "RECORD-ONE" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED"
           END-WRITE.
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 5 TO REL-KEY.
           DELETE REL-FILE
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "VALID"
           END-DELETE.
           MOVE 1 TO REL-KEY.
           DELETE REL-FILE
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "VALID"
           END-DELETE.
           CLOSE REL-FILE.
           STOP RUN.
