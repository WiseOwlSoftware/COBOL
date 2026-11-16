      *>--- conformance-case
      *> id: g85_bf29_05
      *> rules: [1985-GR-IX.4.5.4-13, 1985-GR-IX.4.5.4-15, 1985-GR-IX.4.5.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:IX.4.5.4 GR13 - READ ... NEXT on a DYNAMIC indexed file
      * retrieves the next logical record: after OPEN (prime key of
      * reference, §1985:IX.4.4.4 GR10) N1 and N2 give 01 then 02.
      * §1985:IX.4.5.4 GR15 - a format 2 READ with KEY IS R-ALT makes the
      * alternate key the key of reference for that retrieval (A1 finds 02
      * by "CC") and, in dynamic mode, for the format 1 READs after it: A2
      * is the record after CC in ALTERNATE order, DD = 01 (prime order
      * would give 03), and A3 is the at end 10 (DD is the last alternate).
      * §1985:IX.4.5.4 GR16 - a format 2 READ with no KEY phrase makes the
      * PRIME key the key of reference, and in dynamic mode for the READs
      * after it: B1 re-establishes the alternate, B2 reads 01 with no KEY
      * phrase, so B3 must give 02 (prime order); had the alternate stood,
      * B3 would be the at end 10 (nothing follows DD).
      * Prime 01..04 against alternate AA=04 .. DD=01 are exact opposites,
      * so each of A2 and B3 is the answer only one key of reference gives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2905.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85bf2905.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 R-KEY PIC X(2).
           05 R-ALT PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "01DD" TO R.
           WRITE R INVALID KEY DISPLAY "W01 INVALID " FS.
           MOVE "02CC" TO R.
           WRITE R INVALID KEY DISPLAY "W02 INVALID " FS.
           MOVE "03BB" TO R.
           WRITE R INVALID KEY DISPLAY "W03 INVALID " FS.
           MOVE "04AA" TO R.
           WRITE R INVALID KEY DISPLAY "W04 INVALID " FS.
           CLOSE F.
           OPEN INPUT F.
           READ F NEXT AT END DISPLAY "N1 AT END " FS.
           DISPLAY "N1=" FS " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "N2 AT END " FS.
           DISPLAY "N2=" FS " " R-KEY "/" R-ALT.
           CLOSE F.
           OPEN INPUT F.
           MOVE "CC" TO R-ALT.
           READ F KEY IS R-ALT INVALID KEY DISPLAY "A1 INVALID".
           DISPLAY "A1=" FS " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A2 AT END".
           DISPLAY "A2=" FS " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "A3 AT END " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "CC" TO R-ALT.
           READ F KEY IS R-ALT INVALID KEY DISPLAY "B1 INVALID".
           DISPLAY "B1=" FS " " R-KEY "/" R-ALT.
           MOVE "01" TO R-KEY.
           READ F INVALID KEY DISPLAY "B2 INVALID".
           DISPLAY "B2=" FS " " R-KEY "/" R-ALT.
           READ F NEXT AT END DISPLAY "B3 AT END " FS.
           DISPLAY "B3=" FS " " R-KEY "/" R-ALT.
           CLOSE F.
           STOP RUN.
