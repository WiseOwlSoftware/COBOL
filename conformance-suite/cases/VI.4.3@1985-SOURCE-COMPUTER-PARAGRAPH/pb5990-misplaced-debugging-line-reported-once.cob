      *>--- conformance-case
      *> id: pb5990-misplaced-debugging-line-reported-once
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "A debugging line is only permitted in the
      *> separately compiled program after the OBJECT-COMPUTER
      *> paragraph." The DATE-WRITTEN paragraph below is written on a
      *> debugging line before it; §1985:VI.4.3.4 GR4 compiles debugging
      *> lines as that presentation specifies. The unit has no mode, and
      *> the one cause is reported once, as COBOL7060 alone (kb/Work
      *> PB5990, PB5212).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990E.
      DDATE-WRITTEN. TODAY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "END".
           STOP RUN.
