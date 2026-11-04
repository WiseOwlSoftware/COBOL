*>--- conformance-case
*> id: pb1654-aligned-strong-typedef
*> rules: [SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1654 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  The refusal is COBOL1942 either way; what this test pins is the
      *> WORDS of its subject description, which must name what the entry IS (a group of a STRONG type declaration, which no GROUP-USAGE BIT clause can make a bit group (13.18.29.3 SR1; 8.5.3.1)).
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002 (see pb487-aligned-non-bit-subject).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1654STR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG ALIGNED.
          05 A PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
