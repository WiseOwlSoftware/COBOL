      *>--- conformance-case
      *> id: g85_x01
      *> rules: [1985-SR-X.4.6.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.6.3 SR2 (THE GLOBAL CLAUSE). A refusal: "In the same
      *> Data Division, the data description entries for any two data
      *> items for which the same data-name is specified must not
      *> include the GLOBAL clause." Lines 5 and 6 of the program below
      *> both describe G85X01-ITEM with the GLOBAL clause in one
      *> Working-Storage Section; WiseOwl refuses line 6 at 85 with
      *> COBOL6862. A compiler that accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G85X01-ITEM GLOBAL PIC X VALUE "A".
       01  G85X01-ITEM GLOBAL PIC X VALUE "B".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ACCEPTED".
           STOP RUN.
