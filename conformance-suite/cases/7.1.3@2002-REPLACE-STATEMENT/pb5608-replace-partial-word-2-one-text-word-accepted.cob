      *>--- conformance-case
      *> id: pb5608-replace-partial-word-2-one-text-word-accepted
      *> rules: [2002-SR-7.1.3.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR6 - "Partial-word-2 shall consist of zero or
      *> one text-word"; §2002:7.1.1.4 - a text-word is "any other
      *> sequence of contiguous COBOL characters bounded by separators".
      *> Partial-word-2 YZ is one text-word: LEADING XX is replaced in
      *> XX1, which becomes YZ1, the data-name the program displays.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608A.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 YZ1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE LEADING ==XX== BY ==YZ==.
           DISPLAY XX1.
           STOP RUN.
