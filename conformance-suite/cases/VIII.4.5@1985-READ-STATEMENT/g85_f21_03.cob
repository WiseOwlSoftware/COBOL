      *>--- conformance-case
      *> id: g85_f21_03
      *> rules: [1985-SR-VIII.4.5.3-2, 1985-SR-VIII.4.5.3-4, 1985-GR-VIII.4.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> One relative file, G85F2103.DAT, under three file-names, one
      *> for each access mode, opened one at a time.
      *> §1985:VIII.4.5.3 SR2 - "Format 1 must be used for all files in
      *> sequential access mode": REL-S (sequential access) is read
      *> only with format 1 (AT END), S1..S4.
      *> §1985:VIII.4.5.4 GR2 - "For files in sequential access mode,
      *> the NEXT phrase is optional and has no effect": S1 and S3 use
      *> NEXT, S2 and S4 do not; the records come back in relative
      *> record number order 1, 2, 3 and then the at end condition
      *> (status 10), as plain READs would give.
      *> §1985:VIII.4.5.3 SR4 - "Format 2 is used for files in random
      *> access mode or for files in dynamic access mode when records
      *> are to be retrieved randomly": REL-R (random) and REL-D
      *> (dynamic) are read with format 2 (INVALID KEY) and get the
      *> record the relative key names (R2, D3, D1), or the invalid
      *> key condition for a record that does not exist (R5, status
      *> 23).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2103.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-S ASSIGN TO "G85F2103.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT REL-R ASSIGN TO "G85F2103.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-STAT.
           SELECT REL-D ASSIGN TO "G85F2103.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-DK
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-S
           LABEL RECORDS ARE STANDARD.
       01  REL-S-REC PIC X(3).
       FD  REL-R
           LABEL RECORDS ARE STANDARD.
       01  REL-R-REC PIC X(3).
       FD  REL-D
           LABEL RECORDS ARE STANDARD.
       01  REL-D-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-RK PIC 9(4).
       01  WS-DK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT REL-S.
           MOVE "AAA" TO REL-S-REC.
           WRITE REL-S-REC INVALID KEY DISPLAY "W INVALID".
           MOVE "BBB" TO REL-S-REC.
           WRITE REL-S-REC INVALID KEY DISPLAY "W INVALID".
           MOVE "CCC" TO REL-S-REC.
           WRITE REL-S-REC INVALID KEY DISPLAY "W INVALID".
           CLOSE REL-S.
           OPEN INPUT REL-S.
           READ REL-S NEXT RECORD AT END DISPLAY "S1 AT END".
           DISPLAY "S1 " REL-S-REC " " WS-STAT.
           READ REL-S RECORD AT END DISPLAY "S2 AT END".
           DISPLAY "S2 " REL-S-REC " " WS-STAT.
           READ REL-S NEXT AT END DISPLAY "S3 AT END".
           DISPLAY "S3 " REL-S-REC " " WS-STAT.
           READ REL-S AT END DISPLAY "S4 AT END " WS-STAT.
           CLOSE REL-S.
           OPEN INPUT REL-R.
           MOVE 2 TO WS-RK.
           READ REL-R RECORD
               INVALID KEY DISPLAY "R2 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R2 " REL-R-REC " " WS-STAT
           END-READ.
           MOVE 5 TO WS-RK.
           READ REL-R
               INVALID KEY DISPLAY "R5 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R5 FOUND"
           END-READ.
           CLOSE REL-R.
           OPEN INPUT REL-D.
           MOVE 3 TO WS-DK.
           READ REL-D RECORD
               INVALID KEY DISPLAY "D3 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "D3 " REL-D-REC " " WS-STAT
           END-READ.
           MOVE 1 TO WS-DK.
           READ REL-D
               INVALID KEY DISPLAY "D1 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "D1 " REL-D-REC " " WS-STAT
           END-READ.
           CLOSE REL-D.
           STOP RUN.
