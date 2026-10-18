*>--- conformance-case
*> id: pb3253-record-contains-bits-beside-dynamic-one-over
*> rules: [SR-13.18.43.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3253 - the BOUNDARY of ISO 1989 §13.18.43.3 SR3 ("No record description entry for the file may
      *> specify a number of bytes greater than integer-1") for a record of USAGE BIT items beside a DYNAMIC LENGTH
      *> item: the two 3-bit items share ONE byte (§8.5.1.6.3, GR4 of §13.18.43.4) and D takes at most its LIMIT 10,
      *> so the description is 1 + 10 = 11 bytes at its greatest - ONE over integer-1 = 10, and the entry is refused;
      *> the same entry under RECORD CONTAINS 11 is the positive golden
      *> conformance-suite/cases/13.18.43-RECORD/pb3253_record_contains_bits_beside_dynamic.cob. WHY IT CAN FAIL: a
      *> maximum that gave each bit item a byte of its own (12) would refuse this as well, so only the pair pins 11.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3253N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3253n1.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 10 CHARACTERS.
       01  R.
           05 B1 PIC 1(3) USAGE BIT.
           05 B2 PIC 1(3) USAGE BIT.
           05 D  PIC X DYNAMIC LENGTH LIMIT 10.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
