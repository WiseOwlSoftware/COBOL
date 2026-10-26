*>--- conformance-case
*> id: pb3540_commit_in_object_method
*> rules: [SR-9.1.18.6-p1, SR-14.9.7.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the static reach of ISO 9.1.18.6: "APPLY COMMIT clauses, and the COMMIT and ROLLBACK statements are
*> not permitted in factory and object classes, nor in any of their methods nor in any runtime elements that they
*> invoke." A COMMIT written in a method of an object definition is refused at translation: 14.9.7.3 SR1 reads "This
*> statement shall not be specified in a recursive source element." (8.6.6: "Functions and methods are
*> always recursive."), COBOL1690. The dynamic half, a program a method invokes, is the runtime's
*> (pb3540_commit_rollback_under_method). Refused at 2023, the one edition that has the statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB3540G.
       PROCEDURE DIVISION.
           INVOKE PB3540G "NEW" RETURNING O
           INVOKE O "DOWORK"
           STOP RUN.
       END PROGRAM PB3540E.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540G INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. DOWORK.
       PROCEDURE DIVISION.
       MMAIN.
           COMMIT.
       END METHOD DOWORK.
       END OBJECT.
       END CLASS PB3540G.
