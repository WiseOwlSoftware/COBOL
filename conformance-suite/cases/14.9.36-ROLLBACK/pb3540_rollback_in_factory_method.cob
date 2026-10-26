*>--- conformance-case
*> id: pb3540_rollback_in_factory_method
*> rules: [SR-9.1.18.6-p1, SR-14.9.36.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the static reach of ISO 9.1.18.6 for a FACTORY method and ROLLBACK: "APPLY COMMIT clauses, and the
*> COMMIT and ROLLBACK statements are not permitted in factory and object classes, nor in any of their methods nor in
*> any runtime elements that they invoke." A ROLLBACK written in a method of a factory definition is refused at
*> translation: 14.9.36.3 SR1 reads "This statement shall not be specified in a recursive source element." (8.6.6:
*> "Functions and methods are always recursive."), COBOL1690. Its object-method twin for COMMIT is
*> pb3540_commit_in_object_method.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540J.
       PROCEDURE DIVISION.
           INVOKE PB3540J "DOWORK"
           STOP RUN.
       END PROGRAM PB3540H.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540J INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. DOWORK.
       PROCEDURE DIVISION.
       MMAIN.
           ROLLBACK.
       END METHOD DOWORK.
       END FACTORY.
       END CLASS PB3540J.
