*>--- conformance-case
*> id: pb3544_item8_rollback_in_sort_procedure_ec_flow
*> rules: [GR-14.9.36.4-2, GR-14.6.13.1.3-5, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3544 - item 8's ROLLBACK is a ROLLBACK STATEMENT, so the ROLLBACK statement's
      *> GR2 holds for it:
      *>   cite.py: OK 14.6.13.1.3 8) "However, if there is a fatal exception condition for a file
      *>   subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
      *>   unconditionally."
      *>   cite.py: OK 14.9.36.4 2) "If this statement is attempted to be executed under the control
      *>   of a recursive runtime element or a file SORT or MERGE statement, then the exception
      *>   condition EC-FLOW-ROLLBACK is set to exist."
      *>   cite.py: OK 14.6.13.1.3 5) "If checking for the exception condition is enabled and there
      *>   is an applicable USE statement ... the associated declarative is executed. If execution
      *>   of the declarative completes normally the execution of the run unit is terminated
      *>   abnormally as specified in 14.6.12".
      *> EC-I-O checking is off and EC-FLOW-ROLLBACK checking is on. The READ of RELF in the SORT's
      *> input procedure, RELF open OUTPUT, answers '47' (fatal); item 8's ROLLBACK is attempted
      *> under the SORT, so EC-FLOW-ROLLBACK exists instead and nothing is rolled back; its
      *> declarative runs and completes normally, and the run unit ends abnormally. Expected: the
      *> declarative's one line and nothing after it. The defect rolled RELF and WS-COUNT back under
      *> the SORT and raised nothing (IN-P COUNT=0001 FS=00, AFTER SORT ...).
       >>TURN EC-FLOW-ROLLBACK CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544FR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544fr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT SWK ASSIGN TO "p3544fr.tmp".
           SELECT OUTF ASSIGN TO "p3544fr-out.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       SD SWK.
       01 SREC PIC X(10).
       FD OUTF.
       01 OREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 WS-COUNT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-ROLLBACK.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           ADD 1 TO WS-COUNT
           COMMIT
           MOVE 2 TO RK
           MOVE "BBBB" TO RREC
           WRITE RREC
           ADD 1 TO WS-COUNT
           SORT SWK ON ASCENDING KEY SREC
               INPUT PROCEDURE IS IN-P
               GIVING OUTF
           DISPLAY "AFTER SORT COUNT=" WS-COUNT " FS=" FS
           STOP RUN.
       IN-P.
           READ RELF
           DISPLAY "IN-P COUNT=" WS-COUNT " FS=" FS.
