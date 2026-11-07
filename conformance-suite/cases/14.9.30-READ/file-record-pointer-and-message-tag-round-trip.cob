*>--- conformance-case
*> id: file-record-pointer-and-message-tag-round-trip
*> rules: [GR-14.9.30.4-13, GR-13.18.60.4-9, GR-13.18.60.4-23, GR-13.18.60.4-24, GR-13.18.5.4-4, DOC-A.1-212, DOC-A.1-216]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4838 - a FILE record (FD) whose record is a strongly-typed group holding a data-pointer, a program-pointer and
      *> a message-tag leaf (13.18.60.3 SR14 admits all of them only in a strongly-typed group).
      *> 14.9.30.4 GR13 c): "If no exception condition exists, the record is made available in the record area" - the record
      *> the file holds, so what a WRITE put into the file is what a later READ makes available, whatever the representation of
      *> its leaves. 13.18.60.4 GR23/GR24/GR26/GR9 leave the representation of a pointer and a message tag to the implementor;
      *> the owner decided (2026-11-06) that the file carries the leaf's 8-position token and that READ restores the reference
      *> it names within the run unit (docs/CONFORMANCE.md section 7, DOC-A.1-212 and DOC-A.1-216). So the pointer read equals the
      *> pointer written (8.8.4.2.16: equal when they reference the same address), the based item reached through it is the item
      *> addressed when the record was written, the program-pointer read equals the one written, and a message tag that was NULL
      *> (13.18.63.4 GR4) is NULL again.
      *> A token this run unit never produced (here: eight bytes of a plain record) is not NULL, and a reference through such a
      *> data-pointer is not a reference to a valid address of storage: 13.18.5.4 GR4 sets EC-BOUND-PTR. The message tag read from
      *> such eight bytes is likewise not NULL.
      *> The statement paths: WRITE to a file that is not open leaves I-O status 48 (14.9.51.4 / 9.1.13.1) and the file unchanged;
      *> a READ after the last record leaves status 10 and the record area as it was.
      *> (cite.py --check OK for 14.9.30.4 13), 13.18.5.4 4) and 13.18.60.4 9).)
      *> Expected: the records 07 and 09 come back with their pointers, REWRITE of record 07 with the other pointer is read back,
      *> the foreign token is not NULL, and the dereference is handled as EC-BOUND-PTR.
       >>TURN EC-BOUND-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILERECPTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "filerecptr.dat"
              ORGANIZATION SEQUENTIAL FILE STATUS FS.
           SELECT RAWF ASSIGN TO "filerecptr.raw"
              ORGANIZATION SEQUENTIAL FILE STATUS FS.
           SELECT XF ASSIGN TO "filerecptr.raw"
              ORGANIZATION SEQUENTIAL FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD TF.
       01 FREC TYPE T-REC.
       FD RAWF.
       01 RAW-REC.
          05 RAW-KEY PIC X(2).
          05 RAW-PTR PIC X(8).
          05 RAW-PRG PIC X(8).
          05 RAW-TAG PIC X(8).
       FD XF.
       01 XREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 TKEY PIC 9(2).
          05 TPTR USAGE POINTER.
          05 TPRG USAGE PROGRAM-POINTER.
          05 TTAG USAGE MESSAGE-TAG.
       01 WS-REC TYPE T-REC.
       01 WS-A PIC X(4) VALUE "AAAA".
       01 WS-B PIC X(4) VALUE "BBBB".
       01 BX PIC X(4) BASED.
       01 PP USAGE PROGRAM-POINTER.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BP SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-PTR.
       D-BP-P.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           SET PP TO ENTRY "TARGETPROG"
           OPEN OUTPUT TF
           MOVE 7 TO TKEY OF WS-REC
           SET TPTR OF WS-REC TO ADDRESS OF WS-A
           SET TPRG OF WS-REC TO PP
           SET TTAG OF WS-REC TO NULL
           MOVE WS-REC TO FREC
           WRITE FREC
           DISPLAY "WRITE " FS
           MOVE 9 TO TKEY OF WS-REC
           SET TPTR OF WS-REC TO ADDRESS OF WS-B
           MOVE WS-REC TO FREC
           WRITE FREC
           DISPLAY "WRITE " FS
           CLOSE TF
           WRITE FREC
           DISPLAY "WRITE CLOSED " FS
           OPEN INPUT TF
           PERFORM 3 TIMES
              READ TF
              DISPLAY "READ " FS
              IF FS = "00"
                 SET ADDRESS OF BX TO TPTR OF FREC
                 DISPLAY TKEY OF FREC " " BX
                 IF TPRG OF FREC = PP
                    DISPLAY "PROGRAM-POINTER SAME"
                 ELSE
                    DISPLAY "PROGRAM-POINTER DIFFERENT"
                 END-IF
                 IF TPRG OF FREC = NULL
                    DISPLAY "PROGRAM-POINTER NULL"
                 END-IF
                 IF TTAG OF FREC = NULL
                    DISPLAY "TAG NULL"
                 END-IF
              ELSE
                 SET ADDRESS OF BX TO TPTR OF FREC
                 DISPLAY TKEY OF FREC " " BX " unchanged"
              END-IF
           END-PERFORM
           CLOSE TF
           OPEN I-O TF
           READ TF
           SET TPTR OF FREC TO ADDRESS OF WS-B
           REWRITE FREC
           DISPLAY "REWRITE " FS
           CLOSE TF
           OPEN INPUT TF
           READ TF
           SET ADDRESS OF BX TO TPTR OF FREC
           DISPLAY TKEY OF FREC " " BX
           CLOSE TF
           OPEN OUTPUT RAWF
           MOVE "11" TO RAW-KEY
           MOVE X"7F00000000000001" TO RAW-PTR
           MOVE X"7F00000000000002" TO RAW-PRG
           MOVE X"0000000000000009" TO RAW-TAG
           WRITE RAW-REC
           CLOSE RAWF
           OPEN INPUT XF
           READ XF
           DISPLAY "FOREIGN READ " FS
           IF TPTR OF XREC = NULL
              DISPLAY "FOREIGN POINTER NULL"
           ELSE
              DISPLAY "FOREIGN POINTER NOT NULL"
           END-IF
           IF TTAG OF XREC = NULL
              DISPLAY "FOREIGN TAG NULL"
           ELSE
              DISPLAY "FOREIGN TAG NOT NULL"
           END-IF
           SET ADDRESS OF BX TO TPTR OF XREC
           DISPLAY BX
           DISPLAY "DONE"
           CLOSE XF
           STOP RUN.
       END PROGRAM FILERECPTR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TARGETPROG.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM TARGETPROG.
