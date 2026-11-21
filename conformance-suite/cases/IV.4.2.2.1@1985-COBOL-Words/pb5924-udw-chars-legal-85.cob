      *>--- conformance-case
      *> id: pb5924-udw-chars-legal-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "may not appear as the first or last character"
      *> A user-defined word of letters (either case), digits and inner
      *> hyphens, beginning with a digit, is accepted and resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWCHR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1-B2-C3          PIC X(3) VALUE "ABC".
       01  9TH-ITEM          PIC X(3) VALUE "DEF".
       01  item-x            PIC X(3) VALUE "GHI".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY a1-b2-c3.
           DISPLAY 9TH-ITEM.
           DISPLAY ITEM-X.
           STOP RUN.
