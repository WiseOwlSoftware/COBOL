*>--- conformance-case
*> id: pb6276-method-select-when-hidden-condition-name
*> rules: [SR-13.18.51.3-1, GR-11.7.4-5]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6276 -- §2023:11.7.4 GR5 (cite.py --check) "If a given user-defined word is defined in the data division of this method definition and
*> in the data division of the containing object definition, the use of that word in this method refers to the declaration in this method". The
*> object declares the level-88 K; the method declares the data item K (a PIC X), so in the method K is that data item, which is no condition-name,
*> and the SELECT WHEN clause of the method's record names no level-88 entry: §2023:13.18.51.1 (cite.py --check) "The SELECT WHEN clause
*> specifies a condition-name condition under which a record description entry is associated with a record", and a data item is no
*> condition-name. The clause is refused.
*> Why it can fail: the lookup answers the object's level-88 K, which the method's own K hides, and accepts the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6276N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB6276D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB6276D.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB6276D "NEW" RETURNING OBJ
           INVOKE OBJ "SHOW"
           STOP RUN.
       END PROGRAM PB6276N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB6276D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OW PIC X VALUE "Z".
          88 K VALUE "Z".
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R SELECT WHEN K.
          05 K PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY "SHOWN".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB6276D.
