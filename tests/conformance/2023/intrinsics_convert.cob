      *> ISO §15.19 CONVERT (2023). ANUM<->HEX (NOTE 3 a/b), HEX->BYTE (r5), NAT->ANUM HEX (r2 over UTF-16BE),
      *> and the ANUM->NAT->ANUM repertoire round-trip (r1/r3). The alphanumeric coded set is 8-bit Latin-1
      *> (code point == byte); national is UTF-16, one char/position (D-N1). See §15.19.4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRCONV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A   PIC X    VALUE "A".
       01 WS-HX  PIC XX   VALUE "41".
       01 WS-N   PIC N    VALUE N"A".
       01 WS-R   PIC X(8).
       01 WS-NR  PIC N(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONVERT(WS-A ANUM ANUM HEX) TO WS-R.
           DISPLAY "AH=" WS-R.
           MOVE FUNCTION CONVERT(WS-HX HEX ANUM) TO WS-R.
           DISPLAY "HA=" WS-R.
           MOVE FUNCTION CONVERT(WS-HX HEX BYTE) TO WS-R.
           DISPLAY "HB=" WS-R.
           MOVE FUNCTION CONVERT(WS-N NAT ANUM HEX) TO WS-R.
           DISPLAY "NH=" WS-R.
           MOVE FUNCTION CONVERT(WS-A ANUM NAT) TO WS-NR.
           MOVE FUNCTION CONVERT(WS-NR NAT ANUM) TO WS-R.
           DISPLAY "RT=" WS-R.
           STOP RUN.
       END PROGRAM INTRCONV.
