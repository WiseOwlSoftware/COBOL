*>--- conformance-case
*> id: pb4219_search_all_when_names_own_data_item_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 review R1-1 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source
*> element B is the referenced item." PB4219U declares the GLOBAL table T whose KEY K carries the condition-name RELF;
*> PB4219V, contained in it, declares its own numeric data item RELF and writes SEARCH ALL E ... WHEN RELF (IX). RELF
*> names PB4219V's data item, and a WHEN phrase without a relational operator is 14.9.37.2 Format 2's
*> condition-name-1 alternative (COBOL1639, worded as a data item that hides the container's condition-name). Fails if
*> the statement is accepted and searches on the container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T GLOBAL.
          05 E OCCURS 3 ASCENDING KEY IS K INDEXED BY IX.
             10 K PIC 9.
                88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4219V".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           SEARCH ALL E AT END DISPLAY "END"
             WHEN RELF (IX) DISPLAY "HIT"
           END-SEARCH.
           EXIT PROGRAM.
       END PROGRAM PB4219V.
       END PROGRAM PB4219U.
