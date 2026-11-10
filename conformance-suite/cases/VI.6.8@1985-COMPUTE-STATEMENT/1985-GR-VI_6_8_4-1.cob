      *>--- conformance-case
      *> id: 1985-GR-VI_6_8_4-1
      *> rules: [1985-GR-VI.6.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.8.4 GR1 (THE COMPUTE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CMPSNG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 123.
       01 B PIC 9(3) VALUE 0.
       01 C PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           COMPUTE B = A.
           COMPUTE C = 456.
           DISPLAY B.
           DISPLAY C.
           STOP RUN.
