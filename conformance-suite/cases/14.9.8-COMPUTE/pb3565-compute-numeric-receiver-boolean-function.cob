*>--- conformance-case
*> id: pb3565-compute-numeric-receiver-boolean-function
*> rules: [SR-14.9.8.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3565 - a sole boolean function-identifier is a boolean
      *> expression (ISO 8.8.2; 15.2 2), so the statement is COMPUTE Format
      *> 2, whose receiver "shall reference an elementary boolean data item"
      *> (ISO 14.9.8.3 SR2). The numeric receiver N is refused (COBOLNET1511),
      *> never stored into; Format 1 admits no boolean operand either
      *> (8.8.1.1), so there is no reading that accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3565NR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION BOOLEAN-OF-INTEGER(5, 8)
           STOP RUN.
