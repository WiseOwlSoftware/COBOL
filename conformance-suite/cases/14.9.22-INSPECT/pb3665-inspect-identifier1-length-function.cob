*>--- conformance-case
*> id: pb3665-inspect-identifier1-length-function
*> rules: [SR-14.9.22.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 (review round 1, R1-1) - the folded FUNCTION LENGTH as INSPECT identifier-1. It is a
*> function-identifier (8.4.3.2.1) of an integer function, to which 15.2 5) gives no usage, so ISO 14.9.22.3
*> SR1 ("an elementary item described implicitly or explicitly as usage display or national") refuses it as it
*> refuses FUNCTION INTEGER (pb3665-inspect-identifier1-integer-function); the fold to a literal must not change
*> the answer. Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665I1L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC X(3).
          05 G-B PIC X(4).
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT FUNCTION LENGTH(G) TALLYING T FOR ALL "7"
           STOP RUN.
