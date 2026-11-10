      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_5_2
      *> rules: [1985-FMT-VIII.4.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.2 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELRD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "RELRD1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
           SELECT DYN-FILE ASSIGN TO "RELRD2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE.
       01  SEQ-REC PIC X(5).
       FD  DYN-FILE.
       01  DYN-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(5).
       01  WS-EOF PIC 9 VALUE 0.
       01  WS-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "REC A" TO SEQ-REC.
           WRITE SEQ-REC INVALID KEY DISPLAY "WRITE A FAILED".
           MOVE "REC B" TO SEQ-REC.
           WRITE SEQ-REC INVALID KEY DISPLAY "WRITE B FAILED".
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           PERFORM READ-PARA UNTIL WS-EOF = 1.
           CLOSE SEQ-FILE.
           OPEN OUTPUT DYN-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "DYN 1" TO DYN-REC.
           WRITE DYN-REC INVALID KEY DISPLAY "WRITE 1 FAILED".
           MOVE 3 TO WS-KEY.
           MOVE "DYN 3" TO DYN-REC.
           WRITE DYN-REC INVALID KEY DISPLAY "WRITE 3 FAILED".
           CLOSE DYN-FILE.
           OPEN INPUT DYN-FILE.
           MOVE 1 TO WS-KEY.
           READ DYN-FILE RECORD INTO WS-REC
               INVALID KEY DISPLAY "NO RECORD 1"
               NOT INVALID KEY DISPLAY "FOUND " WS-REC
           END-READ.
           READ DYN-FILE NEXT RECORD
               AT END DISPLAY "END 1"
               NOT AT END DISPLAY "NEXT " DYN-REC
           END-READ.
           READ DYN-FILE NEXT
               AT END DISPLAY "END 2"
               NOT AT END DISPLAY "NEXT " DYN-REC
           END-READ.
           MOVE 2 TO WS-KEY.
           READ DYN-FILE
               INVALID KEY DISPLAY "NO RECORD 2"
               NOT INVALID KEY DISPLAY "FOUND " DYN-REC
           END-READ.
           CLOSE DYN-FILE.
           STOP RUN.
       READ-PARA.
           READ SEQ-FILE RECORD INTO WS-REC
               AT END MOVE 1 TO WS-EOF
                   DISPLAY "END"
               NOT AT END DISPLAY WS-REC
           END-READ.
