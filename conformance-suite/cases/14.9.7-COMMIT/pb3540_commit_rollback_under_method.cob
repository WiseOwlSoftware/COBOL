*>--- conformance-case
*> id: pb3540_commit_rollback_under_method
*> rules: [GR-14.9.7.4-2, GR-14.9.36.4-2, GR-9.1.18.7-p2, SR-9.1.18.6-p1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3540 - the recursion arm of 14.9.7.4 GR2 for a METHOD: "If this statement is attempted to be executed under
*> the control of a recursive runtime element or a file SORT or MERGE statement, then the exception condition
*> EC-FLOW-COMMIT is set to exist." 14.9.36.4 GR2 says the same of ROLLBACK and EC-FLOW-ROLLBACK, and 9.1.18.7 pairs
*> them: "EC-FLOW-COMMIT and EC-FLOW-ROLLBACK are also raised if a commit or rollback is attempted to be executed while
*> under the control of a recursive runtime element." 8.6.6 makes a method one: "Functions and methods are always
*> recursive." 9.1.18.6 puts the statements out of a method's reach altogether, "nor in any runtime elements that they
*> invoke". The shape: PB3540M names the RELATIVE file RELF in its APPLY COMMIT clause, so the clause is active from
*> PB3540M's activation (12.4.6.3.4 GR5), and writes record 1. It INVOKEs the object method DOWORK of PB3540C, which
*> CALLs PB3540S (no RECURSIVE attribute of its own); PB3540S executes COMMIT and ROLLBACK while the method is running,
*> that is, under the control of a recursive runtime element. Both conditions are Fatal (14.6.13.1.6 Table 13) and
*> enabled by the TURN directive, so the format-3 declarative runs and RESUME AT NEXT STATEMENT continues. Expected,
*> from those rules:
*>   HANDLED=EC-FLOW-COMMIT     PB3540S's COMMIT, under the method
*>   HANDLED=EC-FLOW-ROLLBACK   PB3540S's ROLLBACK, likewise
*>   RECORD 1 ABSENT            back in PB3540M no recursive element is active and its ROLLBACK restores RELF to the
*>                              start of the run unit (14.9.36.4 GR3): the COMMIT under the method committed nothing.
*> Negative control: PB3540M's own activation raises no EC-FLOW-APPLY-COMMIT, and its ROLLBACK raises nothing.
      >>TURN EC-FLOW-COMMIT EC-FLOW-ROLLBACK EC-FLOW-APPLY-COMMIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3540C.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3540m.dat"
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
       01 O USAGE OBJECT REFERENCE PB3540C.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-COMMIT EC-FLOW-ROLLBACK EC-FLOW-APPLY-COMMIT.
       H-P.
           DISPLAY "MAIN HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           INVOKE PB3540C "NEW" RETURNING O
           INVOKE O "DOWORK"
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
       END PROGRAM PB3540M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3540C INHERITS FROM BASE.
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
           CALL "PB3540S".
       END METHOD DOWORK.
       END OBJECT.
       END CLASS PB3540C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3540S.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-COMMIT EC-FLOW-ROLLBACK.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMMIT
           ROLLBACK
           GOBACK.
       END PROGRAM PB3540S.
