*>--- conformance-case
*> id: pb4606-evaluate-surplus-object-bare-operand-subscript
*> rules: [SR-14.9.13.3-2, SR-8.4.2.3.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4606. ISO 14.9.13.3 SR2: "The number of selection objects within each set of selection objects shall be
*> equal to the number of selection subjects." The WHEN phrase writes two objects for one subject (COBOL2106). The
*> surplus object "> 1 AND WS-N (1)" has no subject to pair with, but its succeeding bare operand WS-N (1) is still a
*> reference (8.8.4.12.4 GR1 makes it the object of the abbreviated relation "> WS-N (1)"). 8.4.2.3.3 SR2, "If a
*> subscript is specified, the data description entry describing qualified-data-name-1 or the conditional variable
*> associated with qualified-condition-name-1 shall contain an OCCURS clause or shall be subordinate to a data
*> description entry that contains an OCCURS clause", refuses the subscript on the scalar 01 WS-N, and 4.2.2 requires
*> the warning mechanism "to indicate violations of the general formats and the explicit syntax rules". Fails if the
*> count breach hides the COBOL2096 (the standalone walk of the surplus object skipped a bare operand), or if the
*> program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4606B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           EVALUATE WS-N
               WHEN 1 ALSO > 1 AND WS-N (1)
                   DISPLAY "B"
           END-EVALUATE.
           STOP RUN.
