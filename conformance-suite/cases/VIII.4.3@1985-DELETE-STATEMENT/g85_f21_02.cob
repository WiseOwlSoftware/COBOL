      *>--- conformance-case
      *> id: g85_f21_02
      *> rules: [1985-GR-VIII.4.3.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.4 GR9 - "The END-DELETE phrase delimits the
      *> scope of the DELETE statement": a relative file in random
      *> access mode. In each IF the DISPLAY after END-DELETE is outside
      *> the DELETE. Leg 1 deletes record 2 successfully; without the
      *> delimiting its DISPLAY would belong to INVALID KEY and not run.
      *> Leg 2 deletes record 2 again, which no longer exists, so the
      *> invalid key condition exists (GR3, status 23); without the
      *> delimiting its DISPLAY would belong to NOT INVALID KEY and not
      *> run. R2 and R3 show that only record 2 was removed (GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2102.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "G85F2102.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF
           LABEL RECORDS ARE STANDARD.
       01  RELF-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-RK PIC 9(4).
       01  WS-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RELF.
           MOVE 1 TO WS-RK.
           MOVE "AAA" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE 2 TO WS-RK.
           MOVE "BBB" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE 3 TO WS-RK.
           MOVE "CCC" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE RELF.
           OPEN I-O RELF.
           MOVE 2 TO WS-RK.
           IF WS-FLAG = 1
               DELETE RELF RECORD
                   INVALID KEY DISPLAY "D1 INVALID " WS-STAT
               END-DELETE
               DISPLAY "AFTER END-DELETE 1 " WS-STAT
           END-IF.
           IF WS-FLAG = 1
               DELETE RELF RECORD
                   INVALID KEY DISPLAY "D2 INVALID " WS-STAT
                   NOT INVALID KEY DISPLAY "D2 DELETED"
               END-DELETE
               DISPLAY "AFTER END-DELETE 2 " WS-STAT
           END-IF.
           MOVE 2 TO WS-RK.
           READ RELF
               INVALID KEY DISPLAY "R2 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R2 FOUND"
           END-READ.
           MOVE 3 TO WS-RK.
           READ RELF
               INVALID KEY DISPLAY "R3 INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "R3 " RELF-REC
           END-READ.
           CLOSE RELF.
           STOP RUN.
