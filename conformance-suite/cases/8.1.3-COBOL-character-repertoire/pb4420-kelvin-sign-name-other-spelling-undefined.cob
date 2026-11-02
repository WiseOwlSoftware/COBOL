      *>--- conformance-case
      *> id: pb4420-kelvin-sign-name-other-spelling-undefined
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - ISO 8.1.3.2 4) b): "An uppercase extended
      *> letter is treated as though it were folded to its corresponding
      *> lowercase extended letter, if any, in accordance with the case
      *> mapping described in Annex C" (cite.py --check: OK). WORK-K
      *> names the item; WORK-K is a different user-defined word (the
      *> pair (212A,006B) is not applied, see pb4420-kelvin-sign-name-
      *> is-a-word-of-its-own), and no declaration gives it, so the
      *> reference identifies no resource (8.4.2.1, cite.py --check:
      *> OK): COBOL1639. The control is that program. It fails when the
      *> fold maps U+212A to k.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WORK-K PIC X(4) VALUE "KELV".
       PROCEDURE DIVISION.
           DISPLAY WORK-K.
           STOP RUN.
