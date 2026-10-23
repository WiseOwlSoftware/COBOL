*>--- conformance-case
*> id: pb3544_item8_rollback_in_merge_output_procedure_ec_flow
*> rules: [GR-14.9.36.4-2, GR-14.6.13.1.3-5, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3544 - the MERGE arm of item 8's ROLLBACK (wave 1156 A, review lead R1-2); the
      *> SORT input-procedure arm is pb3544_item8_rollback_in_sort_procedure_ec_flow:
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
      *> EC-I-O checking is off and EC-FLOW-ROLLBACK checking is on. The READ of RELF in the MERGE's
      *> output procedure, RELF open OUTPUT, answers '47' (fatal) into FS; item 8's ROLLBACK is
      *> attempted under the MERGE, so EC-FLOW-ROLLBACK exists instead and nothing is rolled back:
      *> the declarative sees WS-COUNT at 0002 and FS at '47', not the 0001 and '00' the COMMIT saved.
      *> It completes normally, and the run unit ends abnormally. Expected: the declarative's one
      *> line and nothing after it. Before PB3544 item 8 rolled RELF and WS-COUNT back under the
      *> MERGE and raised nothing (OUT-P COUNT=0001 FS=00, AFTER MERGE ...).
       >>TURN EC-FLOW-ROLLBACK CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544MR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544mr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT IN1 ASSIGN TO "p3544mr-1.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT IN2 ASSIGN TO "p3544mr-2.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT MWK ASSIGN TO "p3544mr.tmp".
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       FD IN1.
       01 I1REC PIC X(10).
       FD IN2.
       01 I2REC PIC X(10).
       SD MWK.
       01 MREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 WS-COUNT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-ROLLBACK.
       H-P.
           DISPLAY "COUNT=" WS-COUNT " FS=" FS " HANDLED=" FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT IN1
           MOVE "A1" TO I1REC
           WRITE I1REC
           CLOSE IN1
           OPEN OUTPUT IN2
           MOVE "B1" TO I2REC
           WRITE I2REC
           CLOSE IN2
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
           MERGE MWK ON ASCENDING KEY MREC
               USING IN1 IN2
               OUTPUT PROCEDURE IS OUT-P
           DISPLAY "AFTER MERGE COUNT=" WS-COUNT " FS=" FS
           STOP RUN.
       OUT-P.
           READ RELF
           DISPLAY "OUT-P COUNT=" WS-COUNT " FS=" FS.
