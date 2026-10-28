      *>--- conformance-case
      *> id: pb3960-refmod-zero-length
      *> rules: [GR-8.4.3.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3960. ISO 8.4.3.3.4 5) c): a specified length is a
      *> positive nonzero integer (zero only under REF-MOD-ZERO-LENGTH,
      *> 7.3.23; cite.py --check OK). The figurative ZEROS is the integer 0
      *> (8.8.1.1; 8.3.3.6.4 GR4), so T(2:ZEROS) is the zero length that
      *> T(2:0) is, refused COBOL2670 at compile time (owner decision R60).
      *> Before the fix the screen never saw the ZEROS and the program
      *> compiled and aborted at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3960N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T    PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY T(2:ZEROS)
           STOP RUN.
