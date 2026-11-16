*>--- conformance-case
*> id: pb5797-depending-container-counter-global
*> rules: [SR-13.18.38.3-18]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5797 - ISO 13.18.38.3 SR18: under a GLOBAL entry data-name-1 "shall be a global name and
      *> shall reference a data item that is described in the same data division". NG is global but is
      *> described in the CONTAINING program's data division, not the contained program's that holds T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797OG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG PIC 9 GLOBAL VALUE 2.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5797IG"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797IG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
           05 T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON NG.
       PROCEDURE DIVISION.
       Q1.
           EXIT PROGRAM.
       END PROGRAM PB5797IG.
       END PROGRAM PB5797OG.
