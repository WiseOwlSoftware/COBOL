      *>--- conformance-case
      *> id: 1985-GR-VIII_4_6_4-6
      *> rules: [1985-GR-VIII.4.6.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR6 (THE REWRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWRT6.
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
       01  REL-REC              PIC X(7).
       WORKING-STORAGE SECTION.
       01  WS-REC               PIC X(7).
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT REL-FILE.
           MOVE "ORIGINL" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           READ REL-FILE
               AT END DISPLAY "NO RECORD".
           MOVE "UPDATED" TO WS-REC.
           REWRITE REL-REC FROM WS-REC.
           DISPLAY WS-REC.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE
               AT END DISPLAY "NO RECORD".
           DISPLAY REL-REC.
           DISPLAY WS-REC.
           CLOSE REL-FILE.
           STOP RUN.
