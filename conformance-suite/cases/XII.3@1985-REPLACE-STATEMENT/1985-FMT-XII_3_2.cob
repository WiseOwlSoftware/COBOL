      *>--- conformance-case
      *> id: 1985-FMT-XII_3_2
      *> rules: [1985-FMT-XII.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.2 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPL1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           REPLACE ==AAA== BY ==WS-A==
                   ==BBB== BY ==WS-B==.
       01  AAA PIC X(3) VALUE "ONE".
       01  BBB PIC X(3) VALUE "TWO".
           REPLACE OFF.
       01  AAA PIC X(3) VALUE "RAW".
       PROCEDURE DIVISION.
       P1.
           DISPLAY WS-A.
           DISPLAY WS-B.
           DISPLAY AAA.
           STOP RUN.
