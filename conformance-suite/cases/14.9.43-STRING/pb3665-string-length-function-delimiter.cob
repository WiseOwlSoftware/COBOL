*>--- conformance-case
*> id: pb3665-string-length-function-delimiter
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - the DELIMITED BY position of pb3665-string-length-function-sender. The folded FUNCTION
*> LENGTH is identifier-2, a function-identifier (8.4.3.2.1), and 15.2 gives no integer function a usage, so
*> ISO 14.9.43.3 SR1 refuses it; the old diagnostic called it "a numeric literal". Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665LD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC X(3).
          05 G-B PIC X(4).
       01 R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING "ab" DELIMITED BY FUNCTION LENGTH(G) INTO R
           STOP RUN.
