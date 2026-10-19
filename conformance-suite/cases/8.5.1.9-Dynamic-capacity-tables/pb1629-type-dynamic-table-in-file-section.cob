*>--- conformance-case
*> id: pb1629-type-dynamic-table-in-file-section
*> rules: [SR-8.5.1.9.1-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1629 - ISO 8.5.1.9.1 item 3: a dynamic-capacity table
      *>   "may be defined in any place, other than the file section".
      *>   The TYPE clause's effect is "as though the data description
      *>   identified by type-name-1 had been coded in place"
      *>   (13.18.57.4 GR1), so the table TT brings into the record FREC is
      *>   in the file section though the type declaration, in
      *>   working-storage, is legal. The refusal is reported at the
      *>   record entry FREC that wrote the TYPE clause (the unit test
      *>   ReferenceSitePositionTests pins the line), not at the
      *>   declaration.
      *> cite.py --check 8.5.1.9.1 "it may be defined in any place, other
      *>   than the file section" -> OK  8.5.1.9.1 3)
      *> cite.py --check 13.18.57.4 "as though the data description
      *>   identified by type-name-1 had been coded in" -> OK  13.18.57.4 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1629C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1629c.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC TYPE TT.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF.
          05 T OCCURS DYNAMIC FROM 2 TO 5.
             10 E PIC X(3).
       PROCEDURE DIVISION.
           STOP RUN.
