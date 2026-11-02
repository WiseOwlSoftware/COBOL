*>--- conformance-case
*> id: pb4219_own_data_item_hides_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 (the data-name / condition-name sibling) - ISO 8.4.6.2.1 3) a): "If the name is declared in source
*> element B, the item in source element B is the referenced item." PB4219H declares the condition-name RELF under its
*> GLOBAL group G; PB4219J, contained in it, declares its own numeric data item RELF and writes IF RELF. PB4219J's data
*> item is the referenced item, and a numeric data item is no conditional expression (COBOL2318). Fails if the
*> statement is accepted and tests the container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 K PIC 9 VALUE 1.
             88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4219J".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           IF RELF DISPLAY "TRUE" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4219J.
       END PROGRAM PB4219H.
