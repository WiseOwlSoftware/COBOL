      *>--- conformance-case
      *> id: g85_f17_07
      *> rules: [1985-GR-IX.2.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.4 GR4 - data-name-1 need be described in only one
      *> record description; its character positions are implicitly
      *> keys for every other record description of the file. The
      *> alternate key ALT-A is described in REC-A only (positions 5-8);
      *> records are written through REC-A and through REC-B, whose
      *> positions 5-8 lie in B-DATA. Random READs by the alternate key
      *> find the REC-B records by the value placed in B-DATA(1:4). A
      *> compiler that keys only REC-A's writes reports 23 (INVALID).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1707.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85F17XK.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS KEY-A
               ALTERNATE RECORD KEY IS ALT-A
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  REC-A.
           05  KEY-A PIC X(4).
           05  ALT-A PIC X(4).
           05  A-REST PIC X(4).
       01  REC-B.
           05  B-HEAD PIC X(4).
           05  B-DATA PIC X(8).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT XF.
           MOVE "K001" TO KEY-A.
           MOVE "AAAA" TO ALT-A.
           MOVE "RECA" TO A-REST.
           WRITE REC-A INVALID KEY DISPLAY "WRITE A FAILED".
           MOVE "K002" TO B-HEAD.
           MOVE "BBBBRECB" TO B-DATA.
           WRITE REC-B INVALID KEY DISPLAY "WRITE B FAILED".
           MOVE "K003" TO B-HEAD.
           MOVE "CCCCRECC" TO B-DATA.
           WRITE REC-B INVALID KEY DISPLAY "WRITE C FAILED".
           CLOSE XF.
           OPEN INPUT XF.
           MOVE "CCCC" TO ALT-A.
           READ XF KEY IS ALT-A INVALID KEY DISPLAY "INVALID CCCC".
           DISPLAY "ALT CCCC " ST " " REC-B.
           MOVE "BBBB" TO ALT-A.
           READ XF KEY IS ALT-A INVALID KEY DISPLAY "INVALID BBBB".
           DISPLAY "ALT BBBB " ST " " REC-B.
           MOVE "AAAA" TO ALT-A.
           READ XF KEY IS ALT-A INVALID KEY DISPLAY "INVALID AAAA".
           DISPLAY "ALT AAAA " ST " " REC-A.
           CLOSE XF.
           STOP RUN.
