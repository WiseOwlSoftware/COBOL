*>--- conformance-case
*> id: pb3544_item8_not_on_a_sort_transfer
*> rules: [GR-14.6.13.1.3-2, GR-14.6.13.1.3-8, GR-14.9.40.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3544 - a fatal status on a SORT's USING file runs no item 8 ROLLBACK:
      *>   cite.py: OK 14.6.13.1.3 2) "If the executed statement is a MERGE or SORT statement, then
      *>   the rules for those statements apply." Item 2 comes before item 8, so item 8 is not
      *>   reached.
      *>   cite.py: OK 14.9.40.4 12) b) "If a fatal exception condition exists for file-name-1, the
      *>   SORT is terminated".
      *> RELM, a RELATIVE file the APPLY COMMIT clause names, does not exist, so the SORT's implicit
      *> OPEN answers '35' (fatal) into its FILE STATUS item FM, and the SORT ends. Nothing is rolled
      *> back. Expected: WS-COUNT keeps the 0002 the second ADD made, FM holds '35', and record 2,
      *> written to RELF after the COMMIT, is still in the file. The defect rolled back under the
      *> SORT (0001, FM restored to spaces, record 2 absent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544SU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544su.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT RELM ASSIGN TO "p3544su-missing.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FM.
           SELECT SWK ASSIGN TO "p3544su.tmp".
           SELECT OUTF ASSIGN TO "p3544su-out.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON RELF RELM WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       FD RELM.
       01 MREC PIC X(10).
       SD SWK.
       01 SREC PIC X(10).
       FD OUTF.
       01 OREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 FM PIC XX.
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
           CLOSE RELF
           SORT SWK ON ASCENDING KEY SREC
               USING RELM GIVING OUTF
           DISPLAY "AFTER SORT COUNT=" WS-COUNT " FM=" FM
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
