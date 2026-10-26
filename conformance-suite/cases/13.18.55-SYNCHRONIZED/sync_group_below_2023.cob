*>--- conformance-case
*> id: sync_group_below_2023
*> rules: [SR-13.18.55.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> SYNCHRONIZED on a GROUP item (ISO Annex E.3.2 item 6 / VCR row 43) is a COBOL-2023 introduction; below
      *> 2023 it is refused with COBOL0900 (P3 step 10 — it was silently accepted). The refusal is a hard error
      *> under --permissive too (CA14, owner-approved option (a)): --permissive is the migration mode for constructs
      *> an edition REMOVED, and has no meaning for one the targeted edition has not yet acquired (kb/Work PB494; the
      *> TYPEDEF arm is conformance:negative/pb494-typedef-sync-group-below-2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G SYNCHRONIZED.
         05 A PIC X.
         05 B PIC X.
       PROCEDURE DIVISION.
       M. STOP RUN.
