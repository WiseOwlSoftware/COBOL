      *>--- conformance-case
      *> id: g2_write_relative_io_insert
      *> rules: [2002-GR-14.8.47.3-28]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR28 - "When a relative file is opened in the
      *> I-O mode and the access mode is random or dynamic, records are
      *> to be inserted in the associated file. Prior to the execution
      *> of the WRITE statement, the value of the relative key data item
      *> shall be initialized by the runtime element with the relative
      *> record number to be associated with the record that is to be
      *> written. That record is then released to the operating
      *> environment by the execution of the WRITE statement."
      *> The file holds record 3 only. RRF (ACCESS RANDOM) opened I-O
      *> inserts record 1; DRF (ACCESS DYNAMIC, the same physical file)
      *> opened I-O inserts record 5. Each insert lands at the number
      *> the program put in the relative key, and record 3 survives:
      *> random reads of 1..5 find 1, 3, 5 and no record at 2 and 4
      *> (status 23). A leg fails if I-O mode WRITE is refused, appends
      *> at the next slot instead of the key's number, or loses 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RRF ASSIGN TO "g2wr04.tmp" ORGANIZATION RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT DRF ASSIGN TO "g2wr04.tmp" ORGANIZATION RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS DK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RRF.
       01 RR PIC X(4).
       FD DRF.
       01 DR PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 RK PIC 9(4).
       01 DK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RRF
           MOVE 3 TO RK
           WRITE RR FROM "CCCC"
           DISPLAY "SEED-3 " FS
           CLOSE RRF
           OPEN I-O RRF
           MOVE 1 TO RK
           WRITE RR FROM "AAAA"
           DISPLAY "RANDOM-IO-1 " FS
           CLOSE RRF
           OPEN I-O DRF
           MOVE 5 TO DK
           WRITE DR FROM "EEEE"
           DISPLAY "DYNAMIC-IO-5 " FS
           CLOSE DRF
           OPEN INPUT RRF
           PERFORM VARYING RK FROM 1 BY 1 UNTIL RK > 5
               MOVE SPACES TO RR
               READ RRF
                   INVALID KEY DISPLAY "READ-" RK " " FS
                   NOT INVALID KEY DISPLAY "READ-" RK " " FS " " RR
               END-READ
           END-PERFORM
           CLOSE RRF
           STOP RUN.
