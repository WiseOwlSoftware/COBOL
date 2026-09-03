      *> reject-at: 2002 2014 2023
      *> ISO 14.9.11 - DISPLAY format 2 (screen), item 9 - the MINIMAL shape, which used to PRINT the screen record
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42DSP1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X FROM WS-X.
       PROCEDURE DIVISION.
           DISPLAY SG END-DISPLAY.
           STOP RUN.
