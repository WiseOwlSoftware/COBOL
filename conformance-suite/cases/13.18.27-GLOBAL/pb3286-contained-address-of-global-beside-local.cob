*>--- conformance-case
*> id: pb3286-contained-address-of-global-beside-local
*> rules: [GR-13.18.27.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.27.4 GR2: "A statement in a program contained directly or indirectly within a program
      *> that describes a global name may reference that name without describing it again."  8.4.6.2.1: "A
      *> local name may be referenced only in the source element in which it is declared."  The container
      *> declares X twice: under R1 (local to the container) and under R2 GLOBAL.  In the contained
      *> program X can only mean R2's X, so SET P TO ADDRESS OF X addresses the GLOBAL record and DISPLAY V
      *> shows "GLOB".  The leg can fail if the contained head is resolved among ALL the container's names:
      *> it takes R1's X, drops it as not global, and R2 is never put on addressable storage.
      *> Expected output, derived from the rules: LOCL (the container's own R1), then GLOB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G1OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
          05 X PIC X(4) VALUE "LOCL".
       01 R2 GLOBAL.
          05 X PIC X(4) VALUE "GLOB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY R1
           CALL "PB3286-G1IN"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3286-G1IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 V PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X
           SET ADDRESS OF V TO P
           DISPLAY V
           GOBACK.
       END PROGRAM PB3286-G1IN.
       END PROGRAM PB3286-G1OUT.
