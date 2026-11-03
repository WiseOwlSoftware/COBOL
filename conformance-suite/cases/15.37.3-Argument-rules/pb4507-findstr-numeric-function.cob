*>--- conformance-case
*> id: pb4507-findstr-numeric-function
*> rules: [AR-15.37.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 - ISO §15.37.3 r3: argument-3 "shall be an integer data item or integer literal". A nested function-identifier
      *> is a data item (§8.4.3.2.4 r1), but FUNCTION SQRT is a NUMERIC function (§15.2 item 6), not an integer function, so its
      *> temporary item is not an integer data item whatever one reference happens to yield (§8.4.3.2.3 SR11). The refusal is the
      *> §15.3 type 6 integer test now that the operand-shape predicate admits a function-identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGFINDSTRNUMFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9    PIC X(9) VALUE "ABCABCABC".
       01 T     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" FUNCTION SQRT(4)).
           STOP RUN.
