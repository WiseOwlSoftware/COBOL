      *>--- conformance-case
      *> id: pb5125_relative_write_key_range_file_status_2002
      *> rules: [GR-14.9.51.4-29, GR-14.9.51.4-33, DOC-A.1-107]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5125 - at COBOL-2002, a random WRITE of a relative file whose key is outside the file's boundaries,
      *> written with neither an INVALID KEY phrase nor a USE AFTER EXCEPTION procedure: the FILE STATUS item alone carries
      *> the invalid key condition, and execution continues.
      *>
      *> §2002:14.8.47.3 GR29 b) (cite.py --check): "When an attempt is made to write beyond the externally defined
      *> boundaries of the file, the I-O status associated with the write file connector is set to '24'", the invalid key
      *> condition; §2002:9.1.13 item 4: with no INVALID KEY phrase and no USE AFTER EXCEPTION procedure, "control is
      *> transferred to the end of the input-output statement". COBOL-85 has no such path (§1985:VIII.1.3.5: without the
      *> phrase "a USE AFTER EXCEPTION procedure must be associated with the file connector"), and from 2014 the attempt is
      *> the permanent error 34 instead (§2014:14.9.47.3 GR26 b), whose witness is pb5125_relative_write_key_range.
      *>
      *> The boundaries are WiseOwl's (docs/CONFORMANCE.md DOC-A.1-107): relative record numbers 1 through 2 147 483 647.
      *> ZERO and ABOVE answer 24 and the next statement runs; 24 is no permanent error, so K5 is written ('00') and DUP5
      *> is the invalid key '22'; the READ after the CLOSE finds record 5 as K5 wrote it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5125D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RV ASSIGN TO "pb5125d.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RV.
       01  RVR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(10).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RV.
           MOVE 0 TO RK.
           MOVE "ZERO" TO RVR.
           WRITE RVR.
           DISPLAY "ZERO=" FS " " RVR.
           MOVE 2147483648 TO RK.
           MOVE "HIGH" TO RVR.
           WRITE RVR.
           DISPLAY "ABOVE=" FS " " RVR.
           MOVE 5 TO RK.
           MOVE "FIVE" TO RVR.
           WRITE RVR.
           DISPLAY "K5=" FS.
           MOVE "DUP5" TO RVR.
           WRITE RVR.
           DISPLAY "DUP5=" FS.
           CLOSE RV.
           DISPLAY "CLOSE=" FS.
           OPEN INPUT RV.
           MOVE 5 TO RK.
           READ RV.
           DISPLAY "READ5=" FS " " RVR.
           CLOSE RV.
           STOP RUN.
