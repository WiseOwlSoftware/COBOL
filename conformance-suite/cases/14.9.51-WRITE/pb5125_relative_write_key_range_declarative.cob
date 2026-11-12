      *>--- conformance-case
      *> id: pb5125_relative_write_key_range_declarative
      *> rules: [GR-14.9.51.4-29, GR-14.9.51.4-33, DOC-A.1-105, DOC-A.1-107]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5125 - a random WRITE of a relative file whose key is outside the file's boundaries, written WITHOUT an
      *> INVALID KEY phrase, under a USE AFTER EXCEPTION procedure and with a FILE STATUS item.
      *>
      *> Below 2014 (.85.out, .2002.out) the attempt is the invalid key condition, I-O status 24: §1985:VIII.1.3.4 (cite.py
      *> --check) "I-O Status = 24. An attempt is made to write beyond the externally defined boundaries of a relative
      *> file", §2002:14.8.47.3 GR29 b) the same. With no INVALID KEY phrase "a USE AFTER EXCEPTION procedure must be
      *> associated with the file connector and that procedure is executed" (§1985:VIII.1.3.5; §2002:9.1.13 item 3: "that
      *> USE AFTER EXCEPTION procedure is executed"). 24 is no permanent error, so nothing stays in effect: K5 is
      *> written ('00', no USE), DUP5 is the invalid key '22' and runs the procedure again, and ABOVE is '24' again.
      *>
      *> From 2014 (.out) the same attempt is a permanent error, I-O status 34: §2023:14.9.51.4 GR29 b) "If the relative key
      *> data item contains a value that is less than 1 or greater than the highest relative record number permitted for
      *> the file, the execution of the WRITE statement is unsuccessful, and the I O status for the write file connector is
      *> set to '34'" (first in §2014:14.9.47.3 GR26 b). The procedure runs for it, and the error "remains in effect for all
      *> subsequent input-output operations on the file" (§2023:9.1.13.1) until the CLOSE (this processor's technique,
      *> docs/CONFORMANCE.md DOC-A.1-105): K5, DUP5 and ABOVE each answer 34 and run the procedure, and nothing is written,
      *> so the READ of record 5 after the CLOSE is the invalid key '23', which runs the procedure once more.
      *>
      *> The boundaries are WiseOwl's (docs/CONFORMANCE.md DOC-A.1-107): relative record numbers 1 through 2 147 483 647.
      *> WHY EACH LEG CAN FAIL: a 24 routed as a permanent error below 2014 would leave it in effect (K5=24 with a USE
      *> line, READ5=23); a 24 that skipped the declarative would print no USE line before ZERO=; a 34 that did not stay in
      *> effect from 2014 would print K5=00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5125C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RV ASSIGN TO "pb5125c.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RV.
       01  RVR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(10).
       01  UC PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RV-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RV.
       RV-ERR-P.
           ADD 1 TO UC.
           DISPLAY "USE " UC " FS=" FS.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RV.
           MOVE 0 TO RK.
           MOVE "ZERO" TO RVR.
           WRITE RVR.
           DISPLAY "ZERO=" FS " " RVR.
           MOVE 5 TO RK.
           MOVE "FIVE" TO RVR.
           WRITE RVR.
           DISPLAY "K5=" FS.
           MOVE 5 TO RK.
           MOVE "DUP5" TO RVR.
           WRITE RVR.
           DISPLAY "DUP5=" FS.
           MOVE 2147483648 TO RK.
           MOVE "HIGH" TO RVR.
           WRITE RVR.
           DISPLAY "ABOVE=" FS " " RVR.
           CLOSE RV.
           DISPLAY "CLOSE=" FS.
           OPEN INPUT RV.
           MOVE 5 TO RK.
           READ RV.
           IF FS = "00"
               DISPLAY "READ5=" FS " " RVR
           ELSE
               DISPLAY "READ5=" FS.
           CLOSE RV.
           DISPLAY "UC=" UC.
           STOP RUN.
