*>--- conformance-case
*> id: pb3665-string-integer-function-delimiter
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - the DELIMITED BY position of pb3665-string-integer-function-sender: identifier-2 is an
*> identifier too, so ISO 14.9.43.3 SR1 ("all identifiers, except identifier-4, shall be described implicitly or
*> explicitly as usage display or national") refuses an integer function there for the same reason (15.2 5)
*> states no usage). Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING "ab" DELIMITED BY FUNCTION INTEGER(N) INTO R
           STOP RUN.
