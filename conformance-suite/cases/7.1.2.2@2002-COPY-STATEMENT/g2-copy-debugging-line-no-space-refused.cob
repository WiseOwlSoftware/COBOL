      *>--- conformance-case
      *> id: g2-copy-debugging-line-no-space-refused
      *> rules: [2002-SR-7.1.2.2-2]
      *> files: [G2CPN.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR2 - "A COPY statement shall be preceded by a
      *> space except when it is the first statement in a compilation
      *> group". On the D line below COPY follows "(" with no space; a
      *> 2002 debugging line is processed for COPY before it is compiled
      *> or becomes a comment (§2002:6.2.7), so the rule applies there.
      *> Refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       01 AAA PIC X(3) VALUE "OLD".
       01 BBB PIC X(3) VALUE "NEW".
       PROCEDURE DIVISION.
       P1.
      D    COMPUTE X = (COPY G2CPN. )
           DISPLAY X.
           STOP RUN.
