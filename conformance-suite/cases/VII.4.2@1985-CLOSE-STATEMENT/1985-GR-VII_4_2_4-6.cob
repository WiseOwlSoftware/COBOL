      *>--- conformance-case
      *> id: 1985-GR-VII_4_2_4-6
      *> rules: [1985-GR-VII.4.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.2.4 GR6 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLOSEREC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO DISKFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-SAVE PIC X(5) VALUE SPACES.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "ABCDE" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT F.
           READ F INTO W-SAVE AT END MOVE "EOF  " TO W-SAVE.
           CLOSE F.
           DISPLAY W-SAVE.
           STOP RUN.
