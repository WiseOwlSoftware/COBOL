      *>--- conformance-case
      *> id: pb5125_relative_write_key_range
      *> rules: [GR-14.9.51.4-29, DOC-A.1-107]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5125 - a random or dynamic WRITE of a relative file whose key is outside the file's boundaries, from 2023:
      *> I-O status 34, a permanent error, so neither the INVALID KEY nor the NOT INVALID KEY phrase runs.
      *>
      *> §2023:14.9.51.4 GR29 b) (cite.py --check): "If the relative key data item contains a value that is less than 1 or
      *> greater than the highest relative record number permitted for the file, the execution of the WRITE statement is
      *> unsuccessful, and the I O status for the write file connector is set to '34'." The earlier editions answer 24
      *> and the invalid key condition for the same attempt: see pb5125_relative_write_key_range_pre2023.
      *>
      *> The highest permitted number is WiseOwl's 2 147 483 647 (docs/CONFORMANCE.md DOC-A.1-107).  K3 is inside the range:
      *> 00, NOT INVALID KEY.  ABOVE (2 147 483 648) and ZERO are outside: 34, and BR stays NONE.  The record area is
      *> unaffected (GR15).  The permanent error stays in effect until the CLOSE (§2023:9.1.13.1), which completes normally
      *> and ends it.  Neither key is released: the READ walk finds record 3 and then the at-end 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5125B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RV ASSIGN TO "pb5125b.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RV.
       01  RVR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  RK PIC 9(10).
       01  BR PIC X(7).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RV.
           MOVE 3 TO RK.
           MOVE "THRE" TO RVR.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "K3=" FS " " BR.
           MOVE 2147483648 TO RK.
           MOVE "HIGH" TO RVR.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "ABOVE=" FS " " BR " " RVR.
           CLOSE RV.
           DISPLAY "CLOSE1=" FS.
           OPEN I-O RV.
           MOVE 0 TO RK.
           MOVE "ZERO" TO RVR.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "ZERO=" FS " " BR " " RVR.
           CLOSE RV.
           DISPLAY "CLOSE2=" FS.
           OPEN INPUT RV.
           MOVE SPACES TO RVR.
           READ RV NEXT RECORD AT END DISPLAY "READ1 AT END".
           DISPLAY "READ1=" FS " " RK " " RVR.
           READ RV NEXT RECORD AT END DISPLAY "READ2 AT END".
           DISPLAY "READ2=" FS.
           CLOSE RV.
           STOP RUN.
