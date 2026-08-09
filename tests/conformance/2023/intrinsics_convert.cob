      *> ISO §15.19 CONVERT (2023). ANUM<->HEX (NOTE 3 a/b), HEX->BYTE (r5), NAT->ANUM HEX (r2 over UTF-16BE),
      *> and the ANUM->NAT->ANUM repertoire round-trip (r1/r3). The alphanumeric coded set is 8-bit Latin-1
      *> (code point == byte); national is UTF-16, one char/position (D-N1). See §15.19.4.
      *> CS: NAT/ANUM/HEX/BYTE are §8.10 CONTEXT-SENSITIVE words - reserved only where CONVERT's format
      *> permits them, so a data item NAMED one of them is legal as argument-1 (PB59 / FMT-15.19.2).
      *> D9: 15.19.3 r5's NOTE puts the ANUM-source test on the REPRESENTATION, not the class - a numeric
      *> DISPLAY item is a valid string of characters from the alphanumeric set ("005" -> its hex).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRCONV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A   PIC X    VALUE "A".
       01 WS-HX  PIC XX   VALUE "41".
       01 WS-N   PIC N    VALUE N"A".
       01 WS-R   PIC X(8).
       01 WS-NR  PIC N(4).
       01 NAT    PIC XX   VALUE "41".
       01 WS-D   PIC 9(3) VALUE 5.
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
           MOVE FUNCTION CONVERT(NAT HEX ANUM) TO WS-R.
           DISPLAY "CS=" WS-R.
           MOVE FUNCTION CONVERT(WS-D ANUM ANUM HEX) TO WS-R.
           DISPLAY "D9=" WS-R.
           STOP RUN.
       END PROGRAM INTRCONV.
