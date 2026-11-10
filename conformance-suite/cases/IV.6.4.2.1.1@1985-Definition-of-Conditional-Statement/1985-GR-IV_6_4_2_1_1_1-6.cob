      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-6
      *> rules: [1985-GR-IV.6.4.2.1.1.1-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR6 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR6TST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-SRC PIC X(5) VALUE "ABCDE".
       01  WS-DST PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       P1.
           STRING WS-SRC DELIMITED BY SIZE INTO WS-DST
               ON OVERFLOW DISPLAY "OVERFLOW"
               NOT ON OVERFLOW DISPLAY "NO OVERFLOW"
           END-STRING.
           DISPLAY WS-DST.
           STOP RUN.
