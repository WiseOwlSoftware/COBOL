      *> reject-at: 2002 2014 2023
      *> ISO 13.18.4 - the BACKGROUND-COLOR clause (item 3)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42BGC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 BACKGROUND-COLOR IS 2.
       PROCEDURE DIVISION.
           STOP RUN.
