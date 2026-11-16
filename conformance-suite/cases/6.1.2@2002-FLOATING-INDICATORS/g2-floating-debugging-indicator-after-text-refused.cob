      *>--- conformance-case
      *> id: g2-floating-debugging-indicator-after-text-refused
      *> rules: [2002-SR-6.1.2.1-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR7 - "A floating debugging indicator may be
      *> preceded on a line only by zero or more space characters": a
      *> >>D after the program text DISPLAY "A" on the same line is no
      *> debugging line, and the line is refused at the '>'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FDI03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A" >>D DISPLAY "B".
           STOP RUN.
