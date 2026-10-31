*>--- conformance-case
*> id: pb4209-set-pointer-to-zero-expression
*> rules: [SR-14.9.39.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR17: "Identifier-5 shall reference a data item of
      *> category data-pointer. Identifier-6 shall be of category data-pointer."
      *> (cite.py: OK  §14.9.39.3 17)). A ZERO + 1 sender is an arithmetic
      *> expression, not a data-pointer. kb/Work PB4209: the refusal quotes
      *> the sender AS THE PROGRAMMER WROTE IT, 'ZERO + 1'; it used to print
      *> TO 0+1 (the rewritten ZERO token glued to its neighbours).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4209C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET WS-P TO ZERO + 1
           STOP RUN.