      *>--- conformance-case
      *> id: g85_bf29_02
      *> rules: [1985-GR-VIII.4.5.4-4, 1985-GR-VIII.4.5.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:VIII.4.5.4 GR4 - how the file position indicator picks the
      * record a format 1 READ makes available, every branch:
      *   c. set by OPEN (to 1, §1985:VIII.4.4.4 GR10) or START: the first
      *      record whose number is >= the indicator (N1 gets 2; N2 after
      *      START at 5 gets 5 itself, the "or equal");
      *   d. set by a READ: the first record whose number is > it (N3 gets 7
      *      after 5; N6 gets 5 after a random READ of 2, skipping 3 and 4);
      *   found but more significant digits than the RELATIVE KEY item: the
      *      at end of GR10 (N4: record 12 against DK PIC 9 -> 14);
      *   a. no valid next record (GR12, after N4): unsuccessful, status 46
      *      (§1985:VIII.1.3.4), the USE procedure runs, AT END does not (N5);
      *   none found: no next logical record, at end 10 (E2);
      *   b. optional input file not present: GR10, at end 10 (O1).
      * §1985:VIII.4.5.4 GR13 - READ ... NEXT on a DYNAMIC relative file
      * retrieves the next logical record (N1-N3, N6, E1).
      * A leg fails if a branch selects the wrong record or status.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2902.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "g85bf2902.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WK
               FILE STATUS IS FS-W.
           SELECT RX ASSIGN TO "g85bf2902.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS DK
               FILE STATUS IS FS-D.
           SELECT RE ASSIGN TO "g85bf2902.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS EK
               FILE STATUS IS FS-E.
           SELECT OPTIONAL RO ASSIGN TO "g85bf2902n.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS OK
               FILE STATUS IS FS-O.
       DATA DIVISION.
       FILE SECTION.
       FD  RW.
       01  WREC PIC X(5).
       FD  RX.
       01  DREC PIC X(5).
       FD  RE.
       01  EREC PIC X(5).
       FD  RO.
       01  OREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WK   PIC 99.
       01  DK   PIC 9.
       01  EK   PIC 99.
       01  OK   PIC 99.
       01  FS-W PIC XX.
       01  FS-D PIC XX.
       01  FS-E PIC XX.
       01  FS-O PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RX-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RX.
       RX-ERR-PARA.
           DISPLAY "USE RX " FS-D.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           OPEN OUTPUT RW.
           MOVE 2 TO WK.
           MOVE "REC02" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W2 INVALID " FS-W.
           MOVE 5 TO WK.
           MOVE "REC05" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W5 INVALID " FS-W.
           MOVE 7 TO WK.
           MOVE "REC07" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W7 INVALID " FS-W.
           MOVE 12 TO WK.
           MOVE "REC12" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W12 INVALID " FS-W.
           CLOSE RW.
           OPEN INPUT RX.
           READ RX NEXT AT END DISPLAY "N1 AT END " FS-D
               NOT AT END DISPLAY "N1 " DREC " " FS-D " " DK.
           MOVE 5 TO DK.
           START RX KEY IS EQUAL TO DK
               INVALID KEY DISPLAY "ST INVALID " FS-D.
           READ RX NEXT AT END DISPLAY "N2 AT END " FS-D
               NOT AT END DISPLAY "N2 " DREC " " FS-D " " DK.
           READ RX NEXT AT END DISPLAY "N3 AT END " FS-D
               NOT AT END DISPLAY "N3 " DREC " " FS-D " " DK.
           READ RX NEXT AT END DISPLAY "N4 AT END " FS-D
               NOT AT END DISPLAY "N4 " DREC " " FS-D " " DK.
           READ RX NEXT AT END DISPLAY "N5 AT END " FS-D.
           DISPLAY "N5 " FS-D.
           CLOSE RX.
           OPEN INPUT RX.
           MOVE 2 TO DK.
           READ RX INVALID KEY DISPLAY "R1 INVALID " FS-D
               NOT INVALID KEY DISPLAY "R1 " DREC " " FS-D.
           READ RX NEXT AT END DISPLAY "N6 AT END " FS-D
               NOT AT END DISPLAY "N6 " DREC " " FS-D " " DK.
           CLOSE RX.
           OPEN INPUT RE.
           MOVE 12 TO EK.
           START RE KEY IS EQUAL TO EK
               INVALID KEY DISPLAY "SE INVALID " FS-E.
           READ RE NEXT AT END DISPLAY "E1 AT END " FS-E
               NOT AT END DISPLAY "E1 " EREC " " FS-E " " EK.
           READ RE NEXT AT END DISPLAY "E2 AT END " FS-E
               NOT AT END DISPLAY "E2 " EREC " " FS-E " " EK.
           CLOSE RE.
           OPEN INPUT RO.
           DISPLAY "O0 OPEN " FS-O.
           READ RO NEXT AT END DISPLAY "O1 AT END " FS-O
               NOT AT END DISPLAY "O1 READ " FS-O.
           CLOSE RO.
           STOP RUN.
