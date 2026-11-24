*>--- conformance-case
*> id: pb5709-constant-length-data-name-subscript-from-2014
*> rules: [SR-13.10.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> 13.10.3 SR3: "All subscripts of data-name-1 and data-name-2 shall be literals." (kb/Work PB5709) The
      *> rule first stands in 2014 (13.10.2 SR3); COBOL-2002's constant entry has none, so the same program is
      *> legal there (13.9@2002-CONSTANT-ENTRY/pb5709-constant-length-data-name-subscript). The subscript I is
      *> a data-name, not a literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5709NO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 2.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K CONSTANT AS LENGTH OF E (I).
       PROCEDURE DIVISION.
           DISPLAY "K=" K
           STOP RUN.
