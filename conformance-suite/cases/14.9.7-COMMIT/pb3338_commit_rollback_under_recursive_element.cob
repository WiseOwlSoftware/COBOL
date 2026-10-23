*>--- conformance-case
*> id: pb3338_commit_rollback_under_recursive_element
*> rules: [GR-14.9.7.4-2, GR-14.9.36.4-2, GR-9.1.18.7-p2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - the recursion arm of ISO 14.9.7.4 GR2: "If this statement is attempted to be executed under the
*> control of a recursive runtime element or a file SORT or MERGE statement, then the exception condition
*> EC-FLOW-COMMIT is set to exist." ISO 14.9.36.4 GR2 states the same for ROLLBACK and EC-FLOW-ROLLBACK, and 9.1.18.7
*> the pair. The shape: PB3338RM names the RELATIVE file RELF in its APPLY COMMIT clause, so the clause is active from
*> PB3338RM's activation (12.4.6.3.4 GR5), and writes record 1. It calls the RECURSIVE PB3338RR, which calls PB3338RS;
*> PB3338RS executes COMMIT and ROLLBACK while PB3338RR is active, that is, under the control of a recursive runtime
*> element. Both conditions are Fatal (14.6.13.1.6 Table 13) and enabled by the TURN directive, so the format-3
*> declarative runs and RESUME AT NEXT STATEMENT continues. Expected, from those rules:
*>   HANDLED=EC-FLOW-COMMIT     PB3338RS's COMMIT, under the recursive PB3338RR
*>   HANDLED=EC-FLOW-ROLLBACK   PB3338RS's ROLLBACK, likewise
*>   RECORD 1 ABSENT            back in PB3338RM, no recursive element is active and its ROLLBACK restores RELF to
*>                              the start of the run unit (14.9.36.4 GR3): the COMMIT under PB3338RR committed nothing.
*> Negative control: PB3338RM's own activation raises no EC-FLOW-APPLY-COMMIT, and its ROLLBACK raises nothing.
      >>TURN EC-FLOW-COMMIT EC-FLOW-ROLLBACK EC-FLOW-APPLY-COMMIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338RM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338rm.dat"
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
           CALL "PB3338RR"
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
       END PROGRAM PB3338RM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338RR RECURSIVE.
       PROCEDURE DIVISION.
           CALL "PB3338RS"
           GOBACK.
       END PROGRAM PB3338RR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338RS.
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
       END PROGRAM PB3338RS.
