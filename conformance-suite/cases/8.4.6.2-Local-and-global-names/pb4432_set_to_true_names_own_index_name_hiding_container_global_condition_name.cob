*>--- conformance-case
*> id: pb4432_set_to_true_names_own_index_name_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432U declares the GLOBAL item X with the condition-name RELF;
*> PB4432V, contained in it, declares its own table T1 INDEXED BY RELF and writes SET RELF TO TRUE. In PB4432V, RELF is
*> its index-name, not a condition-name (COBOL1639), so the condition-only position of SET Format 4 refuses it. Fails if
*> the statement is accepted and sets the container's X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 0.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432V".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           SET RELF TO TRUE.
           EXIT PROGRAM.
       END PROGRAM PB4432V.
       END PROGRAM PB4432U.
