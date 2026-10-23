*>--- conformance-case
*> id: pb3544_item8_rollback_in_sort_procedure_unchecked
*> rules: [GR-14.9.36.4-2, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3544 - the checking-off half of pb3544_item8_rollback_in_sort_procedure_ec_flow:
      *>   cite.py: OK 14.6.13.1.3 8) "However, if there is a fatal exception condition for a file
      *>   subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
      *>   unconditionally."
      *>   cite.py: OK 14.9.36.4 2) "If this statement is attempted to be executed under the control
      *>   of a recursive runtime element or a file SORT or MERGE statement, then the exception
      *>   condition EC-FLOW-ROLLBACK is set to exist."
      *> No checking is enabled. The READ of RELF in the SORT's input procedure, RELF open OUTPUT,
      *> answers '47' (fatal) into FS, which covers it, so the run unit continues; item 8's ROLLBACK
      *> is attempted under the SORT, EC-FLOW-ROLLBACK exists with checking off, and nothing is
      *> rolled back (docs/CONFORMANCE.md A.4.3: a forbidden statement with checking off is not
      *> executed). Expected: WS-COUNT keeps 0002 and FS '47' in the procedure and after the SORT;
      *> the program's own ROLLBACK after the SORT then finds the transaction and restores 0001,
      *> '00' and the file without record 2. The defect rolled back inside the SORT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544FO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544fo.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT SWK ASSIGN TO "p3544fo.tmp".
           SELECT OUTF ASSIGN TO "p3544fo-out.dat"
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
           ROLLBACK
           DISPLAY "AFTER ROLLBACK COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
       IN-P.
           READ RELF
           DISPLAY "IN-P COUNT=" WS-COUNT " FS=" FS.
