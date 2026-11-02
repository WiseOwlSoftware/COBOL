*>--- conformance-case
*> id: pb4433_if_subscripts_own_scalar_data_item_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4433 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is the
*> referenced item." PB4433S declares the condition-name RELF under its GLOBAL group G; PB4433T, contained in it, declares
*> its own scalar data item RELF and writes IF RELF (1). A bare operand of IF admits a data item, so RELF (1) is PB4433T's
*> item with a subscript, and 8.4.2.3.3 SR2 ("If a subscript is specified, the data description entry describing
*> qualified-data-name-1 ... shall contain an OCCURS clause or shall be subordinate to a data description entry that
*> contains an OCCURS clause") refuses it (COBOL2096), the verdict the same reference draws with no level-88 of the
*> spelling. Fails if the reference is told the position requires a condition-name (COBOL1639), or is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4433S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 K PIC 9 VALUE 1.
             88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4433T".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4433T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           IF RELF (1)
               DISPLAY "A"
           END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4433T.
       END PROGRAM PB4433S.
