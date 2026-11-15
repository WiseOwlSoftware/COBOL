      *>--- conformance-case
      *> id: g2-replace-debugging-line-partial-word-1-refused
      *> rules: [2002-SR-7.1.3.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR5 - "Partial-word-1 shall consist of one
      *> text-word": partial-word-1 of the D-line REPLACE below holds
      *> two. No WITH DEBUGGING MODE: the REPLACE is processed before
      *> the D line becomes a comment (§2002:6.2.7, §2002:7.1.3.2 SR1),
      *> so the program is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "OLD".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN".
      D    REPLACE LEADING ==AA BB== BY ==CC==.
           DISPLAY AAA.
           STOP RUN.
