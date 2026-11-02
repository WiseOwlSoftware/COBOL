*>--- conformance-case
*> id: pb4432_set_to_true_names_own_capacity_register_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432Y declares the GLOBAL item X with the condition-name CAP;
*> PB4432Z, contained in it, declares its own dynamic table whose CAPACITY IN phrase names the register CAP (ISO
*> 13.18.38.3 SR30: a data-name of its entry's source element) and writes SET CAP TO TRUE. In PB4432Z, CAP is that
*> register, not a condition-name (COBOL1639). Fails if the statement is accepted and sets the container's X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 0.
          88 CAP VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432Z".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC X OCCURS DYNAMIC CAPACITY IN CAP.
       PROCEDURE DIVISION.
           SET CAP TO TRUE.
           EXIT PROGRAM.
       END PROGRAM PB4432Z.
       END PROGRAM PB4432Y.
