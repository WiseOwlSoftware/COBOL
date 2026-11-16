      *>--- conformance-case
      *> id: g85_bf30_02
      *> rules: [1985-GR-VIII.4.5.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR14 - for a relative file with the RELATIVE
      *> KEY phrase, a format 1 READ "moves the relative record number
      *> of the record made available to the relative key data item
      *> according to the rules for the MOVE statement".
      *> The file holds records 2, 5 and 7 only (written by key,
      *> §1985:VIII.4.9.4 GR11b), so the stored numbers are not a
      *> count of reads.  The key item is clobbered (9999 / 99) before
      *> EVERY read, so each printed key can only come from the store
      *> GR14 requires; a missing store prints 9999 or 99.
      *> Leg D: dynamic access, READ NEXT (§1985:VIII.4.5.4 GR4c and
      *> GR4d select 2, then 5, then 7): KD must read 0002, 0005, 0007.
      *> Leg S: sequential access with RELATIVE KEY (the ACCESS MODE
      *> format's SEQUENTIAL [RELATIVE KEY IS data-name-1]) over the
      *> same physical file: KS is PIC 9(2), so the MOVE rules give
      *> 02, 05, 07 (a store of the raw 4-digit number would not fit).
      *> The fourth read of each leg is the at end condition: status 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3002.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT D ASSIGN TO "g85bf3002.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS KD
               FILE STATUS IS DS.
           SELECT S ASSIGN TO "g85bf3002.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS KS
               FILE STATUS IS SS.
       DATA DIVISION.
       FILE SECTION.
       FD D.
       01 D-REC PIC X(4).
       FD S.
       01 S-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 DS PIC XX.
       01 SS PIC XX.
       01 KD PIC 9(4).
       01 KS PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT D.
           MOVE 5 TO KD.
           MOVE "R005" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "W5-IK".
           MOVE 2 TO KD.
           MOVE "R002" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "W2-IK".
           MOVE 7 TO KD.
           MOVE "R007" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "W7-IK".
           CLOSE D.
           OPEN INPUT D.
           MOVE 9999 TO KD.
           READ D NEXT AT END DISPLAY "D1-ATEND".
           DISPLAY "D1=" DS " " KD " " D-REC.
           MOVE 9999 TO KD.
           READ D NEXT AT END DISPLAY "D2-ATEND".
           DISPLAY "D2=" DS " " KD " " D-REC.
           MOVE 9999 TO KD.
           READ D NEXT AT END DISPLAY "D3-ATEND".
           DISPLAY "D3=" DS " " KD " " D-REC.
           READ D NEXT AT END DISPLAY "D4-ATEND=" DS.
           CLOSE D.
           OPEN INPUT S.
           MOVE 99 TO KS.
           READ S AT END DISPLAY "S1-ATEND".
           DISPLAY "S1=" SS " " KS " " S-REC.
           MOVE 99 TO KS.
           READ S AT END DISPLAY "S2-ATEND".
           DISPLAY "S2=" SS " " KS " " S-REC.
           MOVE 99 TO KS.
           READ S AT END DISPLAY "S3-ATEND".
           DISPLAY "S3=" SS " " KS " " S-REC.
           READ S AT END DISPLAY "S4-ATEND=" SS.
           CLOSE S.
           STOP RUN.
