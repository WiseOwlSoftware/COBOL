*>--- conformance-case
*> id: pb4635-abs-parenthesized-alphanumeric
*> rules: [AR-15.7.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-1 - ISO §15.7.3 r1: "Argument-1 shall be of class numeric." (cite.py --check 15.7.3 OK). (X9) is "an
      *> arithmetic expression enclosed in parentheses" (§8.8.1.2 r1), and §8.8.1.1 admits "an identifier referencing a numeric
      *> data item" in one, so the alphanumeric item is refused as it is when written bare. The grouping parentheses must not
      *> turn the refusal into digit-decoding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGABSPARENALPHA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9    PIC X(9) VALUE "123456789".
       01 T     PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION ABS((X9)).
           STOP RUN.
