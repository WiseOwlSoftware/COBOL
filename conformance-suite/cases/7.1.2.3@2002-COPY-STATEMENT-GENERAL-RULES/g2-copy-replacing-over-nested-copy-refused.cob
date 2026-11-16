      *>--- conformance-case
      *> id: g2-copy-replacing-over-nested-copy-refused
      *> rules: [2002-GR-7.1.2.3-11]
      *> files: [G2CBNEST.cpy, G2CBW.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR11 - "If the REPLACING phrase is specified, the
      *> library text shall not contain a COPY statement": the COPY
      *> below has the 2002 word-1 BY literal-4 REPLACING form, and
      *> G2CBNEST holds COPY G2CBW. Refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY13.
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
           COPY G2CBNEST REPLACING AAA BY "HELLO".
           STOP RUN.
