      *>--- conformance-case
      *> id: g2-floating-debugging-indicator-on-fixed-debugging-line-refused
      *> rules: [2002-SR-6.1.2.1-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR8 - "A floating debugging indicator shall not
      *> be specified on a line that contains a fixed debugging indicator
      *> or a fixed continuation indicator": >>D on a line whose
      *> indicator area holds the fixed debugging indicator D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FDI04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       P1.
      D>>D DISPLAY "D".
           STOP RUN.
