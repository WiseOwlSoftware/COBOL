      *>--- conformance-case
      *> id: pb5214-debugging-line-copy-nested-no-mode
      *> rules: [2002-GR-7.1.2.3-13]
      *> accept: [2002]
      *> files: [PB5214-DL1.cpy, PB5214-NEST.cpy]
      *> source-format: fixed
      *>---
      *> §2002:7.1.2.3 GR13: text introduced by a COPY statement that begins
      *> on a debugging line appears on debugging lines, the text of a COPY
      *> statement nested in the library text included (COBOL-85 forbids
      *> the nesting, §1985:XII.2.4 GR12). Without WITH DEBUGGING MODE the
      *> debugging lines are comment lines: only START and END print.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5214C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    COPY PB5214-NEST.
           DISPLAY "END".
           STOP RUN.
