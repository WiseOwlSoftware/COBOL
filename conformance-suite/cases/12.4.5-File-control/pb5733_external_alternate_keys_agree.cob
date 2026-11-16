      *>--- conformance-case
      *> id: pb5733_external_alternate_keys_agree
      *> rules: [GR-12.4.5.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5733 - 2023:12.4.5.3 GR1: "all file control entries in the run unit that reference this file
      *> connector shall have" the same data description entry and relative location for each alternate record key and
      *> the same number of alternate record keys (1985:IX.2.5.4 GR5; 2002:12.3.4.3 GR1). Two programs describe one
      *> EXTERNAL indexed file with the same TWO alternate keys, one WITH DUPLICATES. The file connector is one, so it
      *> has two alternate keys, not four: B's OPEN INPUT succeeds and a READ by the SECOND alternate key
      *> A2 finds record 2 through the one set of keys. A run that registered the keys again ends the run unit
      *> with status 39 on that OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5733A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5733X.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K1
               ALTERNATE RECORD KEY IS A1 WITH DUPLICATES
               ALTERNATE RECORD KEY IS A2.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  R1.
           05  K1 PIC X(2).
           05  A1 PIC X(2).
           05  A2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XF.
           MOVE "01AAXX" TO R1.
           WRITE R1 INVALID KEY DISPLAY "W1".
           MOVE "02AAYY" TO R1.
           WRITE R1 INVALID KEY DISPLAY "W2".
           CLOSE XF.
           DISPLAY "A DONE".
           CALL "PB5733B".
           STOP RUN.
       END PROGRAM PB5733A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5733B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "PB5733X.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K1
               ALTERNATE RECORD KEY IS A1 WITH DUPLICATES
               ALTERNATE RECORD KEY IS A2.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  R1.
           05  K1 PIC X(2).
           05  A1 PIC X(2).
           05  A2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT XF.
           MOVE "YY" TO A2.
           READ XF KEY IS A2 INVALID KEY DISPLAY "NOTFOUND".
           DISPLAY R1.
           CLOSE XF.
           EXIT PROGRAM.
       END PROGRAM PB5733B.
