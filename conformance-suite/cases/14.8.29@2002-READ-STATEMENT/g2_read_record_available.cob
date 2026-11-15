      *>--- conformance-case
      *> id: g2_read_record_available
      *> rules: [2002-GR-14.8.29.3-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.3 GR14 - "a record is available to the runtime
      *> element prior to the execution of imperative-statement-2 or
      *> imperative-statement-4, if specified, or prior to the
      *> execution of any statement following the READ statement, if
      *> imperative-statement-2 or imperative-statement-4 is not
      *> specified." Each arm clears the record area first, so a
      *> record not yet available shows spaces (or the old value):
      *> NAE  sequential READ ... INTO ... NOT AT END: R001 is in the
      *>      record area and, by GR4, in W when imperative-statement-2
      *>      runs.
      *> NXT  sequential READ with no phrase: R002 is there for the
      *>      next statement.
      *> NIK  random READ ... KEY ... NOT INVALID KEY: the K002 record
      *>      (D002) is there when imperative-statement-4 runs.
      *> RND  random READ with no phrase (§2002:14.8.29.2 SR8: no NEXT
      *>      is implied, so by GR16 a random read on the prime key):
      *>      D001 is there for the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "g2rd14s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS IS S1.
           SELECT IX ASSIGN TO "g2rd14i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IX-K FILE STATUS IS S3.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-R PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X(4).
          05 IX-D PIC X(4).
       WORKING-STORAGE SECTION.
       01 S1 PIC XX.
       01 S3 PIC XX.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQ.
           MOVE "R001" TO SQ-R. WRITE SQ-R.
           MOVE "R002" TO SQ-R. WRITE SQ-R.
           CLOSE SQ.
           OPEN OUTPUT IX.
           MOVE "K001" TO IX-K. MOVE "D001" TO IX-D. WRITE IX-R.
           MOVE "K002" TO IX-K. MOVE "D002" TO IX-D. WRITE IX-R.
           CLOSE IX.
           OPEN INPUT SQ.
           MOVE SPACES TO SQ-R W.
           READ SQ INTO W
               NOT AT END DISPLAY "NAE SQ-R=" SQ-R " W=" W
           END-READ.
           MOVE SPACES TO SQ-R.
           READ SQ.
           DISPLAY "NXT SQ-R=" SQ-R.
           CLOSE SQ.
           OPEN INPUT IX.
           MOVE "K002" TO IX-K. MOVE SPACES TO IX-D.
           READ IX KEY IS IX-K
               NOT INVALID KEY DISPLAY "NIK IX-D=" IX-D
           END-READ.
           MOVE "K001" TO IX-K. MOVE SPACES TO IX-D.
           READ IX RECORD.
           DISPLAY "RND IX-D=" IX-D.
           CLOSE IX.
           STOP RUN.
