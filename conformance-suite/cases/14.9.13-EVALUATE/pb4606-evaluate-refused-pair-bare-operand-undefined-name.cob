*>--- conformance-case
*> id: pb4606-evaluate-refused-pair-bare-operand-undefined-name
*> rules: [SR-14.9.13.3-10, GR-8.8.4.12.4-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4606. ISO 14.9.13.3 SR10: "The permissible combinations of selection subject and selection object operands
*> are indicated in Table 15", and Table 15 marks the TRUE selection subject with a partial-expression object invalid,
*> so the pair is refused (COBOL1634). That is a fault of the PAIR. The partial-expression's succeeding bare operand is
*> a reference of its own: 8.8.4.12.4 GR1, "The effect of using abbreviations is as if the last preceding stated
*> subject were inserted in place of the omitted subject, and the last stated relational operator were inserted in
*> place of the omitted relational operator", makes UNDEFX in "> 1 AND UNDEFX" the object of the relation "> UNDEFX",
*> and 8.4.2.1, "In order to use a resource, a statement shall contain a reference that uniquely identifies that
*> resource", is broken by UNDEFX, which no declaration gives. 4.2.2 requires the warning mechanism "to indicate
*> violations of the general formats and the explicit syntax rules", so BOTH are indicated. Fails if the Table 15
*> refusal hides the COBOL1639 (the standalone walk of the refused object skipped a bare operand), or if the program
*> is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4606A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN > 1 AND UNDEFX
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
