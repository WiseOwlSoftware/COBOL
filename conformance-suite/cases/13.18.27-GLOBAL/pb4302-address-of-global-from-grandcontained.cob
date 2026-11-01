*>--- conformance-case
*> id: pb4302-address-of-global-from-grandcontained
*> rules: [GR-13.18.27.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.27.4 GR2: "A statement in a program contained directly or indirectly within a program that describes a
      *> global name may reference that name without describing it again."  INDIRECTLY: the program that describes GLOBAL R
      *> is OUTER, the program that references it is INNER, and MIDDLE, which sits between them, describes nothing and
      *> references nothing.  The storage INNER's ADDRESS OF takes is OUTER's, so OUTER's R must live where an address can be
      *> taken (8.4.3.11) and INNER's store through the pointer must be the value OUTER reads back.  Expected output: NEWV.
      *> Why each leg can fail: if OUTER's claim looked only at its DIRECT children (MIDDLE), R would stay in ordinary
      *> storage and the SET in INNER would be refused or would address a copy, so OUTER would print OLDV; if MIDDLE
      *> (no GLOBAL name of its own) claimed the cell, R would be declared twice in the generated type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4302-OUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R GLOBAL.
          05 X PIC X(4) VALUE "OLDV".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB4302-MIDDLE"
           DISPLAY X
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4302-MIDDLE.
       PROCEDURE DIVISION.
           CALL "PB4302-INNER"
           GOBACK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4302-INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 V PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X
           SET ADDRESS OF V TO P
           MOVE "NEWV" TO V
           GOBACK.
       END PROGRAM PB4302-INNER.
       END PROGRAM PB4302-MIDDLE.
       END PROGRAM PB4302-OUTER.
