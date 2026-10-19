*>--- conformance-case
*> id: pb1587_write_released_to_input_reader
*> rules: [GR-14.9.51.4-12, GR-9.1.15-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1587 - a record a WRITE released is in the physical
      *> file when the WRITE returns, so a second file connector open
      *> for input on the same physical file READs it, whatever the
      *> writer's sharing mode admits beside it.
      *>
      *> "The successful execution of a WRITE statement releases a
      *> logical record to the operating environment."
      *>   cite.py: OK  §14.9.51.4 12)  (General rules)
      *> "The sharing with read only mode restricts concurrent access
      *> to a physical file through file connectors other than this
      *> one, to input mode."
      *>   cite.py: OK  §9.1.15 2)  (Sharing mode)
      *> "If the file position indicator indicates that an optional
      *> input file is not present or that no next or previous logical
      *> record exists, the I-O status value associated with file-name-1
      *> is set to '10', the at end condition exists"
      *>   cite.py: OK  §14.9.30.4 21)  (General rules)
      *> "For a sequential READ statement, if the previous READ or START
      *> statement for the file connector was unsuccessful, then the
      *> READ statement is unsuccessful and the I-O status is set to
      *> '46'"
      *>   cite.py: OK  §14.9.30.4 21)  (General rules)
      *>
      *> The standard prints no buffering rule for a WRITE; GR12 is the
      *> whole of it, and §9.1.15 2) lets the INPUT connector be open
      *> beside the READ ONLY writer, so what the writer released is
      *> what the reader meets. Until PB1587 a SHARING WITH READ ONLY
      *> writer held its records in its 4,096-byte input-output area
      *> (docs/CONFORMANCE.md DOC-A.1-164) and the reader met AT END.
      *>
      *> DERIVATION. W is the writer (READ ONLY), R the reader (ALL
      *> OTHER: Table 19 admits an INPUT request of that mode beside a
      *> READ ONLY OUTPUT or EXTEND connector). Each leg is one record
      *> sequential physical file.
      *>   A the file is empty: R's first READ is the at end condition
      *>     ('10'), handled by the AT END phrase, so no declarative.
      *>   B one record is released: R reads it ('00'), then the next
      *>     READ is '10' (no next record), then a READ with no AT END
      *>     phrase follows an unsuccessful READ: '46' (GR21), a status
      *>     no phrase handles, so the USE procedure on R runs.
      *>   C R reads record 1; the writer then releases two more; R's
      *>     next READs are records 2 and 3 (its read-ahead is not
      *>     served stale), then '10'.
      *>   D the writer is re-opened EXTEND with R open on the file:
      *>     the READ that follows the first released record is that
      *>     record, then '10'.
      *>   E a reader that states SHARING WITH READ ONLY is refused
      *>     beside the READ ONLY writer ('61', Table 19, a request
      *>     READ ONLY / INPUT against an existing read only / extend
      *>     connector), as is any opener beside a NO OTHER writer;
      *>     the refusal leaves both files as they were.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1587SEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT W ASSIGN TO "pb1587seq.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS W-ST.
           SELECT R ASSIGN TO "pb1587seq.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS R-ST.
           SELECT R2 ASSIGN TO "pb1587seq.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS R2-ST.
           SELECT N ASSIGN TO "pb1587seqn.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS N-ST.
           SELECT NR ASSIGN TO "pb1587seqn.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS NR-ST.
       DATA DIVISION.
       FILE SECTION.
       FD W.
       01 W-REC  PIC X(8).
       FD R.
       01 R-REC  PIC X(8).
       FD R2.
       01 R2-REC PIC X(8).
       FD N.
       01 N-REC  PIC X(8).
       FD NR.
       01 NR-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 W-ST   PIC XX.
       01 R-ST   PIC XX.
       01 R2-ST  PIC XX.
       01 N-ST   PIC XX.
       01 NR-ST  PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       R-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON R.
       R-ERR-1.
           DISPLAY "DECLARATIVE R " R-ST.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT W
           DISPLAY "A OPEN W " W-ST
           OPEN INPUT R
           DISPLAY "A OPEN R " R-ST
           READ R
               AT END DISPLAY "A READ AT END " R-ST
               NOT AT END DISPLAY "A READ GOT " R-REC
           END-READ
           CLOSE R
           MOVE "RECORD-1" TO W-REC
           WRITE W-REC
           DISPLAY "B WRITE " W-ST
           OPEN INPUT R
           READ R
               AT END DISPLAY "B READ AT END " R-ST
               NOT AT END DISPLAY "B READ " R-ST " " R-REC
           END-READ
           READ R
               AT END DISPLAY "B READ2 AT END " R-ST
               NOT AT END DISPLAY "B READ2 " R-ST " " R-REC
           END-READ
           READ R
           DISPLAY "B READ3 " R-ST
           CLOSE R
           OPEN INPUT R
           READ R
           DISPLAY "C READ " R-ST " " R-REC
           MOVE "RECORD-2" TO W-REC
           WRITE W-REC
           DISPLAY "C WRITE " W-ST
           MOVE "RECORD-3" TO W-REC
           WRITE W-REC
           DISPLAY "C WRITE " W-ST
           READ R
           DISPLAY "C READ " R-ST " " R-REC
           READ R
           DISPLAY "C READ " R-ST " " R-REC
           READ R
               AT END DISPLAY "C READ AT END " R-ST
               NOT AT END DISPLAY "C READ GOT " R-REC
           END-READ
           CLOSE R
           CLOSE W
           OPEN EXTEND W
           DISPLAY "D OPEN W " W-ST
           OPEN INPUT R
           DISPLAY "D OPEN R " R-ST
           READ R
           DISPLAY "D READ " R-ST " " R-REC
           MOVE "RECORD-4" TO W-REC
           WRITE W-REC
           DISPLAY "D WRITE " W-ST
           READ R
           DISPLAY "D READ " R-ST " " R-REC
           READ R
           DISPLAY "D READ " R-ST " " R-REC
           READ R
           DISPLAY "D READ " R-ST " " R-REC
           READ R
               AT END DISPLAY "D READ AT END " R-ST
               NOT AT END DISPLAY "D READ GOT " R-REC
           END-READ
           OPEN INPUT R2
           DISPLAY "E OPEN R2 " R2-ST
           CLOSE R
           CLOSE W
           OPEN OUTPUT N
           DISPLAY "E OPEN N " N-ST
           OPEN INPUT NR
           DISPLAY "E OPEN NR " NR-ST
           MOVE "RECORD-9" TO N-REC
           WRITE N-REC
           DISPLAY "E WRITE N " N-ST
           CLOSE N
           OPEN INPUT NR
           DISPLAY "E OPEN NR " NR-ST
           READ NR
           DISPLAY "E READ NR " NR-ST " " NR-REC
           CLOSE NR
           STOP RUN.
