      *>--- conformance-case
      *> id: pb5932-sr4-linkage-initialize-dynamic-table-not-referenceable
      *> rules: [SR-13.7.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:13.7.3 SR4 (cite.py --check: OK): a linkage item "may be
      *> referenced within the procedure division of that source element
      *> if, and only if" it meets one of the rule's legs. L-REC is no
      *> USING operand, no redefinition or renaming of one, and not
      *> BASED, so INITIALIZE of its whole dynamic-capacity table L-TAB
      *> is refused with COBOL2746, as `MOVE SPACES TO L-E (1)` is. The
      *> whole-table arm of INITIALIZE identifies its operand without
      *> the ordinary reference path and compiled clean until kb/Work
      *> PB5932's review of step 6b's remainder slice 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932IA.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-REC.
           05  L-TAB OCCURS DYNAMIC CAPACITY IN L-CAP.
               10  L-E PIC X(4).
       01  L-OK PIC X.
       PROCEDURE DIVISION USING L-OK.
       P0.
           INITIALIZE L-TAB.
           GOBACK.
       END PROGRAM PB5932IA.
