*>--- conformance-case
*> id: pb5786_own_file_name_hides_container_global_data_item
*> rules: [GR-8.4.6.2.1-1, GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5786 - ISO 8.4.6.2.1 1): the set of names is "all names that are defined in source element B and all global
*> names" of its containers, and 3) a): "If the name is declared in source element B, the item in source element B is the
*> referenced item." PB5786A declares the data item FX IS GLOBAL; PB5786B, contained in it, declares its own file FX and
*> writes MOVE "LOCL" TO FX. FX there is B's file-name, which identifies no data item (COBOL1639, "is a file-name"). Fails
*> if the MOVE is accepted and changes the container's item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FX GLOBAL PIC X(4) VALUE "GLOB".
       PROCEDURE DIVISION.
       PA.
           CALL "PB5786B".
           DISPLAY "A FX=" FX.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "PB5786B.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD FX.
       01 FXR PIC X(4).
       PROCEDURE DIVISION.
       PB.
           MOVE "LOCL" TO FX.
           EXIT PROGRAM.
       END PROGRAM PB5786B.
       END PROGRAM PB5786A.
