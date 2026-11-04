*>--- conformance-case
*> id: pb4603-aligned-national-boolean
*> rules: [SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4603 - ISO 13.18.1.3 syntax rule 1: "The ALIGNED clause may be specified only for a bit group
      *> item or an elementary bit data item."  An elementary bit data item is one described with USAGE BIT
      *> (13.18.60.4 general rule 5).  B is a boolean item (PICTURE 1(4)) described with USAGE NATIONAL, which
      *> 13.18.60.3 syntax rule 12 permits: "An elementary data item with usage national shall be described with a
      *> picture character-string that describes a boolean, national, national-edited, numeric, or
      *> numeric-edited data item."  So the entry is refused (COBOL1942), and what this test pins is the WORDS of its
      *> subject description: it names the item's category (a category-boolean item, 8.5.2.1 Table 2) and must not
      *> say its usage is DISPLAY, which is untrue of this entry.
      *> NOT rejected at 85: ISO 8.9 reserves ALIGNED only from 2002 (see pb487-aligned-non-bit-subject).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4603NBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 1(4) USAGE NATIONAL ALIGNED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
