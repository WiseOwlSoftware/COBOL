      *>--- conformance-case
      *> id: pb4420-dotted-capital-i-name-other-spelling-undefined
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - ISO 8.1.3.2 4) b): "An uppercase extended
      *> letter is treated as though it were folded to its corresponding
      *> lowercase extended letter, if any, in accordance with the case
      *> mapping described in Annex C" (cite.py --check: OK). CUSTOMER-
      *> İD names the item; CUSTOMER-ID is a different user-defined word
      *> (the pair (0130,0069) is not applied, see pb4420-dotted-
      *> capital-i-name-is-a-word-of-its-own), and no declaration gives
      *> it, so the reference identifies no resource (8.4.2.1: "In order
      *> to use a resource, a statement shall contain a reference that
      *> uniquely identifies that resource", cite.py --check: OK):
      *> COBOL1639. The control is that program. It fails when the fold
      *> maps U+0130 to i.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CUSTOMER-İD PIC X(4) VALUE "DOTI".
       PROCEDURE DIVISION.
           DISPLAY CUSTOMER-ID.
           STOP RUN.
