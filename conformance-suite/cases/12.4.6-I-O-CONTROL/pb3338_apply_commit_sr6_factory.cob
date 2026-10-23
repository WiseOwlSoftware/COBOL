*>--- conformance-case
*> id: pb3338_apply_commit_sr6_factory
*> rules: [SR-12.4.6.3.3-6, SR-9.1.18.6-p1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR6: "This clause shall not be specified in a class, factory, or interface
*> definition." and ISO 9.1.18.6: "APPLY COMMIT clauses, and the COMMIT and ROLLBACK statements are not permitted
*> in factory and object classes". The shape: a factory definition's I-O-CONTROL paragraph writes the clause over
*> its own RELATIVE file. A class definition's own environment division has no INPUT-OUTPUT SECTION (12.4.3 SR1),
*> so a factory or an instance definition is where the clause can be written. The clause is accepted for a
*> RELATIVE file in a program, so the only refusal this program can draw is SR6's, by name (COBOLNET4709).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3338F6 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338f6.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       END FACTORY.
       END CLASS PB3338F6.
