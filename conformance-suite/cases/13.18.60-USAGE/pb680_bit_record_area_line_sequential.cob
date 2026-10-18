*>--- conformance-case
*> id: pb680_bit_record_area_line_sequential
*> rules: [GR-13.18.60.4-5, GR-9.1.13.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB680 - the LINE SEQUENTIAL organization (COBOL-2023, ORGANIZATION clause 12.4.5.10.3 GR2) of a
      *> file whose record area is a USAGE BIT item (13.18.60.4 GR5), the line-delimited twin of
      *> pb680_bit_record_area_file_io. The record is ONE character wide (8 bits, 8.5.1.6.3, 8 bits to the
      *> character being the 8.1.2 implementor choice), the character is that of the packed bits, high-order bit
      *> first: 01000010 = 0x42 = 'B', ORD 67 (15.70.1; 'A' is 66). The byte is printable on purpose, because a
      *> line-delimited file's record terminator and its treatment of control characters are the implementor's.
      *> A sender that wrote the '0'/'1' carrier would read back ORD 49 or 50, and one that wrote the wrong width
      *> would not leave the AT END (9.1.13.4 item 1, status 10) where it is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB680BITLS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FL ASSIGN TO "pb680_ls.dat" ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FLX ASSIGN TO "pb680_ls.dat" ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD FL.
       01 LR PIC 1(8) USAGE BIT.
       FD FLX.
       01 LXR PIC X(1).
       WORKING-STORAGE SECTION.
       01 ST-B PIC XX.
       01 ST-X PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT FL.
           MOVE B"01000010" TO LR.
           WRITE LR.
           CLOSE FL.
           DISPLAY "LS-W=" ST-B.
           OPEN INPUT FLX.
           READ FLX.
           DISPLAY "LS-X1=" FUNCTION ORD(LXR).
           READ FLX AT END DISPLAY "LS-X2=" ST-X.
           CLOSE FLX.
           OPEN INPUT FL.
           READ FL.
           DISPLAY "LS-B1=[" LR "]".
           READ FL AT END DISPLAY "LS-B2=" ST-B.
           CLOSE FL.
           STOP RUN.
