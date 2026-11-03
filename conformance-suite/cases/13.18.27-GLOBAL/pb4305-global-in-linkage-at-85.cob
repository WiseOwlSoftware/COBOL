      *>--- conformance-case
      *> id: pb4305-global-in-linkage-at-85
      *> rules: [SR-13.18.27.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_global_in_linkage_2002. COBOL-85 did not allow GLOBAL
      *>   in the linkage section (2002 Annex F.2 item 61). Expected:
      *>   COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NGL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X GLOBAL.
       PROCEDURE DIVISION USING L.
       MAIN-P.
           EXIT PROGRAM.
