*>--- conformance-case
*> id: pb3540_commit_under_method_below_2023
*> rules: [GR-12.4.6.3.4-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the APPLY COMMIT clause, COMMIT and ROLLBACK are COBOL-2023 additions (Annex E.3.2 item 2), so the
*> program of its 2023 twin pb3540_commit_rollback_under_method, which carries the clause in the program that INVOKEs
*> a method, is refused below 2023 at the clause (the closed I-O-CONTROL format of 2002 and 2014, COBOLNET1970).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540Z.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3540n.dat"
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
       01 O USAGE OBJECT REFERENCE PB3540Z.
       PROCEDURE DIVISION.
           INVOKE PB3540Z "NEW" RETURNING O
           INVOKE O "DOWORK"
           STOP RUN.
       END PROGRAM PB3540N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540Z INHERITS FROM BASE.
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
           DISPLAY "DOWORK".
       END METHOD DOWORK.
       END OBJECT.
       END CLASS PB3540Z.
