*>--- conformance-case
*> id: pb6304-inline-invocation-ref-mod-twice
*> rules: [SR-8.4.3.3.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6304. ISO 8.4.3.3.3 SR3: "Identifier-1 shall not be a reference-modification format identifier."
      *> (cite.py OK). The inline method invocation is an identifier (8.4.3.1.2 Format 4) and a reference modifier
      *> applies to it (8.4.3.1.4 GR1g); a second modifier would make identifier-1 the reference-modified item the
      *> first made, as W (1:5) (2:2) over a data item is refused (COBOL1630). The inline path applied each modifier
      *> in turn and compiled this clean.
      *> WHY THE LEG FAILS. A binder that reads every written modifier in turn accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6304TWICE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB6304ACCB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB6304ACCB.
       01 W3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB6304ACCB "NEW" RETURNING A1.
           MOVE A1 :: "GETNAME" (1:5) (2:2) TO W3.
           STOP RUN.
       END PROGRAM PB6304TWICE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB6304ACCB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB6304ACCB.
