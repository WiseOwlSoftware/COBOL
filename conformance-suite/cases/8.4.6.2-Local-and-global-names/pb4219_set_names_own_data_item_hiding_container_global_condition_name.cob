*>--- conformance-case
*> id: pb4219_set_names_own_data_item_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 review R1-1 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source
*> element B is the referenced item." PB4219S declares the condition-name RELF under its GLOBAL group G; PB4219T,
*> contained in it, declares its own numeric data item RELF and writes SET RELF TO TRUE. RELF names PB4219T's data
*> item, and 14.9.39.3 SR6 requires a condition-name associated with a conditional variable (COBOL1639, worded as a
*> data item that hides the container's condition-name). Fails if the statement is accepted and sets the container's
*> conditional variable K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 K PIC 9 VALUE 1.
             88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4219T".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           SET RELF TO TRUE.
           EXIT PROGRAM.
       END PROGRAM PB4219T.
       END PROGRAM PB4219S.
