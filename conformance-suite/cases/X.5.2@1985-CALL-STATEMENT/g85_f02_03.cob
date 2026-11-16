      *>--- conformance-case
      *> id: g85_f02_03
      *> rules: [1985-GR-X.5.2.4-5, 1985-GR-X.5.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR5 - G85F02SB (not initial) and its contained
      *> G85F02NB keep state between calls (S2 N2) and are both initial
      *> on the first call after CANCEL "G85F02SB" (S1 N1 again).
      *> §1985:X.5.2.4 GR6 - G85F02IB is INITIAL: it and its contained
      *> G85F02NC are initial on every call (I1 M1 twice).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0203.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F02SB".
           CALL "G85F02SB".
           CANCEL "G85F02SB".
           DISPLAY "CANCELLED".
           CALL "G85F02SB".
           CALL "G85F02IB".
           CALL "G85F02IB".
           STOP RUN.
       END PROGRAM G85F0203.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-SB.
           ADD 1 TO N.
           DISPLAY "S" N.
           CALL "G85F02NB".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-NB.
           ADD 1 TO N.
           DISPLAY "N" N.
           EXIT PROGRAM.
       END PROGRAM G85F02NB.
       END PROGRAM G85F02SB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02IB IS INITIAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-IB.
           ADD 1 TO N.
           DISPLAY "I" N.
           CALL "G85F02NC".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-NC.
           ADD 1 TO N.
           DISPLAY "M" N.
           EXIT PROGRAM.
       END PROGRAM G85F02NC.
       END PROGRAM G85F02IB.
