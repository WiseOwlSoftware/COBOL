      *>--- conformance-case
      *> id: g85_bf21_09
      *> rules: [1985-SR-IX.2.7.3-1, 1985-GR-IX.2.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.7.3 SR1 - data-name-1 of the RECORD KEY clause may
      *> be qualified: R-KEY is also declared in WORKING-STORAGE, so
      *> RECORD KEY IS R-KEY OF REC-A names the record's item; the
      *> working-storage R-KEY holds "9999", so a key bound to it makes
      *> every WRITE after the first a duplicate ("W" lines) and the
      *> keyed READs fail.
      *> §1985:IX.2.7.4 GR3 - the key is described only in REC-A; the
      *> same character positions in REC-B (B-ID) are implicitly the
      *> prime key for REC-B: records written through REC-B are keyed by
      *> B-ID (the "RD" and "SEQ" lines), and a REC-B write repeating an
      *> existing B-ID is a duplicate prime key (status 22).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2109.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-F ASSIGN TO "BF2109.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY OF REC-A
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-F.
       01  REC-A.
           05  R-KEY            PIC X(4).
           05  R-DATA           PIC X(6).
       01  REC-B.
           05  B-ID             PIC X(4).
           05  B-TXT            PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-FS                PIC XX.
       01  WS-AREA.
           05  R-KEY            PIC X(4) VALUE "9999".
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-F.
           MOVE "0003" TO R-KEY OF REC-A.
           MOVE "A-WROT" TO R-DATA.
           WRITE REC-A INVALID KEY DISPLAY "W3 INVALID".
           DISPLAY "W 0003 " WS-FS.
           MOVE "0001" TO B-ID.
           MOVE "B-WROT" TO B-TXT.
           WRITE REC-B INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "W 0001 " WS-FS.
           MOVE "0002" TO B-ID.
           MOVE "B-TWO" TO B-TXT.
           WRITE REC-B INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "W 0002 " WS-FS.
           MOVE "0001" TO B-ID.
           MOVE "B-DUP" TO B-TXT.
           WRITE REC-B INVALID KEY DISPLAY "W1 DUP INVALID " WS-FS.
           CLOSE IDX-F.
           OPEN INPUT IDX-F.
           MOVE "0002" TO R-KEY OF REC-A.
           READ IDX-F INVALID KEY DISPLAY "RD 0002 INVALID".
           DISPLAY "RD 0002 [" B-TXT "]".
           MOVE "0001" TO R-KEY OF REC-A.
           READ IDX-F INVALID KEY DISPLAY "RD 0001 INVALID".
           DISPLAY "RD 0001 [" R-DATA "]".
           CLOSE IDX-F.
           OPEN INPUT IDX-F.
           PERFORM READ-SEQ UNTIL WS-EOF = "Y".
           CLOSE IDX-F.
           DISPLAY "WS R-KEY " R-KEY OF WS-AREA.
           STOP RUN.
       READ-SEQ.
           READ IDX-F NEXT
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "SEQ END"
               NOT AT END
                   DISPLAY "SEQ " B-ID " [" B-TXT "]"
           END-READ.
