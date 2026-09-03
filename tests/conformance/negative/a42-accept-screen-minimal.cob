      *> reject-at: 2002 2014 2023
      *> ISO 14.9.1 - ACCEPT format 3 (screen), item 1 - the MINIMAL shape, token-identical to the format-1 device ACCEPT
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ACC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X TO WS-X.
       PROCEDURE DIVISION.
           ACCEPT SG END-ACCEPT.
           STOP RUN.
