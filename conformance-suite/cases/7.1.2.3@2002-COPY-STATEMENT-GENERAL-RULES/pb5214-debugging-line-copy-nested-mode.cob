      *>--- conformance-case
      *> id: pb5214-debugging-line-copy-nested-mode
      *> rules: [2002-GR-7.1.2.3-13]
      *> accept: [2002]
      *> files: [PB5214-DL1.cpy, PB5214-NEST.cpy]
      *> source-format: fixed
      *>---
      *> §2002:7.1.2.3 GR13: text introduced by a COPY statement that begins
      *> on a debugging line appears on debugging lines, the text of a COPY
      *> statement nested in the library text included. With WITH DEBUGGING
      *> MODE the debugging lines are compiled, so the outer and the nested
      *> library text both run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5214D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    COPY PB5214-NEST.
           DISPLAY "END".
           STOP RUN.
