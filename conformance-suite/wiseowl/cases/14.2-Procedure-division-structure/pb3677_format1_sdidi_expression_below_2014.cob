*>--- conformance-case
*> id: pb3677_format1_sdidi_expression_below_2014
*> rules: [GR-14.2.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3677, the negative of pb3677_format1_sdidi_expression: ARITHMETIC IS STANDARD-DECIMAL, which makes the
      *> argument expression an SDIDI value, is a COBOL-2014 introduction (OPTIONS paragraph, ISO 11.9), so it is refused at
      *> COBOL-2002 and COBOL-85 by name (COBOL0900) and accepted from 2014 on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3677BM.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "N"
           STOP RUN.
       END PROGRAM PB3677BM.
