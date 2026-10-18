*>--- conformance-case
*> id: pb3253_record_contains_bits_beside_dynamic
*> rules: [SR-13.18.43.3-3, GR-13.18.43.4-8, GR-13.18.43.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3253 - ISO 1989 §13.18.43.3 SR3 ("No record description entry for the file may specify a number of
      *> bytes greater than integer-1") over a record that holds USAGE BIT items BESIDE a DYNAMIC LENGTH item.
      *> §13.18.43.4 GR8 b) sizes the record description at its MAXIMUM ("The maximum number of table elements
      *> described in the record is used in the summation above to determine the maximum number of bytes associated
      *> with the record description"), a dynamic-length item counting at its maximum size, the smallest of its LIMIT,
      *> its prefixed usage's largest integer and the implementor's maximum (§8.5.1.10.1), and GR4 counts a trailing
      *> partial byte whole ("the record size includes the entire byte in which that data item ends").
      *>
      *> DERIVATION: B1 and B2 are same-level USAGE BIT items of 3 bits each, so §8.5.1.6.3 places B2 in the byte B1
      *> starts - the bits take ONE byte, not two - and D takes at most its LIMIT 10: the description is 1 + 10 = 11
      *> bytes at its greatest, EXACTLY integer-1 of RECORD CONTAINS 11, so the entry is LEGAL. The negative
      *> conformance-suite/cases/13.18.43-RECORD/pb3253-record-contains-bits-beside-dynamic-one-over.cob states 10.
      *> Before kb/Work PB3253 the maximum re-summed each bit item as a byte of its own, 12, and this legal entry was
      *> refused (COBOLNET2010) - while the minimum, 1, already shared the byte.
      *>
      *> WHY EACH LEG CAN FAIL: a Format 1 file is plain 11-byte blocks (D-FRA (vi)), each member at the position it
      *> has at its maximum size; the members are overwritten with junk before every READ, so a value that is right
      *> can only have come from the file. R1 fills the bits and D; R2 has D empty (the minimum, 1 byte) and the
      *> bits at the other extreme, so a bit run that was not read back at the byte it was written in shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3253BD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3253bd.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 11 CHARACTERS.
       01  R.
           05 B1 PIC 1(3) USAGE BIT.
           05 B2 PIC 1(3) USAGE BIT.
           05 D  PIC X DYNAMIC LENGTH LIMIT 10.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE B"101" TO B1.
           MOVE B"011" TO B2.
           MOVE "HELLO" TO D.
           WRITE R.
           DISPLAY "W1 " FS.
           MOVE B"000" TO B1.
           MOVE B"110" TO B2.
           MOVE "" TO D.
           WRITE R.
           DISPLAY "W2 " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE B"111" TO B1.
           MOVE B"111" TO B2.
           MOVE "JUNKJUNKJU" TO D.
           READ F.
           DISPLAY "R1 " B1 " " B2 " D=[" D "] " FS.
           MOVE B"111" TO B1.
           MOVE B"111" TO B2.
           MOVE "JUNKJUNKJU" TO D.
           READ F.
           DISPLAY "R2 " B1 " " B2 " D=[" D "] " FS.
           CLOSE F.
           STOP RUN.
