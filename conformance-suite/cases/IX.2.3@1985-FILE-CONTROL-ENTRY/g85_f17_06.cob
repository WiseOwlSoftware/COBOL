      *>--- conformance-case
      *> id: g85_f17_06
      *> rules: [1985-GR-IX.2.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR7 - the FILE STATUS clause of an indexed file is
      *> the Sequential I-O clause, its content the indexed I-O status
      *> (§1985:IX.1.3.4): OPEN 00; WRITE 00; WRITE making a duplicate
      *> alternate key that has WITH DUPLICATES 02; WRITE of a duplicate
      *> prime key 22; READ of an absent key 23; READ of a present key 00;
      *> READ NEXT 00 then 10 at the end. Each status item is set to XX
      *> first, so an item the statement did not update shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1706.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85F17XS.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XF-KEY
               ALTERNATE RECORD KEY IS XF-ALT WITH DUPLICATES
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XF-REC.
           05  XF-KEY PIC X(4).
           05  XF-ALT PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           MOVE "XX" TO ST.
           OPEN OUTPUT XF.
           DISPLAY "OPEN OUTPUT " ST.
           MOVE "K001ALTA" TO XF-REC.
           MOVE "XX" TO ST.
           WRITE XF-REC INVALID KEY DISPLAY "INVALID K001".
           DISPLAY "WRITE K001 " ST.
           MOVE "K002ALTA" TO XF-REC.
           MOVE "XX" TO ST.
           WRITE XF-REC INVALID KEY DISPLAY "INVALID K002".
           DISPLAY "WRITE K002 " ST.
           MOVE "K001ALTB" TO XF-REC.
           MOVE "XX" TO ST.
           WRITE XF-REC INVALID KEY DISPLAY "INVALID DUP".
           DISPLAY "WRITE DUP " ST.
           MOVE "XX" TO ST.
           CLOSE XF.
           DISPLAY "CLOSE " ST.
           MOVE "XX" TO ST.
           OPEN INPUT XF.
           DISPLAY "OPEN INPUT " ST.
           MOVE "K009" TO XF-KEY.
           MOVE "XX" TO ST.
           READ XF INVALID KEY DISPLAY "INVALID K009".
           DISPLAY "READ K009 " ST.
           MOVE "K001" TO XF-KEY.
           MOVE "XX" TO ST.
           READ XF INVALID KEY DISPLAY "INVALID K001".
           DISPLAY "READ K001 " ST " " XF-REC.
           MOVE "XX" TO ST.
           READ XF NEXT AT END DISPLAY "AT END 1".
           DISPLAY "NEXT " ST " " XF-REC.
           MOVE "XX" TO ST.
           READ XF NEXT AT END DISPLAY "AT END 2".
           DISPLAY "NEXT " ST.
           CLOSE XF.
           STOP RUN.
