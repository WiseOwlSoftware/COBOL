      *>--- conformance-case
      *> id: pb4305-value-in-linkage-at-85
      *> rules: [GR-13.18.63.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_value_in_linkage_2002. COBOL-85 allowed no data-item
      *>   VALUE clause in the linkage section (2002 Annex F.2 item
      *>   145). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NVL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X VALUE "A".
       PROCEDURE DIVISION USING L.
       MAIN-P.
           EXIT PROGRAM.
