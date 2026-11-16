      *>--- conformance-case
      *> id: g85_f16_04
      *> rules: [1985-GR-IX.1.3.4-3, 1985-GR-IX.1.3.4-L2.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR3 - the invalid key class: the statement is
      *> unsuccessful as a result of an invalid key condition, so every
      *> leg below must show status key 1 = 2 and take the INVALID
      *> KEY branch (BR = IK).
      *> §1985:IX.1.3.4 GRL2.3 - its values, one leg per case:
      *>  S21W  a. ascending sequence violated: sequential access,
      *>        open OUTPUT, A001 written after A002 - 21.
      *>  S21R  a. the prime key changed between a successful READ and
      *>        the next REWRITE - 21.
      *>  D22P  b.1) WRITE of a duplicate prime key - 22.
      *>  D22A  b.2) WRITE of a duplicate alternate key, no DUPLICATES
      *>        phrase - 22.
      *>  D23R  c.1) random READ of a record not in the file - 23.
      *>  O23R  c.2) random READ of an optional input file that is not
      *>  O23S  present, and START of it - 23 (its OPEN gives 05).
      *> Item d (24, the externally defined boundary) is the host's
      *> file size; a run that reaches it is not made here.
      *> A leg fails if the statement succeeds, if another value or
      *> class is set, or if NOT INVALID KEY runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1604.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "g85f1604s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS SQ-KEY
               FILE STATUS IS FS.
           SELECT DYNF ASSIGN TO "g85f1604d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DY-KEY
               ALTERNATE RECORD KEY IS DY-ALT
               FILE STATUS IS FS.
           SELECT OPTIONAL OPTF ASSIGN TO "g85f1604o.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS OP-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF
           LABEL RECORDS ARE STANDARD.
       01  SQREC.
           05 SQ-KEY           PIC X(4).
           05 SQ-DATA          PIC X(6).
       FD  DYNF
           LABEL RECORDS ARE STANDARD.
       01  DYREC.
           05 DY-KEY           PIC X(4).
           05 DY-ALT           PIC X(4).
       FD  OPTF
           LABEL RECORDS ARE STANDARD.
       01  OPREC.
           05 OP-KEY           PIC X(4).
           05 OP-DATA          PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS.
           05 FS-1             PIC X.
           05 FS-2             PIC X.
       01  BR                  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-LOGIC SECTION.
       M-S21W.
           OPEN OUTPUT SQF.
           MOVE "A002" TO SQ-KEY.
           MOVE "TWO" TO SQ-DATA.
           MOVE "NO" TO BR.
           WRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "W1 " FS " " BR.
           MOVE "A001" TO SQ-KEY.
           MOVE "ONE" TO SQ-DATA.
           MOVE "NO" TO BR.
           WRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "S21W " FS " KEY1 " FS-1 " " BR.
           CLOSE SQF.
       M-S21R.
           OPEN I-O SQF.
           READ SQF AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "R1 " FS " " SQREC.
           MOVE "A009" TO SQ-KEY.
           MOVE "NO" TO BR.
           REWRITE SQREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-REWRITE.
           DISPLAY "S21R " FS " KEY1 " FS-1 " " BR.
           CLOSE SQF.
       M-D22.
           OPEN OUTPUT DYNF.
           MOVE "K001" TO DY-KEY.
           MOVE "X001" TO DY-ALT.
           MOVE "NO" TO BR.
           WRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "W2 " FS " " BR.
           MOVE "X002" TO DY-ALT.
           MOVE "NO" TO BR.
           WRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "D22P " FS " KEY1 " FS-1 " " BR.
           MOVE "K002" TO DY-KEY.
           MOVE "X001" TO DY-ALT.
           MOVE "NO" TO BR.
           WRITE DYREC INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-WRITE.
           DISPLAY "D22A " FS " KEY1 " FS-1 " " BR.
           CLOSE DYNF.
       M-D23.
           OPEN INPUT DYNF.
           MOVE "K009" TO DY-KEY.
           MOVE "NO" TO BR.
           READ DYNF INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-READ.
           DISPLAY "D23R " FS " KEY1 " FS-1 " " BR.
           CLOSE DYNF.
       M-O23.
           OPEN INPUT OPTF.
           DISPLAY "OPEN-OPT " FS.
           MOVE "P001" TO OP-KEY.
           MOVE "NO" TO BR.
           READ OPTF INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-READ.
           DISPLAY "O23R " FS " KEY1 " FS-1 " " BR.
           MOVE "NO" TO BR.
           START OPTF KEY IS EQUAL TO OP-KEY
               INVALID KEY MOVE "IK" TO BR
               NOT INVALID KEY MOVE "NIK" TO BR
           END-START.
           DISPLAY "O23S " FS " KEY1 " FS-1 " " BR.
           CLOSE OPTF.
           STOP RUN.
