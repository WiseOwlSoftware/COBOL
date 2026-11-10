      *>--- conformance-case
      *> id: 1985-FMT-VI_6_23_2
      *> rules: [1985-FMT-VI.6.23.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.2 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 10 TIMES INDEXED BY IX IY.
       01  N PIC 99.
       01  M PIC 99 VALUE 2.
       01  FLAG PIC 9 VALUE 0.
           88  FLAG-ON VALUE 1.
       01  COLOR PIC X VALUE SPACE.
           88  RED VALUE "R" "S".
       PROCEDURE DIVISION.
       P1.
           SET IX TO 3.
           SET IX UP BY 2.
           SET N TO IX.
           DISPLAY N.
           SET IX DOWN BY 1.
           SET N TO IX.
           DISPLAY N.
           SET IY IX TO M.
           SET IX IY UP BY M.
           SET IY TO IX.
           SET IX DOWN BY 3.
           SET N TO IX.
           DISPLAY N.
           SET N TO IY.
           DISPLAY N.
           SET FLAG-ON RED TO TRUE.
           DISPLAY FLAG COLOR.
           SET SW1 TO ON.
           IF SW1-ON DISPLAY "ON" ELSE DISPLAY "NOT ON".
           SET SW1 TO OFF.
           IF SW1-OFF DISPLAY "OFF" ELSE DISPLAY "NOT OFF".
           STOP RUN.
