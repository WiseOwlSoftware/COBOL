*>--- conformance-case
*> id: pb4432_odo_names_own_condition_name_hiding_container_global_index_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 - ISO 8.4.6.2.1 1): the set of names consists of "all names that are defined in source element B and all
*> global names" of its containers, and 3) a): "If the name is declared in source element B, the item in source element B
*> is the referenced item." PB4432F declares the GLOBAL table T1 whose INDEXED BY phrase names RELF; PB4432G, contained in
*> it, declares its own condition-name RELF and writes OCCURS DEPENDING ON RELF. PB4432G's condition-name is the
*> referenced item, and it identifies no data item (COBOL0851, "is declared as a condition-name"). Fails if the clause
*> is refused as an index-name (the farther declaration).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS GLOBAL.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           CALL "PB4432G".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 RELF VALUE 1.
       01 T.
          05 E2 PIC X OCCURS 1 TO 3 DEPENDING ON RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB4432G.
       END PROGRAM PB4432F.
