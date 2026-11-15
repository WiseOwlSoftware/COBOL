      *>--- conformance-case
      *> id: g2-copy-pseudo-text-1-no-text-word-refused
      *> rules: [2002-SR-7.1.2.2-6]
      *> files: [G2CPA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR6 - "Pseudo-text-1 shall contain one or more
      *> text-words, at least one of which shall be neither a separator
      *> comma nor a separator semicolon". The only content of
      *> pseudo-text-1 is a separator comma on a D line. Without WITH
      *> DEBUGGING MODE the D line becomes a comment only after COPY
      *> processing (§2002:6.2.7), so the comma is part of
      *> pseudo-text-1 (SR12), which then holds no qualifying text-word,
      *> and the program is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY10.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       PROCEDURE DIVISION.
       P1.
           COPY G2CPA REPLACING ==
      D    ,
           == BY ==BBB==.
           STOP RUN.
