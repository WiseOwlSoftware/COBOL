*>--- conformance-case
*> id: pb3228-string-bit-group-sender
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3228 - ISO 14.9.43.3 SR1: "all identifiers, except identifier-4, shall be described implicitly or
*> explicitly as usage display or national." A GROUP-USAGE BIT group "is treated as though it were an elementary
*> data item of usage bit and class and category boolean" (13.18.29.4 GR1 b), so identifier-1 here is described as
*> usage bit and the statement is a syntax error. STRING's private re-derivation of the usage sentence read only
*> elementary PICTUREs and strongly-typed groups, so the bit group fell through and the bit string was sent as
*> characters; INSPECT, asking the one usage predicate, refused the same group (pb856-inspect-identifier1-bit-group).
*> GROUP-USAGE BIT is COBOL-2002, so 85 refuses the clause itself instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3228S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 B1 PIC 1(8) VALUE B"01000001".
          05 B2 PIC 1(8) VALUE B"01000010".
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           STRING BG DELIMITED BY SIZE INTO R
           STOP RUN.
