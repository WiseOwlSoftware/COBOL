*>--- conformance-case
*> id: pb3665-string-boolean-function-sender
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - 15.2 2): "Boolean functions have an implicit usage bit", which is neither display nor
*> national, so FUNCTION BOOLEAN-OF-INTEGER is no STRING sender under ISO 14.9.43.3 SR1 (it compiled and sent
*> the bit string "0111"). Boolean functions are COBOL-2002, so 85 refuses the function itself instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           STRING FUNCTION BOOLEAN-OF-INTEGER(N 4)
               DELIMITED BY SIZE INTO R
           STOP RUN.
