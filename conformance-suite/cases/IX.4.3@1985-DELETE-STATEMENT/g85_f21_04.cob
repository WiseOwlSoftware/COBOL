      *>--- conformance-case
      *> id: g85_f21_04
      *> rules: [1985-GR-IX.4.3.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.4 GR9 - "The END-DELETE phrase delimits the scope
      *> of the DELETE statement": an indexed file in random access
      *> mode. In each IF the DISPLAY after END-DELETE is outside the
      *> DELETE. Leg 1 deletes the record with prime key K2
      *> successfully; without the delimiting its DISPLAY would belong
      *> to INVALID KEY and not run. Leg 2 deletes K2 again, which no
      *> longer exists, so the invalid key condition exists (GR3,
      *> status 23); without the delimiting its DISPLAY would belong to
      *> NOT INVALID KEY and not run. R2 and R3 show that only K2 was
      *> removed (GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2104.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "G85F2104.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IXF-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF
           LABEL RECORDS ARE STANDARD.
       01  IXF-REC.
           05  IXF-KEY  PIC X(2).
           05  IXF-DATA PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXF.
           MOVE "K1AAA" TO IXF-REC.
           WRITE IXF-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "K2BBB" TO IXF-REC.
           WRITE IXF-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE "K3CCC" TO IXF-REC.
           WRITE IXF-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE IXF.
           OPEN I-O IXF.
           MOVE "K2" TO IXF-KEY.
           IF WS-FLAG = 1
               DELETE IXF RECORD
                   INVALID KEY DISPLAY "D1 INVALID " WS-STAT
               END-DELETE
               DISPLAY "AFTER END-DELETE 1 " WS-STAT
           END-IF.
           MOVE "K2" TO IXF-KEY.
           IF WS-FLAG = 1
               DELETE IXF RECORD
                   INVALID KEY DISPLAY "D2 INVALID " WS-STAT
                   NOT INVALID KEY DISPLAY "D2 DELETED"
               END-DELETE
               DISPLAY "AFTER END-DELETE 2 " WS-STAT
           END-IF.
           MOVE "K2" TO IXF-KEY.
           READ IXF
               INVALID KEY DISPLAY "R2 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R2 FOUND"
           END-READ.
           MOVE "K3" TO IXF-KEY.
           READ IXF
               INVALID KEY DISPLAY "R3 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R3 " IXF-DATA
           END-READ.
           CLOSE IXF.
           STOP RUN.
