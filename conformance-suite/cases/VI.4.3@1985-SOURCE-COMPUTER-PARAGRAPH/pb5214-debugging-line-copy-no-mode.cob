      *>--- conformance-case
      *> id: pb5214-debugging-line-copy-no-mode
      *> rules: [1985-GR-VI.4.3.4-5, 1985-GR-XII.2.4-9]
      *> accept: [85]
      *> files: [PB5214-DL1.cpy, PB5214-DL2.cpy, PB5214-DL3.cpy, PB5214-DL4.cpy]
      *> source-format: fixed
      *>---
      *> §1985:XII.2.4 GR9: each text word introduced by a COPY statement
      *> that begins on a debugging line appears on a debugging line.
      *> §1985:VI.4.3.4 GR5: without WITH DEBUGGING MODE any debugging
      *> lines are compiled as if they were comment lines, so the copied
      *> text of a debugging-line COPY (REPLACING, and the
      *> text after the statement's period included) is comment; the
      *> normal COPY of a library holding its own debugging line keeps
      *> its normal line and drops its debugging line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5214A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
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
