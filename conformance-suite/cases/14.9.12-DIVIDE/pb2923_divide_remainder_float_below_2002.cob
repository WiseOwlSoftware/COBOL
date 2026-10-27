*>--- conformance-case
*> id: pb2923_divide_remainder_float_below_2002
*> rules: [GR-14.9.12.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> PB2923 - the negative twin of pb2923_divide_remainder_float_usage and pb2923_divide_remainder_float_edited.
      *> The floating-point usages (FLOAT-LONG, FLOAT-SHORT) and the floating-point numeric-edited PICTURE (symbol E) are
      *> COBOL-2002 introductions, so at COBOL-85 the data items that are the receivers of the quotient are refused
      *> (COBOL0900, an edition gate) and no rule of 14.9.12.4 6) c) applies to the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2923FL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FL   USAGE FLOAT-LONG.
       01 FE   PIC +9.99E+99.
       01 R5   PIC S9(5)V9(6).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 1000 BY 3 GIVING FL REMAINDER R5.
           DIVIDE 1000 BY 3 GIVING FE REMAINDER R5.
           STOP RUN.
       END PROGRAM PB2923FL85.
