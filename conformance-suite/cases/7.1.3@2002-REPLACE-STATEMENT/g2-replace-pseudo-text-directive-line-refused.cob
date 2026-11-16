      *>--- conformance-case
      *> id: g2-replace-pseudo-text-directive-line-refused
      *> rules: [2002-SR-7.1.3.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR11 - "Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2": pseudo-text-2 below holds a SOURCE FORMAT
      *> directive line (§2002:6.1.2: >> followed by a
      *> compiler-directive word). Refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "OLD".
       PROCEDURE DIVISION.
       P1.
           REPLACE ==XYZ== BY ==
           >>SOURCE FORMAT IS FIXED
           DISPLAY AAA==.
           XYZ
           STOP RUN.
