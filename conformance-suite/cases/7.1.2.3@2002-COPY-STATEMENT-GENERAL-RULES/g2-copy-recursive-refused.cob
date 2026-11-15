      *>--- conformance-case
      *> id: g2-copy-recursive-refused
      *> rules: [2002-GR-7.1.2.3-14]
      *> files: [G2CBREC.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR14 - "The library text being copied shall not
      *> cause the processing of a COPY statement that directly or
      *> indirectly copies itself": G2CBREC holds COPY G2CBREC. Refused
      *> at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY14.
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
           COPY G2CBREC.
           STOP RUN.
