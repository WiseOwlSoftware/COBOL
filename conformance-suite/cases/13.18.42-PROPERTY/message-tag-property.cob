*>--- conformance-case
*> id: message-tag-property
*> rules: [GR-13.18.42.4-1, GR-13.18.42.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 13.18.42.4 GR1 and GR2 define the implicit GET and SET methods of a PROPERTY clause,
      *> and for a subject "of class index, message-tag, object, or pointer" the definition is a SET statement, not a MOVE
      *> (cite.py --check 13.18.42.4 OK, GR 1): the implicit GET is "SET LS-data-name TO data-name" and the implicit SET
      *> the reverse. MT is a level-1 USAGE MESSAGE-TAG item in the object's working storage, so both implicit methods
      *> are SETs over the tag. A fresh object's tag holds the NULL content (the implementation's initial state, as for a
      *> working-storage tag in message-tag-usage), so the read through the property finds it NULL, and a write of NULL
      *> through the property leaves it NULL.
      *> Derived: GET NULL, then SET NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTPROP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS MTPROPC
           PROPERTY MT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE MTPROPC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE MTPROPC "NEW" RETURNING A.
           IF MT OF A = NULL
              DISPLAY "GET NULL"
           END-IF
           SET MT OF A TO NULL
           IF MT OF A = NULL
              DISPLAY "SET NULL"
           END-IF
           STOP RUN.
       END PROGRAM MTPROP.

       IDENTIFICATION DIVISION.
       CLASS-ID. MTPROPC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT USAGE MESSAGE-TAG PROPERTY.
       END OBJECT.
       END CLASS MTPROPC.
