*>--- conformance-case
*> id: pb4432_odo_names_own_condition_name_hiding_container_global_constant_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is the
*> referenced item." PB4432H declares the constant-name RELF IS GLOBAL (a CONSTANT entry, ISO 13.10); PB4432J, contained
*> in it, declares its own condition-name RELF and writes OCCURS DEPENDING ON RELF. PB4432J's condition-name is the
*> referenced item, and it identifies no data item (COBOL0851, "is declared as a condition-name"). Fails if the clause
*> is refused as a constant-name (the farther declaration).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RELF CONSTANT GLOBAL AS 5.
       PROCEDURE DIVISION.
           CALL "PB4432J".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 RELF VALUE 1.
       01 T.
          05 E PIC X OCCURS 1 TO 3 DEPENDING ON RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB4432J.
       END PROGRAM PB4432H.
