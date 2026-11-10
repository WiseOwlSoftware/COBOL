      *>--- conformance-case
      *> id: 1985-GR-VIII_4_4_4-5
      *> rules: [1985-GR-VIII.4.4.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR5 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELOPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "ALPHA" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END MOVE "Y" TO WS-FLAG.
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           READ REL-FILE AT END MOVE "Y" TO WS-FLAG.
           MOVE "BRAVO" TO REL-REC.
           REWRITE REL-REC.
           CLOSE REL-FILE.
           OPEN EXTEND REL-FILE.
           MOVE "DELTA" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END MOVE "Y" TO WS-FLAG.
           DISPLAY REL-REC.
           READ REL-FILE AT END MOVE "Y" TO WS-FLAG.
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           DISPLAY "AT END " WS-FLAG.
           STOP RUN.
