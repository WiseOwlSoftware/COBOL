*>--- conformance-case
*> id: pb1654-aligned-type-entry
*> rules: [SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1654 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  The refusal is COBOL1942 either way; what this test pins is the
      *> WORDS of its subject description, which must name what the entry IS (an entry described with the TYPE clause, which 13.16.3 SR14 bars from carrying ALIGNED).
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002 (see pb487-aligned-non-bit-subject).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1654TYP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 A PIC X.
       01 X TYPE T ALIGNED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
