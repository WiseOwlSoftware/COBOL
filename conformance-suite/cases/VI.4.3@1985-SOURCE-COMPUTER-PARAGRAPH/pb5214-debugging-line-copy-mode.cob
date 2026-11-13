      *>--- conformance-case
      *> id: pb5214-debugging-line-copy-mode
      *> rules: [1985-GR-VI.4.3.4-5, 1985-GR-XII.2.4-9]
      *> accept: [85]
      *> files: [PB5214-DL1.cpy, PB5214-DL2.cpy, PB5214-DL3.cpy, PB5214-DL4.cpy]
      *> source-format: fixed
      *>---
      *> §1985:XII.2.4 GR9: each text word introduced by a COPY statement
      *> that begins on a debugging line appears on a debugging line.
      *> §1985:VI.4.3.4 GR4: with WITH DEBUGGING MODE the debugging lines
      *> are compiled, so the copied text of a debugging-line COPY runs
      *> (REPLACING, and the text after the statement's
      *> period included), and so does a debugging line of a library.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5214B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ITEM-A PIC X(4) VALUE "INIT".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    COPY PB5214-DL1.
      D    COPY PB5214-DL2.
      D    COPY PB5214-DL3 REPLACING ==TEXT-WORD== BY ==ITEM-A==.
           COPY PB5214-DL4.
      D    COPY PB5214-DL1. DISPLAY "TAIL-ON-D-LINE".
           DISPLAY ITEM-A.
           DISPLAY "END".
           STOP RUN.
