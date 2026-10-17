*>--- conformance-case
*> id: pb3228-string-bit-group-receiver
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3228 - ISO 14.9.43.3 SR1 reaches identifier-3 (the INTO receiver): "all identifiers, except
*> identifier-4, shall be described implicitly or explicitly as usage display or national." A GROUP-USAGE BIT group
*> is treated as an elementary item of usage bit (13.18.29.4 GR1 b), so STRING ... INTO a bit group is a syntax
*> error. The same function screens the sending and the receiving positions (kb/Work PB664).
*> GROUP-USAGE BIT is COBOL-2002, so 85 refuses the clause itself instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3228S3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 B1 PIC 1(8) VALUE B"00000000".
       01 S PIC X(1) VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           STRING S DELIMITED BY SIZE INTO BG
           STOP RUN.
