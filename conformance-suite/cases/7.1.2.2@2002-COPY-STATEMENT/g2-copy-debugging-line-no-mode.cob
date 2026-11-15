      *>--- conformance-case
      *> id: g2-copy-debugging-line-no-mode
      *> rules: [2002-SR-7.1.2.2-1, 2002-SR-7.1.2.2-6]
      *> accept: [2002]
      *> files: [G2CPL.cpy, G2CPA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR1 - a COPY statement "may be specified anywhere
      *> in source text" a character-string may appear, a D line
      *> included. Without WITH DEBUGGING MODE, §2002:6.2.7: "After all
      *> COPY and REPLACE statements have been processed, a debugging
      *> line has the characteristics of a comment line", and by
      *> §2002:7.1.2.3 GR13 the text the D-line COPY introduces is on
      *> debugging lines: FROMLIB does not print. Compiling the copied
      *> text prints FROMLIB. §2002:7.1.2.2 SR6 - "Pseudo-text-1 shall
      *> contain one or more text-words": the only text-word of
      *> pseudo-text-1 (AAA) is on a D line, which SR12 permits, and
      *> §2002:7.1.2.3 GR10c6 "Text-words within a debugging line
      *> participate in the matching", so it is a text-word: G2CPA's
      *> DISPLAY AAA. becomes DISPLAY BBB. (NEW). A pseudo-text-1 read
      *> as empty is refused; an unmatched AAA prints OLD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY02.
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
           DISPLAY "MAIN".
      D    COPY G2CPL.
           COPY G2CPA REPLACING ==
      D    AAA
           == BY ==BBB==.
           DISPLAY "END".
           STOP RUN.
