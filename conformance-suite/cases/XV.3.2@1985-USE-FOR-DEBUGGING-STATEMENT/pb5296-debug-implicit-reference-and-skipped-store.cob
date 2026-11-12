      *>--- conformance-case
      *> id: pb5296-debug-implicit-reference-and-skipped-store
      *> rules: [1985-GR-XV.3.2.4-5, 1985-GR-XV.3.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5296. §1985:XV.3.2.4 GR5: ALL REFERENCES OF runs the
      *> section "for every statement that explicitly references
      *> identifier-1", and an explicit reference is one where "the name
      *> of the referenced item is written in a Procedure Division
      *> statement" (§1985:IV.4.4.1): SET W-SIX TO TRUE writes only the
      *> condition-name, so D1 does not run for it, and DISPLAY W-X, which
      *> writes W-X, does. GR6: the section for the item alone runs after
      *> a statement that "explicitly references and causes the content
      *> of the data item ... to be changed": the ADD whose ON SIZE ERROR
      *> path left W-Y unchanged (§1985:VI.6.4.2) causes nothing, and the
      *> ADD that stored runs D2 once.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5296.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-X PIC 9 VALUE 5.
           88  W-SIX VALUE 6.
       01  W-Y PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF W-X.
       D1P.
           DISPLAY "D1 " DEBUG-NAME (1:3) " " DEBUG-CONTENTS (1:1).
       D2 SECTION.
           USE FOR DEBUGGING ON W-Y.
       D2P.
           DISPLAY "D2 " DEBUG-NAME (1:3) " " DEBUG-CONTENTS (1:1).
       END DECLARATIVES.
       M SECTION.
       M1.
           SET W-SIX TO TRUE.
           DISPLAY "SET DONE".
           DISPLAY W-X.
           ADD 9 TO W-Y ON SIZE ERROR DISPLAY "SIZE ERROR".
           ADD 1 TO W-Y.
           STOP RUN.
