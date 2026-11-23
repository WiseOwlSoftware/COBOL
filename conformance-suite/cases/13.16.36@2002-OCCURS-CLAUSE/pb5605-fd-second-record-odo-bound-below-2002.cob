*>--- conformance-case
*> id: pb5605-fd-second-record-odo-bound-below-2002
*> rules: [2002-GR-13.16.36.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5605 - THE EDITION FLOOR OF §2002:13.16.36.3 GR7's EC-BOUND-ODO.
      *>   cite.py --check "§2002:13.16.36.3 GR7" "If the value of the data item does not fall within the specified bounds, the EC-BOUND-ODO" -> OK
      *> A program can request that the condition be checked only from COBOL-2002 onward: the >>TURN directive (§2023:7.3.25)
      *> is a 2002 introduction, so at --std 85 the version-conformance pass rejects it (COBOL0900). The FD with two records and
      *> the OCCURS DEPENDING table are legal COBOL-85; delete the first line and it compiles at 85. The positive half is
      *> pb5605-fd-second-record-odo-bound-2002.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5605N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5605n.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC X(11).
       01 R2.
          05 H PIC X.
          05 U OCCURS 2 TO 5 DEPENDING ON N.
             10 UX PIC X.
             10 UE PIC 9.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "HA1B2C3D4E5" TO R2
           DISPLAY UX(1)
           STOP RUN.
