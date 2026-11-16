      *>--- conformance-case
      *> id: g85_bf21_04
      *> rules: [1985-GR-VIII.4.4.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR11 - OPEN EXTEND positions a relative file
      *> immediately after its last logical record, the CURRENTLY
      *> EXISTING record with the highest relative record number.
      *> Records 1, 2 and 3 are written, record 3 is deleted, so the
      *> highest existing record is 2: the extend WRITEs must receive
      *> relative record numbers 3 and 4 (§1985:VIII.4.9.4 GR12 moves
      *> each number into the RELATIVE KEY item). An implementation that
      *> positions after the highest number ever written gives 4 and 5;
      *> one that positions at the start fails the readback.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2104.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-F ASSIGN TO "BF2104.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F.
       01  R-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-RK                PIC 9(4) VALUE 0.
       01  WS-FS                PIC XX.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-F.
           MOVE "ONE" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "TWO" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE "THREE" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE REL-F.
           OPEN I-O REL-F.
           READ REL-F AT END DISPLAY "R1 END".
           READ REL-F AT END DISPLAY "R2 END".
           READ REL-F AT END DISPLAY "R3 END".
           DISPLAY "DELETE " WS-RK " [" R-REC "]".
           DELETE REL-F.
           DISPLAY "DELETE STATUS " WS-FS.
           CLOSE REL-F.
           OPEN EXTEND REL-F.
           DISPLAY "EXTEND STATUS " WS-FS.
           MOVE "NEW-A" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WA INVALID".
           DISPLAY "WRITE NEW-A AT " WS-RK.
           MOVE "NEW-B" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WB INVALID".
           DISPLAY "WRITE NEW-B AT " WS-RK.
           CLOSE REL-F.
           OPEN INPUT REL-F.
           PERFORM READ-F UNTIL WS-EOF = "Y".
           CLOSE REL-F.
           STOP RUN.
       READ-F.
           READ REL-F
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "END"
               NOT AT END
                   DISPLAY "REC " WS-RK " [" R-REC "]"
           END-READ.
