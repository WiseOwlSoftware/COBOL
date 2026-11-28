*>--- conformance-case
*> id: pb6276-method-select-when-own-condition-name
*> rules: [SR-13.18.51.3-1, GR-13.18.51.4-2]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6276 -- §2023:13.18.51.3 SR1 (cite.py --check) "A SELECT WHEN clause may be specified only at the 01 level of a record
*> description entry in the file, linkage, local-storage, or working-storage section." and §2023:13.18.51.4 GR2 "SELECT WHEN clauses of record
*> description entries specified in the working-storage section, local-storage section, or the linkage section have no effect": the clause of a
*> method's LOCAL-STORAGE record is legal and has no effect, and the condition-name it names is the method's own level-88 K (X = "A", true).
*> The method then tests K and displays METHOD-K; the object declares no K, so the clause names the only level-88 of the spelling there is.
*> Why it can fail: the clause is refused (COBOL7343 "is not the name of a level-88 entry") because the lookup read only the object's
*> condition-name table, which holds no K, and never the method's own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6276M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB6276C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB6276C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB6276C "NEW" RETURNING OBJ
           INVOKE OBJ "SHOW"
           STOP RUN.
       END PROGRAM PB6276M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB6276C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R SELECT WHEN K.
          05 X PIC X VALUE "A".
             88 K VALUE "A".
       PROCEDURE DIVISION.
           IF K
               DISPLAY "METHOD-K"
           ELSE
               DISPLAY "OBJECT-K"
           END-IF.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB6276C.
