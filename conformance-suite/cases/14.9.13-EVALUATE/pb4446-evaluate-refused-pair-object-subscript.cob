*>--- conformance-case
*> id: pb4446-evaluate-refused-pair-object-subscript
*> rules: [SR-14.9.13.3-10, SR-8.4.2.3.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4446. ISO 14.9.13.3 SR10: "The permissible combinations of selection subject and selection object operands
*> are indicated in Table 15", and Table 15 leaves an identifier object blank against the TRUE selection subject, so the
*> pair is refused (COBOL1634). That is a fault of the PAIR. The object's own reference is a separate one: 8.4.2.3.3 SR2,
*> "If a subscript is specified, the data description entry describing qualified-data-name-1 or the conditional variable
*> associated with qualified-condition-name-1 shall contain an OCCURS clause or shall be subordinate to a data
*> description entry that contains an OCCURS clause", refuses RELF (1) over the scalar 01 RELF, whatever it is paired
*> with (and 8.4.6.2.1 3) a) makes the contained program's own RELF the referenced item over its container's GLOBAL
*> level-88 of the spelling). 4.2.2 requires the warning mechanism "to indicate violations of the general formats and
*> the explicit syntax rules", so BOTH are indicated. Fails if the Table 15 refusal hides the COBOL2096 (the refusal
*> used to return before the object was bound), or if the program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4446A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 K PIC 9 VALUE 1.
             88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4446B".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4446B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           EVALUATE TRUE
               WHEN RELF (1)
                   DISPLAY "B"
           END-EVALUATE.
           EXIT PROGRAM.
       END PROGRAM PB4446B.
       END PROGRAM PB4446A.
