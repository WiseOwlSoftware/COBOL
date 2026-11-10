      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-17
      *> rules: [1985-GR-VIII.4.5.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR17 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDEND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "A" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "EOF 1" END-READ
           DISPLAY "AFTER 1 " REL-REC.
           READ REL-FILE AT END DISPLAY "EOF 2" END-READ
           DISPLAY "AFTER 2".
           CLOSE REL-FILE.
           STOP RUN.
