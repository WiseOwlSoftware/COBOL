*>--- conformance-case
*> id: pb4432_display_names_own_condition_name_hiding_container_global_capacity_register
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 (the capacity-register sibling) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432M declares the GLOBAL record TBL whose dynamic table names the
*> capacity register CAP (ISO 13.18.38.3 SR30: a data-name of its entry's source element); PB4432N, contained in it,
*> declares its own condition-name CAP and writes DISPLAY CAP. PB4432N's condition-name is the referenced item, and it
*> identifies no data item (COBOL1639). Fails if the statement is accepted and displays the container's register.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL IS GLOBAL.
          05 WE PIC 9(3) OCCURS DYNAMIC CAPACITY IN CAP FROM 2 TO 4.
       PROCEDURE DIVISION.
           CALL "PB4432N".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 CAP VALUE 1.
       PROCEDURE DIVISION.
           DISPLAY CAP.
           GOBACK.
       END PROGRAM PB4432N.
       END PROGRAM PB4432M.
