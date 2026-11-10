      *>--- conformance-case
      *> id: 1985-SR-VII_4_2_3-1
      *> rules: [1985-SR-VII.4.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.2.3 SR1 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLOSMIX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO SEQFILE
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE.
       01  SEQ-REC PIC X(10).
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       77  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE REL-FILE.
           MOVE "SEQREC0001" TO SEQ-REC.
           WRITE SEQ-REC.
           MOVE 1 TO REL-KEY.
           MOVE "RELREC0001" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD KEY".
           CLOSE SEQ-FILE REL-FILE.
           OPEN INPUT SEQ-FILE REL-FILE.
           READ SEQ-FILE AT END DISPLAY "SEQ EOF".
           DISPLAY SEQ-REC.
           READ REL-FILE NEXT RECORD AT END DISPLAY "REL EOF".
           DISPLAY REL-REC.
           CLOSE REL-FILE SEQ-FILE.
           DISPLAY "CLOSED BOTH".
           STOP RUN.
