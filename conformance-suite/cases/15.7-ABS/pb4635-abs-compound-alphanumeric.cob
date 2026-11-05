*>--- conformance-case
*> id: pb4635-abs-compound-alphanumeric
*> rules: [AR-15.7.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R1-1 (sibling) - ISO §15.7.3 r1: "Argument-1 shall be of class numeric." (cite.py --check 15.7.3 OK). X9 + 1 is
      *> an arithmetic expression (§8.4.3.2.3 SR8), and §8.8.1.1 admits "an identifier referencing a numeric data item" in one: the
      *> alphanumeric X9 is refused, not digit-decoded, as in COMPUTE T = X9 + 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGABSCOMPALPHA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9    PIC X(9) VALUE "123456789".
       01 T     PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION ABS(X9 + 1).
           STOP RUN.
