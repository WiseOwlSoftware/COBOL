      *>--- conformance-case
      *> id: g2-copy-debugging-line-partial-word-1-refused
      *> rules: [2002-SR-7.1.2.2-14]
      *> files: [G2CPA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR14 - "Partial-word-1 shall consist of one
      *> text-word": partial-word-1 of the D-line COPY below holds two.
      *> The COPY is processed before the D line becomes a comment
      *> (§2002:6.2.7), so the program is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MAIN".
      D    COPY G2CPA REPLACING LEADING ==AA BB== BY ==CC==.
           STOP RUN.
