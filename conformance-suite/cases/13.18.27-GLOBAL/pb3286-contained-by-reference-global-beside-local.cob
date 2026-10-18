*>--- conformance-case
*> id: pb3286-contained-by-reference-global-beside-local
*> rules: [GR-13.18.27.4-2, GR-14.2.3-8]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.27.4 GR2 and 8.4.6.2.1 as in pb3286-contained-address-of-global-beside-local; ISO 14.2.3 GR8:
      *> "If the argument is passed by reference, the activated runtime element operates as if the formal parameter
      *> occupies the same storage area as the argument."  The contained program G3IN passes the GLOBAL R2's X
      *> (R1's X is the container's local) BY REFERENCE to G3SUB, which stores "SUB!" and displays the GLOBAL record
      *> R2 BEFORE returning: sharing the one storage area, R2 already holds "SUB!"; a copy would show "GLOB" until
      *> the return.  Expected output: SUB!.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G3OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
          05 X PIC X(4) VALUE "LOCL".
       01 R2 GLOBAL.
          05 X PIC X(4) VALUE "GLOB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3286-G3IN"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G3IN.
       PROCEDURE DIVISION.
           CALL "PB3286-G3SUB" USING X
           GOBACK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G3SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC X(4).
       PROCEDURE DIVISION USING LK.
           MOVE "SUB!" TO LK
           DISPLAY R2
           GOBACK.
       END PROGRAM PB3286-G3SUB.
       END PROGRAM PB3286-G3IN.
       END PROGRAM PB3286-G3OUT.
