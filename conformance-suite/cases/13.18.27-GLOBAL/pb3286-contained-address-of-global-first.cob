*>--- conformance-case
*> id: pb3286-contained-address-of-global-first
*> rules: [GR-13.18.27.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.27.4 GR2 and 8.4.6.2.1, as in pb3286-contained-address-of-global-beside-local, with the two
      *> records declared in the OTHER order: the GLOBAL R2 first, the container-local R1 second.  The answer
      *> of a contained program's X does not depend on declaration order (it is R2's X, the only global one),
      *> so this is the control the first order is read against.  Expected output: GLOB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G2OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2 GLOBAL.
          05 X PIC X(4) VALUE "GLOB".
       01 R1.
          05 X PIC X(4) VALUE "LOCL".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3286-G2IN"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G2IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 V PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X
           SET ADDRESS OF V TO P
           DISPLAY V
           GOBACK.
       END PROGRAM PB3286-G2IN.
       END PROGRAM PB3286-G2OUT.
