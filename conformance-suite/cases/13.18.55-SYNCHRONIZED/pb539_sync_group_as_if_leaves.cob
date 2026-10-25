*>--- conformance-case
*> id: pb539_sync_group_as_if_leaves
*> rules: [GR-13.18.55.4-1, GR-8.5.3.1-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB539 - a SYNCHRONIZED clause written on a group is the clause of each subordinate elementary item.
      *> 13.18.55.4 GR1: "When this clause is specified for a group item, it is treated as though it had instead been
      *>   separately specified for each of the subordinate elementary items for which this clause is permitted."
      *>   cite.py: OK  13.18.55.4 1)  (General rules)
      *> 8.5.3.1: "Each pair of corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC
      *>   LENGTH, JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses"   cite.py: OK  8.5.3.1
      *> 14.8.2.2 2): "If either the formal parameter or the corresponding argument is a strongly-typed group item,
      *>   both shall be of the same type"   cite.py: OK  14.8.2.2 2)
      *> SYNCHRONIZED on a group item is a COBOL-2023 introduction (Annex E.3.2 item 6), so this is the 2023 witness.
      *> The prototype P539S declares TC with the clause on the GROUP; the caller declares TC with it on EACH LEAF,
      *> and the second pair does the same with SYNC LEFT. By GR1 each pair of declarations has the same SYNCHRONIZED
      *> clause on every pair of corresponding elementary items, so they are equivalent and both CALLs are legal
      *> (before the fix both were refused COBOLNET1688). P539S stores 42 into B1 and 7 into B2.
      *> G539 pins "for which this clause is permitted": a group-level VALUE forbids the clause on its subordinates
      *> (13.18.63.3 SR14, cite.py: OK  13.18.63.3 14)), so the group's clause reaches neither leaf and the entry is
      *> legal; its value is the literal "AB".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P539S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  TC TYPEDEF STRONG SYNC.
           05  X1   PIC X.
           05  B1   PIC S9(4) COMP.
       01  TD TYPEDEF STRONG SYNC LEFT.
           05  X2   PIC X.
           05  B2   PIC S9(4) COMP.
       01  L TYPE TC.
       01  M TYPE TD.
       PROCEDURE DIVISION USING L M.
       END PROGRAM P539S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P539.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P539S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  X1   PIC X SYNC.
           05  B1   PIC S9(4) COMP SYNC.
       01  TD TYPEDEF STRONG.
           05  X2   PIC X SYNC LEFT.
           05  B2   PIC S9(4) COMP SYNC LEFT.
       01  A TYPE TC.
       01  B TYPE TD.
       01  G539 SYNC VALUE "AB".
           05  GA   PIC X.
           05  GB   PIC X.
       PROCEDURE DIVISION.
           CALL P539S USING A B.
           DISPLAY "B1=" B1 OF A " B2=" B2 OF B.
           DISPLAY "G539=" G539.
           STOP RUN.
       END PROGRAM P539.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P539S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG SYNC.
           05  X1   PIC X.
           05  B1   PIC S9(4) COMP.
       01  TD TYPEDEF STRONG SYNC LEFT.
           05  X2   PIC X.
           05  B2   PIC S9(4) COMP.
       LINKAGE SECTION.
       01  L TYPE TC.
       01  M TYPE TD.
       PROCEDURE DIVISION USING L M.
           MOVE 42 TO B1 OF L.
           MOVE 7 TO B2 OF M.
           GOBACK.
       END PROGRAM P539S.
