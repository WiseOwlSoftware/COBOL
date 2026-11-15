      *>--- conformance-case
      *> id: g2-copy-within-copy-refused
      *> rules: [2002-SR-7.1.2.2-1]
      *> files: [G2CPA.cpy, G2CPL.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR1 - "a COPY statement shall not appear within a
      *> COPY statement": pseudo-text-2 of the REPLACING phrase below
      *> holds COPY G2CPL. The program is not conforming and is refused
      *> at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY03.
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
           COPY G2CPA REPLACING ==AAA== BY =="X" COPY G2CPL.==.
           STOP RUN.
