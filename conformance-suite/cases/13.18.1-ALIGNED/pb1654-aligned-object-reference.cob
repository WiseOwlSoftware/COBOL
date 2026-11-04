*>--- conformance-case
*> id: pb1654-aligned-object-reference
*> rules: [SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1654 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  The refusal is COBOL1942 either way; what this test pins is the
      *> WORDS of its subject description, which must name what the entry IS (an elementary item of category object-reference (8.5.2.1 Table 2), not the enum name 'objectreference').
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002 (see pb487-aligned-non-bit-subject).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1654ORF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N USAGE OBJECT REFERENCE ALIGNED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
