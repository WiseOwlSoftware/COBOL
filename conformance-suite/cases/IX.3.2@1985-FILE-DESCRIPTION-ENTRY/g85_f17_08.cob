      *>--- conformance-case
      *> id: g85_f17_08
      *> rules: [1985-GR-IX.3.2.4-1, 1985-GR-IX.3.2.4-2, 1985-GR-IX.3.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Two indexed files. §1985:IX.3.2.4 GR1 - each FD associates its
      *> file-name with its own connector: line READ XA K001 shows XA's
      *> record after XB wrote its own K001. §1985:IX.3.2.4 GR2 - the
      *> Sequential I-O BLOCK CONTAINS clause: XA states 2 RECORDS, XB
      *> 20 TO 400 CHARACTERS (§1985:VII.3.3.3 GR4). §1985:IX.3.2.4 GR5 -
      *> the Sequential I-O RECORD clause: XA format 1 (CONTAINS 10
      *> CHARACTERS), XB format 2 (VARYING IN SIZE FROM 4 TO 12 DEPENDING
      *> ON XB-LEN): after each READ XB-LEN holds the length of the
      *> record read (§1985:VII.3.8.4 GR11), 12 and 04. XB-LEN is set to
      *> 99 before each READ, so a length not stored shows 99.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1708.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "G85F17XA.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XA-KEY.
           SELECT XB ASSIGN TO "G85F17XB.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XB-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XA
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 10 CHARACTERS.
       01  XA-REC.
           05  XA-KEY PIC X(4).
           05  XA-DATA PIC X(6).
       FD  XB
           BLOCK CONTAINS 20 TO 400 CHARACTERS
           RECORD IS VARYING IN SIZE FROM 4 TO 12 CHARACTERS
               DEPENDING ON XB-LEN.
       01  XB-SHORT.
           05  XB-KEY PIC X(4).
       01  XB-LONG.
           05  XB-LKEY PIC X(4).
           05  XB-LDATA PIC X(8).
       WORKING-STORAGE SECTION.
       01  XB-LEN PIC 99.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT XA XB.
           MOVE "K001" TO XA-KEY.
           MOVE "XA-ONE" TO XA-DATA.
           WRITE XA-REC INVALID KEY DISPLAY "XA WRITE FAILED".
           MOVE 4 TO XB-LEN.
           MOVE "K001" TO XB-KEY.
           WRITE XB-SHORT INVALID KEY DISPLAY "XB WRITE 1 FAILED".
           MOVE 12 TO XB-LEN.
           MOVE "K002" TO XB-LKEY.
           MOVE "XB-LONG2" TO XB-LDATA.
           WRITE XB-LONG INVALID KEY DISPLAY "XB WRITE 2 FAILED".
           CLOSE XA XB.
           OPEN INPUT XA XB.
           MOVE "K001" TO XA-KEY.
           READ XA INVALID KEY DISPLAY "XA READ FAILED".
           DISPLAY "READ XA K001 " XA-REC.
           MOVE "K002" TO XB-KEY.
           MOVE 99 TO XB-LEN.
           READ XB INVALID KEY DISPLAY "XB READ 2 FAILED".
           DISPLAY "READ XB K002 " XB-LONG " LEN " XB-LEN.
           MOVE "K001" TO XB-KEY.
           MOVE 99 TO XB-LEN.
           READ XB INVALID KEY DISPLAY "XB READ 1 FAILED".
           DISPLAY "READ XB K001 " XB-SHORT " LEN " XB-LEN.
           CLOSE XA XB.
           STOP RUN.
