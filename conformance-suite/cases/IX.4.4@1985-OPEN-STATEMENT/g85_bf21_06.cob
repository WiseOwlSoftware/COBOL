      *>--- conformance-case
      *> id: g85_bf21_06
      *> rules: [1985-GR-IX.4.4.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR11 - OPEN EXTEND positions an indexed file
      *> immediately after its last logical record, the CURRENTLY
      *> EXISTING record with the highest prime key value. Keys 10, 20
      *> and 30 are written and 30 is deleted, so the last logical
      *> record is 20. In extend mode a WRITE whose key is not greater
      *> than that record's key is an invalid key (§1985:IX.4.9.4 GR14
      *> and GR17a, status 21): key 15 is refused, key 25 (below the
      *> deleted 30) is accepted. Positioning after the highest key ever
      *> written refuses 25; positioning at the start accepts 15.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2106.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-F ASSIGN TO "BF2106.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-F.
       01  IX-REC.
           05  IX-KEY           PIC XX.
           05  IX-DATA          PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-FS                PIC XX.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-F.
           MOVE "10" TO IX-KEY.
           MOVE "TEN" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W10 INVALID".
           MOVE "20" TO IX-KEY.
           MOVE "TWENTY" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W20 INVALID".
           MOVE "30" TO IX-KEY.
           MOVE "THIRTY" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W30 INVALID".
           CLOSE IDX-F.
           OPEN I-O IDX-F.
           READ IDX-F AT END DISPLAY "R1 END".
           READ IDX-F AT END DISPLAY "R2 END".
           READ IDX-F AT END DISPLAY "R3 END".
           DISPLAY "DELETE " IX-KEY.
           DELETE IDX-F.
           DISPLAY "DELETE STATUS " WS-FS.
           CLOSE IDX-F.
           OPEN EXTEND IDX-F.
           DISPLAY "EXTEND STATUS " WS-FS.
           MOVE "15" TO IX-KEY.
           MOVE "FIFTN" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W15 INVALID " WS-FS
               NOT INVALID KEY DISPLAY "W15 WRITTEN " WS-FS
           END-WRITE.
           MOVE "25" TO IX-KEY.
           MOVE "TWT5" TO IX-DATA.
           WRITE IX-REC
               INVALID KEY DISPLAY "W25 INVALID " WS-FS
               NOT INVALID KEY DISPLAY "W25 WRITTEN " WS-FS
           END-WRITE.
           CLOSE IDX-F.
           OPEN INPUT IDX-F.
           PERFORM READ-F UNTIL WS-EOF = "Y".
           CLOSE IDX-F.
           STOP RUN.
       READ-F.
           READ IDX-F
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "END"
               NOT AT END
                   DISPLAY "REC " IX-KEY " [" IX-DATA "]"
           END-READ.
