*>--- conformance-case
*> id: pb4432_if_names_own_file_name_hiding_container_global_condition_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 review R1-1 (the reverse arm) - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the
*> item in source element B is the referenced item." PB4432AA declares the GLOBAL item X with the condition-name RELF;
*> PB4432AB, contained in it, declares its own file RELF (SELECT RELF, FD RELF) and writes IF RELF. In PB4432AB, RELF is
*> its file-name, which identifies no data item (COBOL1639). Fails if the statement is accepted and tests the
*> container's condition-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432AB".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb4432.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RR PIC X.
       PROCEDURE DIVISION.
           IF RELF DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432AB.
       END PROGRAM PB4432AA.
