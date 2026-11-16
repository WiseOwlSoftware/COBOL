*>--- conformance-case
*> id: pb5797-depending-container-counter-external
*> rules: [SR-13.18.38.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5797 - ISO 13.18.38.3 SR21: in an EXTERNAL record data-name-1 "shall reference a data
      *> item possessing the external attribute that is described in the same data division". NE has the
      *> external attribute but is described in the CONTAINING program's data division.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797OE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NREC EXTERNAL GLOBAL.
           05 NE PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE 2 TO NE
           CALL "PB5797IE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797IE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E EXTERNAL.
           05 T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON NE.
       PROCEDURE DIVISION.
       Q1.
           EXIT PROGRAM.
       END PROGRAM PB5797IE.
       END PROGRAM PB5797OE.
