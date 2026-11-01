*>--- conformance-case
*> id: pb4308-byte-length-physical-below-2014
*> rules: [FMT-15.14.2]
*> source-format: free
*> portable: unverified
*>---
      *> The BYTE-LENGTH half of pb4308-length-physical-below-2014 (kb/Work PB4308). FUNCTION BYTE-LENGTH is a 2002
      *> function (15.14.2: ( argument-1 [ PHYSICAL ] ) from 2014), so at 2002 the function is accepted and only the
      *> PHYSICAL phrase is refused by edition (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4308BLB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION BYTE-LENGTH(S PHYSICAL)
           STOP RUN.
