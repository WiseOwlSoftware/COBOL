      *>--- conformance-case
      *> id: 1985-SR-VI_6_12_3-3
      *> rules: [1985-SR-VI.6.12.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.3 SR3 (THE ENTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENTTST.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE".
           GO TO P3.
       P2.
           ENTER FORTRAN SUBRTN.
           ENTER COBOL.
           DISPLAY "RETURNED".
       P3.
           DISPLAY "AFTER".
           STOP RUN.
