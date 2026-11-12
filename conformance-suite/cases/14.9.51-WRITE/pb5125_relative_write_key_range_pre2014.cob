      *>--- conformance-case
      *> id: pb5125_relative_write_key_range_pre2014
      *> rules: [GR-14.9.51.4-29, GR-14.9.51.4-33, DOC-A.1-107]
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5125 - a random or dynamic WRITE of a relative file whose key is outside the file's boundaries, below 2014:
      *> I-O status 24, the invalid key condition, so the INVALID KEY phrase runs.
      *>
      *> §1985:VIII.1.3.4 (cite.py --check): "I-O Status = 24. An attempt is made to write beyond the externally defined
      *> boundaries of a relative file. The implementor specifies the manner in which these boundaries are defined." The
      *> 1985 relative-file status list has no 34, and §1985:VIII.1.3.5 makes 24 an invalid key condition. §2002:14.8.47.3
      *> GR29 b) says the same ("When an attempt is made to write beyond the externally defined boundaries of the file, the
      *> I-O status associated with the write file connector is set to '24'") and its GR25 b) has no status 34 for a
      *> relative key out of range; the 34 sentence of §2023:14.9.51.4 GR29 b) is new in 2014 (§2014:14.9.47.3 GR26 b): see
      *> pb5125_relative_write_key_range.
      *>
      *> The boundaries are WiseOwl's (docs/CONFORMANCE.md DOC-A.1-107): relative record numbers 1 through 2 147 483 647.
      *> K3 is inside them: 00, NOT INVALID KEY. ABOVE (2 147 483 648) and ZERO (a key no relative record number can be)
      *> are outside: 24 and the INVALID KEY branch, the record area unaffected ("HIGH" / "ZERO" stay in RVR, GR15).
      *> Neither is released: the READ walk finds record 3 and then the at-end 10. 24 is no permanent error, so the
      *> CLOSE and the reopening in between are plain.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5125A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RV ASSIGN TO "pb5125a.dat"
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
