*>--- conformance-case
*> id: pb494-typedef-sync-group-below-2023
*> rules: [SR-13.18.55.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB494 - SYNCHRONIZED on a group item is a COBOL-2023 introduction (Annex E.3.2 item 6: "This clause
      *> may now be specified for a group level data item"), and a TYPE DECLARATION's group is a group item: the 2023
      *> text of 13.18.55.3 SR1, "The SYNCHRONIZED clause may be specified for group and elementary items"
      *> (cite.py: OK  13.18.55.3 1)), is what admits it. TYPEDEF is a COBOL-2002 clause, so the TYPEDEF arm is
      *> refused at 2002 and 2014 with COBOLNET0900, exactly as conformance:negative/sync_group_below_2023 refuses a
      *> storage group. Before the fix the gate ran only over the storage forest, which a type declaration is not
      *> part of, so this program compiled and ran at 2002 and 2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P494T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TG TYPEDEF SYNC.
           05  A PIC S9(4) COMP.
           05  B PIC X.
       01  V TYPE TG.
       PROCEDURE DIVISION.
           MOVE 5 TO A OF V
           DISPLAY "A=" A OF V
           STOP RUN.
