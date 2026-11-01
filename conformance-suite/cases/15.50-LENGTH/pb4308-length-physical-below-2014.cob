*>--- conformance-case
*> id: pb4308-length-physical-below-2014
*> rules: [FMT-15.50.2]
*> source-format: free
*> portable: unverified
*>---
      *> The control of pb4308_length_physical_2014 (kb/Work PB4308). The printed 2002 format of FUNCTION LENGTH is
      *> ( argument-1 ); PHYSICAL joins it with 2014 (15.50.2: ( argument-1 [ PHYSICAL ] )), and below 2014 it is a
      *> user-defined word, so the phrase is refused by edition (COBOL0900) at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4308LPB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(S PHYSICAL)
           STOP RUN.
