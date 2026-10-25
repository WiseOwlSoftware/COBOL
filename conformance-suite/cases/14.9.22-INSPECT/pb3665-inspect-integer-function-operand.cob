*>--- conformance-case
*> id: pb3665-inspect-integer-function-operand
*> rules: [SR-14.9.22.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - ISO 14.9.22.3 SR2: "Identifier-3, ..., identifier-n shall reference an elementary item
*> described implicitly or explicitly as usage display or national." The TALLYING FOR operand is one of them; a
*> function-identifier is the data item its 15.2 type describes (8.4.3.2.1), and 15.2 5) states no usage for an
*> integer function, so FUNCTION INTEGER(N) is not described as usage display or national. The statement
*> compiled clean and examined the digits of the value. STRING SR1 is the same sentence (pb3665-string-*).
*> Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665II.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 S PIC X(8) VALUE "ab7cd7ef".
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S TALLYING T FOR ALL FUNCTION INTEGER(N)
           STOP RUN.
