      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-p3
      *> rules: [1985-SR-IV.4.3.8.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 p3 (Uniqueness of Reference). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.3.8.1-p3: the general formats for qualification.
      * Format 1 data-name and condition-name IN data-name IN file-name,
      * Format 2 paragraph-name IN section-name, Format 4
      * LINAGE-COUNTER IN file-name (two LINAGE files), IN = OF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05QUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "s05qa.txt".
           SELECT F2 ASSIGN TO "s05qb.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LINAGE IS 10 LINES.
       01  R1.
           05  K       PIC X.
               88  K-ON    VALUE "Y".
           05  FILLER  PIC X(9).
       FD  F2 LINAGE IS 10 LINES.
       01  R2.
           05  K       PIC X.
               88  K-ON    VALUE "Y".
           05  FILLER  PIC X(9).
       PROCEDURE DIVISION.
       S1 SECTION.
       PX.
           OPEN OUTPUT F1 F2.
           MOVE SPACES TO R1 R2.
           MOVE "Y" TO K IN R1 IN F1.
           MOVE "N" TO K OF R2 OF F2.
           IF K-ON IN K IN R1 IN F1 DISPLAY "F1-K-ON".
           IF NOT K-ON OF F2 DISPLAY "F2-K-OFF".
           WRITE R1.
           WRITE R1.
           WRITE R2.
           DISPLAY "LC1=" LINAGE-COUNTER IN F1.
           DISPLAY "LC2=" LINAGE-COUNTER OF F2.
           CLOSE F1 F2.
           PERFORM PX IN S2.
           PERFORM PX OF S2.
           STOP RUN.
       S2 SECTION.
       PX.
           DISPLAY "S2-PX".
