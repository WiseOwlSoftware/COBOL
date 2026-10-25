*>--- conformance-case
*> id: pb3665-unstring-length-function-delimiter
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - the UNSTRING twin of pb3665-string-length-function-delimiter. ISO 14.9.48.3 SR2: identifier-2
*> and identifier-3 "shall reference data items of category alphanumeric or national"; the folded FUNCTION LENGTH
*> is a function-identifier (8.4.3.2.1) of category numeric (15.2 5)), so the statement is refused. It was
*> refused already, but as "a numeric literal" under SR1; the function is an identifier, so the diagnostic now
*> names SR2 and the category. Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665UL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC X(3).
          05 G-B PIC X(4).
       01 S PIC X(8) VALUE "ab7cd7ef".
       01 R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING S DELIMITED BY FUNCTION LENGTH(G) INTO R
           STOP RUN.
