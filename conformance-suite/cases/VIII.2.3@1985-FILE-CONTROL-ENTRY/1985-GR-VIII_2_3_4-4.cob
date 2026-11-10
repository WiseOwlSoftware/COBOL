      *>--- conformance-case
      *> id: 1985-GR-VIII_2_3_4-4
      *> rules: [1985-GR-VIII.2.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.3.4 GR4 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELASG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "RELFILE"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "RECORD-ONE" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "NO RECORD".
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
