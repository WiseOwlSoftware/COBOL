*>--- conformance-case
*> id: pb4433_evaluate_subscripts_own_scalar_data_item_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4433 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is the
*> referenced item." PB4433U declares the condition-name RELF under its GLOBAL group G; PB4433V, contained in it, declares
*> its own scalar data item RELF and writes EVALUATE RELF (1) WHEN 3. The selection subject is PB4433V's item with a
*> subscript, and 8.4.2.3.3 SR2 ("If a subscript is specified, the data description entry describing
*> qualified-data-name-1 ... shall contain an OCCURS clause or shall be subordinate to a data description entry that
*> contains an OCCURS clause") refuses it (COBOL2096), the verdict the same reference draws with no level-88 of the
*> spelling. Fails if the subject is classed as a condition-name (COBOL1639, or a Table 15 pairing error), or accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4433U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 K PIC 9 VALUE 1.
             88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4433V".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4433V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           EVALUATE RELF (1)
               WHEN 3
                   DISPLAY "B"
           END-EVALUATE.
           EXIT PROGRAM.
       END PROGRAM PB4433V.
       END PROGRAM PB4433U.
