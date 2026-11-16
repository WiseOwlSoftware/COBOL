      *>--- conformance-case
      *> id: g2-copy-pseudo-text-directive-line-refused
      *> rules: [2002-SR-7.1.2.2-13]
      *> files: [G2CPA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR13 - "Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2": pseudo-text-2 below holds a SOURCE FORMAT
      *> directive line. Refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY06.
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
           COPY G2CPA REPLACING ==AAA== BY ==
       >>SOURCE FORMAT IS FIXED
           BBB==.
           STOP RUN.
