*>--- conformance-case
*> id: pb680-bit-record-area-below-2002
*> rules: [GR-13.18.60.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB680 - the NEGATIVE twin of pb680_bit_record_area_file_io. Boolean data (PICTURE symbol 1,
      *> USAGE BIT, 13.18.60.4 GR5) is a COBOL-2002 introduction, so a file record area described with it is
      *> refused at --std 85 with COBOLNET0900, exactly as the same item in WORKING-STORAGE is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB680NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO "pb680_neg.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FB.
       01 BR PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
           STOP RUN.
