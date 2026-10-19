*>--- conformance-case
*> id: pb1629-same-as-dynamic-table-in-file-section
*> rules: [SR-8.5.1.9.1-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1629 - ISO 8.5.1.9.1 item 3: a dynamic-capacity table
      *>   "may be defined in any place, other than the file section".
      *>   The SAME AS clause's effect is "as though the data description
      *>   identified by data-name-1 had been coded in place of the SAME AS
      *>   clause" (13.18.49.4 GR1), so the table of WS-SRC reaches the
      *>   group G of the file record FREC. The refusal is reported at G,
      *>   the entry that wrote the SAME AS clause (the unit test
      *>   ReferenceSitePositionTests pins the line), not at WS-SRC.
      *> cite.py --check 8.5.1.9.1 "it may be defined in any place, other
      *>   than the file section" -> OK  8.5.1.9.1 3)
      *> cite.py --check 13.18.49.4 "as though the data description
      *>   identified by data-name-1 had been coded in place" -> OK
      *>   13.18.49.4 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1629D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1629d.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC.
          05 A PIC X(2).
          05 G SAME AS WS-SRC.
       WORKING-STORAGE SECTION.
       01 WS-SRC.
          05 T OCCURS DYNAMIC FROM 2 TO 5.
             10 E PIC X(3).
       PROCEDURE DIVISION.
           STOP RUN.
