*>--- conformance-case
*> id: pb538-sync-left-right-differ
*> rules: [GR-8.5.3.1-1, GR-13.18.55.4-4, GR-13.18.55.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB538 - SYNC LEFT and SYNC RIGHT are two different SYNCHRONIZED clauses, so two declarations of one
      *> type-name that differ only in them are not equivalent.
      *> 8.5.3.1: "Each pair of corresponding elementary items shall have the same ALIGNED, BLANK WHEN ZERO, DYNAMIC
      *>   LENGTH, JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses"   cite.py: OK  8.5.3.1
      *> 13.18.55.4 GR4: "SYNCHRONIZED LEFT specifies that the elementary item is to be positioned such that it will
      *>   begin at the left byte of the natural boundary in which the elementary item is placed."
      *>   cite.py: OK  13.18.55.4 4)
      *> 13.18.55.4 GR5: "SYNCHRONIZED RIGHT specifies that the elementary item is to be positioned such that it will
      *>   terminate on the right byte of the natural boundary in which the elementary item is placed."
      *>   cite.py: OK  13.18.55.4 5)
      *> 14.8.2.2 2): "If either the formal parameter or the corresponding argument is a strongly-typed group item,
      *>   both shall be of the same type"   cite.py: OK  14.8.2.2 2)
      *> The prototype's B1 is SYNC LEFT and the caller's is SYNC RIGHT; everything else is the same. The two lay out
      *> identically under the one-byte natural boundary (docs/CONFORMANCE.md DOC-A.1-197), but the CLAUSES differ,
      *> so A is not of the same type as L and the CALL is refused COBOLNET1688 (before the fix the phrase was
      *> discarded at bind and the CALL compiled clean). The positive twin is 2023/pb539_sync_group_as_if_leaves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P538S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  X1   PIC X.
           05  B1   PIC S9(4) COMP SYNC LEFT.
       01  L TYPE TC.
       PROCEDURE DIVISION USING L.
       END PROGRAM P538S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P538.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P538S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  X1   PIC X.
           05  B1   PIC S9(4) COMP SYNC RIGHT.
       01  A TYPE TC.
       PROCEDURE DIVISION.
           CALL P538S USING A.
           STOP RUN.
       END PROGRAM P538.
