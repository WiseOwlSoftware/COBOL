      *>--- conformance-case
      *> id: pb5212-debugging-line-before-object-computer
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "A debugging line is only permitted in the
      *> separately compiled program after the OBJECT-COMPUTER
      *> paragraph." The DATE-WRITTEN paragraph below is written on a
      *> debugging line before it; §1985:VI.4.3.4 GR4 compiles debugging
      *> lines as that presentation specifies (kb/Work PB5212).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5212A.
      DDATE-WRITTEN. TODAY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "END".
           STOP RUN.
