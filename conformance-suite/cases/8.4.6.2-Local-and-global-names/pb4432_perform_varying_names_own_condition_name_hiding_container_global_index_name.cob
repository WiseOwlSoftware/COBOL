*>--- conformance-case
*> id: pb4432_perform_varying_names_own_condition_name_hiding_container_global_index_name
*> rules: [GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4432 - ISO 8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is the
*> referenced item." PB4432K declares the GLOBAL table T1 whose INDEXED BY phrase names RELF; PB4432L, contained in it,
*> declares its own condition-name RELF and writes PERFORM VARYING RELF. PB4432L's condition-name is the referenced item,
*> and it identifies no data item (COBOL1639), so the varying position cannot take it. Fails if the statement is
*> accepted and varies the container's index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS GLOBAL.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           CALL "PB4432L".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           PERFORM VARYING RELF FROM 1 BY 1 UNTIL RELF > 2
               CONTINUE
           END-PERFORM.
           GOBACK.
       END PROGRAM PB4432L.
       END PROGRAM PB4432K.
