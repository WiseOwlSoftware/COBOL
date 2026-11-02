*>--- conformance-case
*> id: pb4432_if_names_own_index_name_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432S declares the GLOBAL item X with the condition-name RELF;
*> PB4432T, contained in it, declares its own table T1 INDEXED BY RELF and writes IF RELF. In PB4432T, RELF is its
*> index-name, which is no identifier (COBOL1637), so the IF cannot take it as a condition. Fails if the statement is
*> accepted and tests the container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432T".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           IF RELF DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432T.
       END PROGRAM PB4432S.
