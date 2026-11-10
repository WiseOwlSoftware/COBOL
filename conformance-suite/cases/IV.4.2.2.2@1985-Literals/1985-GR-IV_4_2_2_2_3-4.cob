      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-4
      *> rules: [1985-GR-IV.4.2.2.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR4 (Literals). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOWVAL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X(3) VALUE "ABC".
       01  Y PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
       P1.
           MOVE LOW-VALUES TO X.
           IF X = LOW-VALUE
               DISPLAY "ALL-LOW"
           ELSE
               DISPLAY "NOT-ALL-LOW".
           IF X NOT > Y
               DISPLAY "NOT-GREATER"
           ELSE
               DISPLAY "GREATER".
           IF X NOT > SPACES
               DISPLAY "NOT-GREATER-SPACE"
           ELSE
               DISPLAY "GREATER-SPACE".
           STOP RUN.
