      *>--- conformance-case
      *> id: g85_bf20_01
      *> rules: [1985-GR-VII.2.5.4-1, 1985-GR-VII.2.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.5.4 GR1 - "the data item referenced by data-name-1
      *> is always updated to contain the value of the I-O status
      *> whenever the I-O status is updated": before every OPEN, WRITE,
      *> READ and CLOSE the program moves "ZZ" into the item of the file
      *> the statement names, so a statement that leaves the item stale
      *> shows ZZ instead of 00 or 10.
      *> §1985:VII.2.5.4 GR2 - the item updated "is the one specified in
      *> the file control entry associated with that statement": FA and
      *> FB each have their own item, and the item of the file a
      *> statement does NOT name holds a sentinel (AA or BB) that must
      *> survive; a statement that stores into the wrong item fails.
      *> §1985:VII.1.3.5 - status 00 for a successful statement, 10 for
      *> a sequential READ with no next logical record, set "prior to
      *> the execution of any imperative statement associated with that
      *> input-output statement": the AT END line shows 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2001.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g85bf20a.dat"
               FILE STATUS IS FS-A.
           SELECT FB ASSIGN TO "g85bf20b.dat"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  RA PIC XX.
       FD  FB.
       01  RB PIC XX.
       WORKING-STORAGE SECTION.
       01  FS-A PIC XX VALUE "AA".
       01  FS-B PIC XX VALUE "BB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ZZ" TO FS-A.
           OPEN OUTPUT FA.
           DISPLAY "OPEN-A " FS-A " " FS-B.
           MOVE "ZZ" TO FS-A.
           MOVE "A1" TO RA.
           WRITE RA.
           DISPLAY "WRITE-A " FS-A " " FS-B.
           MOVE "AA" TO FS-A.
           MOVE "ZZ" TO FS-B.
           OPEN OUTPUT FB.
           DISPLAY "OPEN-B " FS-A " " FS-B.
           MOVE "ZZ" TO FS-B.
           MOVE "B1" TO RB.
           WRITE RB.
           DISPLAY "WRITE-B " FS-A " " FS-B.
           MOVE "ZZ" TO FS-A.
           MOVE "BB" TO FS-B.
           CLOSE FA.
           DISPLAY "CLOSE-A " FS-A " " FS-B.
           MOVE "AA" TO FS-A.
           MOVE "ZZ" TO FS-B.
           CLOSE FB.
           DISPLAY "CLOSE-B " FS-A " " FS-B.
           MOVE "ZZ" TO FS-A.
           MOVE "BB" TO FS-B.
           OPEN INPUT FA.
           DISPLAY "OPEN-IN-A " FS-A " " FS-B.
           MOVE "ZZ" TO FS-A.
           MOVE SPACES TO RA.
           READ FA AT END DISPLAY "FIRST READ AT END " FS-A.
           DISPLAY "READ-A " FS-A " " FS-B " " RA.
           MOVE "ZZ" TO FS-A.
           READ FA AT END DISPLAY "AT END " FS-A.
           DISPLAY "READ-A-END " FS-A " " FS-B.
           MOVE "ZZ" TO FS-A.
           CLOSE FA.
           DISPLAY "CLOSE-A " FS-A " " FS-B.
           STOP RUN.
