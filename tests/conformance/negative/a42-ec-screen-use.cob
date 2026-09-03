      *> reject-at: 2002 2014 2023
      *> ISO 14.9.49 - the EC-SCREEN family in a USE statement declarative (item 10).
      *> This line first read "14.6.4", which is Item identification; cite.py --check caught it.
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ECU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-SCREEN-STARTING-COLUMN.
       D-P.
           CONTINUE.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           STOP RUN.
