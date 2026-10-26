*>--- conformance-case
*> id: pb3441-vlg-national-member-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3441 - the variable-length group that holds a dynamic-length NATIONAL item and a dynamic-capacity table
      *> of national elements (the pb3441_vlg_display_national_members program) is a COBOL-2014 program: the DYNAMIC
      *> LENGTH clause and the OCCURS DYNAMIC clause that make the group variable-length (ISO 8.5.1.12.1, 13.18.19,
      *> 13.18.38 Format 4) are 2014 additions, so editions 85 and 2002 reject them with the edition-band diagnostic
      *> COBOL0900 BEFORE the group's display format (14.9.11.4 GR7) is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GD PIC N DYNAMIC LENGTH LIMIT IS 10.
          05 GT PIC N(2) OCCURS DYNAMIC CAPACITY IN GC FROM 1.
          05 GN PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "[" G "]"
           STOP RUN.
       END PROGRAM PB3441NEG.
