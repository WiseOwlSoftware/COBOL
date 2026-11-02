*>--- conformance-case
*> id: pb4219_own_condition_name_hides_container_global_data_item
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 (the data-name / condition-name sibling) - ISO 8.4.6.2.1 1): the set of names is "all names that are
*> defined in source element B and all global names" of its containers, and 3) a): "If the name is declared in source
*> element B, the item in source element B is the referenced item." PB4219F declares the data item RELF IS GLOBAL;
*> PB4219G, contained in it, declares its own condition-name RELF and writes DISPLAY RELF. PB4219G's condition-name is
*> the referenced item, and it identifies no data item (COBOL1639, "is a condition-name"). Fails if the statement is
*> accepted and displays the container's 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF PIC 9 VALUE 7 GLOBAL.
       PROCEDURE DIVISION.
           CALL "PB4219G".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           DISPLAY RELF.
           EXIT PROGRAM.
       END PROGRAM PB4219G.
       END PROGRAM PB4219F.
