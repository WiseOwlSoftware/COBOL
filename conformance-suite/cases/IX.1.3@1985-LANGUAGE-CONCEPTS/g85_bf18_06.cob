      *>--- conformance-case
      *> id: g85_bf18_06
      *> rules: [1985-GR-IX.1.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR1 - "Successful Completion. The input-output
      *> statement was successfully executed." Every OPEN, WRITE, CLOSE,
      *> READ, REWRITE, DELETE and START below on an indexed file
      *> succeeds. Each reports a Successful Completion value
      *> (§1985:IX.1.3.4 GRL2.1): 00 where no further information
      *> applies, and 02 where a duplicate key is detected - the WRITE
      *> of a record whose alternate key (WITH DUPLICATES) repeats one
      *> already in the file (item b 2), and a READ NEXT under that
      *> alternate key whose next record holds the same value (item
      *> b 1). By §1985:IX.1.3.4 no USE AFTER STANDARD EXCEPTION
      *> procedure runs for a Successful Completion condition, the 02s
      *> included: USE-COUNT stays 0. A last READ NEXT with no AT END
      *> phrase is the at end condition, which is not successful
      *> (§1985:IX.4.5.4 GR10c: the procedure runs once, so the 0 is
      *> not a procedure that never fires. Each leg fails on a wrong
      *> status, a wrong record, or a USE procedure run for a success.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1806.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "G85BF1806.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05  IX-KEY PIC XX.
           05  IX-ALT PIC XX.
           05  IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01  IX-ST PIC XX.
       01  USE-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX.
       IX-ERR-PARA.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE RAN " IX-ST.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX.
           DISPLAY "OPEN-OUTPUT " IX-ST.
           MOVE "01XXAAAA" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "WRITE-1 " IX-ST.
           MOVE "02XXBBBB" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "WRITE-2 " IX-ST.
           MOVE "03YYCCCC" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W3 INVALID".
           DISPLAY "WRITE-3 " IX-ST.
           MOVE "04ZZDDDD" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W4 INVALID".
           DISPLAY "WRITE-4 " IX-ST.
           CLOSE IX.
           DISPLAY "CLOSE-1 " IX-ST.
           OPEN I-O IX.
           DISPLAY "OPEN-I-O " IX-ST.
           MOVE "03" TO IX-KEY.
           READ IX INVALID KEY DISPLAY "R3 INVALID".
           DISPLAY "READ-KEY " IX-ST " " IX-REC.
           MOVE "CCC3" TO IX-VAL.
           REWRITE IX-REC INVALID KEY DISPLAY "RW INVALID".
           DISPLAY "REWRITE " IX-ST.
           MOVE "04" TO IX-KEY.
           DELETE IX INVALID KEY DISPLAY "DEL INVALID".
           DISPLAY "DELETE " IX-ST.
           MOVE "XX" TO IX-ALT.
           START IX KEY IS EQUAL TO IX-ALT
               INVALID KEY DISPLAY "START INVALID".
           DISPLAY "START " IX-ST.
           READ IX NEXT RECORD AT END DISPLAY "N1 AT END".
           DISPLAY "NEXT-1 " IX-ST " " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "N2 AT END".
           DISPLAY "NEXT-2 " IX-ST " " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "N3 AT END".
           DISPLAY "NEXT-3 " IX-ST " " IX-REC.
           DISPLAY "USE-COUNT " USE-COUNT.
           READ IX NEXT RECORD.
           DISPLAY "NEXT-4 " IX-ST.
           DISPLAY "USE-COUNT " USE-COUNT.
           CLOSE IX.
           DISPLAY "CLOSE-2 " IX-ST.
           STOP RUN.
