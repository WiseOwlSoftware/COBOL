*>--- conformance-case
*> id: pb4432_if_names_own_capacity_register_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432AE declares the GLOBAL item X with the condition-name CAP;
*> PB4432AF, contained in it, declares its own dynamic table whose CAPACITY IN phrase names the register CAP (ISO
*> 13.18.38.3 SR30: a data-name of its entry's source element) and writes IF CAP. In PB4432AF, CAP is that numeric
*> register, which is none of the conditions of 8.8.4 (COBOL2318). Fails if the statement is accepted and tests the
*> container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 1.
          88 CAP VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432AF".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC X OCCURS DYNAMIC CAPACITY IN CAP.
       PROCEDURE DIVISION.
           IF CAP DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432AF.
       END PROGRAM PB4432AE.
