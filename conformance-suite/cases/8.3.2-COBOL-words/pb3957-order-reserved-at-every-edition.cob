*>--- conformance-case
*> id: pb3957-order-reserved-at-every-edition
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3957 - ORDER is a reserved word at EVERY edition, COBOL-85 included. ISO 8.9 reserves it (cite.py:
      *> OK 8.9 "ORDER") and 8.3.2.1 1) - "Reserved words shall not be used as user-defined words or system-names"
      *> (cite.py: OK) - refuses a data item called ORDER. The 2002 standard's Annex F.1 item 6 ("The following reserved
      *> words have been added", printed page 835 of the post-ballot text, kb/Work PB3602 source P2) lists the 84 words
      *> 2002 reserved that the previous standard did not, and ORDER is not among them although the same text's 8.9
      *> list holds it, so the previous standard reserved it. scripts/gen-reserved-words.ps1 asserts that for every 8.9
      *> word. An earlier revision cleared ORDER at 85 on a corpus proof that did not exist: ST127A spells SORT's
      *> WITH DUPLICATES IN ORDER keyword, and no CCVS program declares a data item named ORDER.
      *> Expected: COBOL0901 naming ORDER at all four editions. It fails if COBOL-85 admits the word as a name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3957ORDR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ORDER PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY ORDER.
           STOP RUN.
