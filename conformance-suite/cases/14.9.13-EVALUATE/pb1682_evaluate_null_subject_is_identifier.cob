      *>--- conformance-case
      *> id: pb1682_evaluate_null_subject_is_identifier
      *> rules: [SR-14.9.13.3-10, SR-8.4.3.10.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1682 - the predefined NULL is an IDENTIFIER (ISO 1989:2023 8.4.3.1.2 identifier Format 8,
      *> predefined-address), so Table 15 of 14.9.13.3 SR10 ("The permissible combinations of selection subject
      *> and selection object operands are indicated in Table 15") reads an EVALUATE NULL subject in its Identifier
      *> column. The compiler read the presence of the grammar's nonNumericLiteral node as "a literal" and refused
      *> EVALUATE NULL WHEN P (identifier subject, identifier object) on the literal/identifier row. The pairing
      *> is a pointer relation (8.8.4.2.3, the 14.9.13.4 GR4 a) 6. "selection-subject [NOT] = selection-object").
      *>   cite.py --check 14.9.13.3 "The permissible combinations of selection subject and selection object" -> OK
      *> EXPECTED OUTPUT, from the spec, never from a run (P is a pointer item; NULL is guaranteed not to be any
      *> item's address, 8.4.3.10.4 GR2, and is equal to a pointer that holds it):
      *>   L1 P holds NULL      EVALUATE NULL WHEN P  : NULL = P is true        -> L1 EQ
      *>   L3 P holds NULL      EVALUATE P WHEN NULL  : P = NULL is true        -> L3 EQ
      *>   L2 P holds X's addr  EVALUATE NULL WHEN P  : NULL = P is false       -> L2 OTHER
      *>   L4 P holds X's addr  EVALUATE P WHEN NULL  : P = NULL is false       -> L4 OTHER
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1682NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO NULL
           EVALUATE NULL
              WHEN P DISPLAY "L1 EQ"
              WHEN OTHER DISPLAY "L1 OTHER"
           END-EVALUATE
           EVALUATE P
              WHEN NULL DISPLAY "L3 EQ"
              WHEN OTHER DISPLAY "L3 OTHER"
           END-EVALUATE
           SET P TO ADDRESS OF X
           EVALUATE NULL
              WHEN P DISPLAY "L2 EQ"
              WHEN OTHER DISPLAY "L2 OTHER"
           END-EVALUATE
           EVALUATE P
              WHEN NULL DISPLAY "L4 EQ"
              WHEN OTHER DISPLAY "L4 OTHER"
           END-EVALUATE
           STOP RUN.
