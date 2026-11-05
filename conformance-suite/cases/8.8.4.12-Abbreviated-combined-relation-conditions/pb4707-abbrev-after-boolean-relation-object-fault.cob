*>--- conformance-case
*> id: pb4707-abbrev-after-boolean-relation-object-fault
*> rules: [SR-8.8.4.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4707. `B1 = B2` involves operands of class boolean, a boolean relation condition (8.8.4.2.1), so 8.8.4.12.3
*> SR1, "Relation-condition-1 shall not be a boolean relation condition.", refuses the abbreviation `OR UNDEFX`:
*> COBOL2723. UNDEFX is a second, independent fault of the same condition: no declaration gives the name, so the reference
*> identifies no resource (8.4.2.1, COBOL1639), and 4.2.2 requires the warning mechanism "to indicate violations of the
*> general formats and the explicit syntax rules". The refusal used to return before it bound the abbreviation's
*> object, so COBOL1639 stayed hidden behind it. Fails if COBOL1639 is not indicated, or if the program is accepted.
*>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4707E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
       MAIN.
           IF B1 = B2 OR UNDEFX
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
