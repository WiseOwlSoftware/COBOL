*>--- conformance-case
*> id: pb3540_commit_under_factory_method_unchecked
*> rules: [GR-14.9.7.4-2, GR-9.1.18.7-p2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the same recursion arm of 14.9.7.4 GR2 as pb3540_commit_rollback_under_method, reached through a
*> FACTORY method and with checking off: "If this statement is attempted to be executed under the control of a
*> recursive runtime element or a file SORT or MERGE statement, then the exception condition EC-FLOW-COMMIT is set to
*> exist." A factory method is a method, and 8.6.6 makes every method recursive ("Functions and methods are always
*> recursive."). PB3540F names the RELATIVE file RELF in its APPLY COMMIT clause and writes record 1; it INVOKEs the
*> factory method DOWORK of PB3540K, which CALLs PB3540U; PB3540U executes COMMIT while the method is running. No
*> checking is enabled anywhere, so the condition exists and nothing else happens (docs/CONFORMANCE.md A.4.3: a
*> forbidden statement with checking off is not executed): the COMMIT commits nothing. Expected:
*>   RECORD 1 ABSENT   PB3540F's own ROLLBACK, with no recursive element active, restores RELF to the start of the run
*>                     unit (14.9.36.4 GR3). Before PB3540 the COMMIT under the method went through, record 1 was
*>                     committed, and this line read RECORD 1=AAAA.
*>                     The run unit's start had no file (OPEN OUTPUT created it), so the ROLLBACK removes it and the
*>                     OPTIONAL file's OPEN INPUT answers 05 (9.1.13.2 item 4 a)) before the READ (kb/Work PB3492).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540K.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL RELF ASSIGN TO "pb3540f.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
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
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           INVOKE PB3540K "DOWORK"
           ROLLBACK
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 1 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 1 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 1=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
       END PROGRAM PB3540F.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540K INHERITS FROM BASE.
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
           CALL "PB3540U".
       END METHOD DOWORK.
       END FACTORY.
       END CLASS PB3540K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540U.
       PROCEDURE DIVISION.
       MAIN-P.
           COMMIT
           GOBACK.
       END PROGRAM PB3540U.
