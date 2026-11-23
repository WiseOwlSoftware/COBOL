      *>--- conformance-case
      *> id: pb5871-assign-using-alphabetic-refused
      *> rules: [SR-12.4.5.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5871 - §2023:12.4.5.2 SR7: "Data-name-1 shall
      *>   reference an alphanumeric data item and shall not be
      *>   subordinate to the file description entry for file-name-1."
      *>   WS-ALPHA is PIC A, category alphabetic, a category of its
      *>   own (§2023:8.5.2.1 Table 2), and from COBOL-2014 on no
      *>   sentence treats an alphabetic item as alphanumeric
      *>   (§2014:E.2). Expected: COBOL1810 at 2014 and 2023; the 2002
      *>   twin pb5871-assign-using-alphabetic-2002 accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5871R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F4 ASSIGN USING WS-ALPHA.
       DATA DIVISION.
       FILE SECTION.
       FD  F4.
       01  R4          PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-ALPHA    PIC A(11) VALUE "PB5871RSGNA".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F4.
           MOVE "FOUR" TO R4.
           WRITE R4.
           CLOSE F4.
           MOVE SPACES TO R4.
           OPEN INPUT F4.
           READ F4 AT END DISPLAY "F4 EMPTY".
           DISPLAY "F4 ALPHABETIC " R4.
           CLOSE F4.
           STOP RUN.
