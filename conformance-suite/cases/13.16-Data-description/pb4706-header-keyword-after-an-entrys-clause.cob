      *>--- conformance-case
      *> id: pb4706-header-keyword-after-an-entrys-clause
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  The sibling of the name slot: the entry has its clause and lacks only its period.  From 2002
      *> LOCAL-STORAGE is reserved, and the header is what the period lets follow, so it is the missing period and no reserved
      *> word written as a name (COBOL0901).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X
       LOCAL-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
