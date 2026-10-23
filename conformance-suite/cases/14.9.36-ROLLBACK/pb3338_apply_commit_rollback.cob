*>--- conformance-case
*> id: pb3338_apply_commit_rollback
*> rules: [GR-12.4.6.3.4-1, GR-14.9.7.4-3, GR-14.9.7.4-5, GR-14.9.36.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - the APPLY COMMIT clause over a RELATIVE file and a data item. ISO 14.9.7.4 GR3: "The execution
*> of the COMMIT statement permanently applies any changes made to the files specified in all the active APPLY
*> COMMIT clauses"; GR5: "The contents of all data-items referenced in the remaining active APPLY COMMIT clauses
*> are saved". ISO 14.9.36.4 GR3: "All files and data-items referenced in active APPLY COMMIT clauses are restored
*> to the state they were in at the execution of the last COMMIT statement ... It also includes the file status
*> data items". Expected, from those rules: record 1 written before the COMMIT survives; record 2 written after it
*> is gone after the ROLLBACK; WS-COUNT is back to the 1 the COMMIT saved; the FILE STATUS item holds the '00' it
*> held at the COMMIT; and a ROLLBACK with no change since the COMMIT restores the same state again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338AR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338ar.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 WS-COUNT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
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
           MOVE "99" TO FS
           DISPLAY "BEFORE ROLLBACK COUNT=" WS-COUNT " FS=" FS
           ROLLBACK
           DISPLAY "AFTER ROLLBACK COUNT=" WS-COUNT " FS=" FS
           ROLLBACK
           DISPLAY "AGAIN COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT FS=" FS
           END-READ
           MOVE 1 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 1 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 1=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
