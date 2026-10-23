*>--- conformance-case
*> id: pb3338_apply_commit_external_two_elements
*> rules: [GR-12.4.6.3.4-2, GR-14.9.36.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.4 GR2: "If the same file connector is specified in more than one runtime element within
*> the run unit, then all or none of the runtime elements shall have an associated APPLY COMMIT clause in effect for that
*> file connector and that file connector shall be an external file connector." Both programs describe the EXTERNAL
*> RELATIVE file RELF and both name it in an APPLY COMMIT clause, so one file connector is under one transaction.
*> ISO 14.9.36.4 GR3: "All files and data-items referenced in active APPLY COMMIT clauses are restored to the state they
*> were in at the execution of the last COMMIT statement". Expected, from those rules: the first program writes record 1,
*> COMMITs, writes record 2 and calls the second, whose ROLLBACK acts on the shared connector; record 2 is gone and
*> record 1, written before the COMMIT, survives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338G2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338g2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           COMMIT
           MOVE 2 TO RK
           MOVE "BBBB" TO RREC
           WRITE RREC
           CALL "PB3338G2B"
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           MOVE 1 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 1 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 1=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
       END PROGRAM PB3338G2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338G2B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338g2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
           ROLLBACK
           GOBACK.
       END PROGRAM PB3338G2B.
