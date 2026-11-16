      *>--- conformance-case
      *> id: g85_bf31_02
      *> rules: [1985-GR-IX.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.4 GR4 - after a successful DELETE the record "has
      *> been logically removed from the file and can no longer be
      *> accessed" (indexed file, dynamic access). Keys AAA,BBB,CCC
      *> written; DELETE with XKEY=BBB (GR3: the record identified by
      *> the prime record key) -> NOT INVALID KEY, status 00. Then: a
      *> random READ of BBB takes INVALID KEY with status 23
      *> (§1985:IX.1.3.4: "An attempt is made to randomly access a
      *> record that does not exist in the file"); a second DELETE of
      *> BBB takes INVALID KEY (GR3); CCC still reads; a sequential
      *> pass after CLOSE/OPEN INPUT returns AAA, CCC, then AT END.
      *> Each leg fails if the record survives anywhere, the wrong one
      *> goes, or the delete does not persist past CLOSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3102.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XFILE ASSIGN TO "g85bf3102.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XKEY
               FILE STATUS IS XST.
       DATA DIVISION.
       FILE SECTION.
       FD  XFILE.
       01  XREC.
           05  XKEY  PIC X(3).
           05  XDATA PIC X(5).
       WORKING-STORAGE SECTION.
       77  XST  PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XFILE.
           MOVE "AAA" TO XKEY.
           MOVE "DATA1" TO XDATA.
           WRITE XREC INVALID KEY DISPLAY "WRITE1 FAIL".
           MOVE "BBB" TO XKEY.
           MOVE "DATA2" TO XDATA.
           WRITE XREC INVALID KEY DISPLAY "WRITE2 FAIL".
           MOVE "CCC" TO XKEY.
           MOVE "DATA3" TO XDATA.
           WRITE XREC INVALID KEY DISPLAY "WRITE3 FAIL".
           CLOSE XFILE.
           OPEN I-O XFILE.
           MOVE "BBB" TO XKEY.
           DELETE XFILE RECORD
               INVALID KEY DISPLAY "DELB INVALID"
               NOT INVALID KEY DISPLAY "DELB OK"
           END-DELETE.
           DISPLAY "DELB ST=" XST.
           MOVE "BBB" TO XKEY.
           READ XFILE
               INVALID KEY DISPLAY "READB INVALID"
               NOT INVALID KEY DISPLAY "READB FOUND " XDATA
           END-READ.
           DISPLAY "READB ST=" XST.
           MOVE "BBB" TO XKEY.
           DELETE XFILE RECORD
               INVALID KEY DISPLAY "DELB AGAIN INVALID"
               NOT INVALID KEY DISPLAY "DELB AGAIN OK"
           END-DELETE.
           MOVE "CCC" TO XKEY.
           READ XFILE
               INVALID KEY DISPLAY "READC INVALID"
               NOT INVALID KEY DISPLAY "READC FOUND " XDATA
           END-READ.
           CLOSE XFILE.
           OPEN INPUT XFILE.
           PERFORM 3 TIMES
               READ XFILE NEXT RECORD
                   AT END DISPLAY "SEQ END"
                   NOT AT END DISPLAY "SEQ " XKEY " " XDATA
               END-READ
           END-PERFORM.
           CLOSE XFILE.
           STOP RUN.
