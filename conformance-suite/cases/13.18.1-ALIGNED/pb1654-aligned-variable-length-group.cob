*>--- conformance-case
*> id: pb1654-aligned-variable-length-group
*> rules: [SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1654 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  The refusal is COBOL1942 either way; what this test pins is the
      *> WORDS of its subject description, which must name what the entry IS (a variable-length group, which 13.18.29.4 GR3 excludes from the alphanumeric group items (8.5.1.12.1)).
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002 (see pb487-aligned-non-bit-subject).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1654VAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G ALIGNED.
          05 A PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
