*>--- conformance-case
*> id: pb4219_search_all_relation_names_own_data_item_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 review R1-1 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source
*> element B is the referenced item." PB4219W declares the GLOBAL table T whose KEY K carries the condition-name RELF;
*> PB4219X, contained in it, declares its own numeric data item RELF and writes SEARCH ALL E ... WHEN RELF (IX) = 1.
*> RELF names PB4219X's data item, which is not referenced in the KEY phrase of E (14.9.37.3 SR8, COBOL1965); it is
*> not a condition-name written as the comparison's receiving operand. Fails if the statement is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T GLOBAL.
          05 E OCCURS 3 ASCENDING KEY IS K INDEXED BY IX.
             10 K PIC 9.
                88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4219X".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           SEARCH ALL E AT END DISPLAY "END"
             WHEN RELF (IX) = 1 DISPLAY "HIT"
           END-SEARCH.
           EXIT PROGRAM.
       END PROGRAM PB4219X.
       END PROGRAM PB4219W.
