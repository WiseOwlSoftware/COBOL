*>--- conformance-case
*> id: pb4432_if_names_own_constant_name_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432W declares the GLOBAL item X with the condition-name RELF;
*> PB4432X, contained in it, declares its own constant-name RELF (5) and writes IF RELF. In PB4432X, RELF is the literal
*> 5 (ISO 13.10.4 GR1), which is none of the conditions of 8.8.4 (COBOL2318). Fails if the statement is accepted and
*> tests the container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432X".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF CONSTANT AS 5.
       PROCEDURE DIVISION.
           IF RELF DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432X.
       END PROGRAM PB4432W.
