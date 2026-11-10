      *>--- conformance-case
      *> id: 1985-FMT-VII_3_8_2
      *> rules: [1985-FMT-VII.3.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.2 (THE RECORD CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECCL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "RECF1".
           SELECT F2 ASSIGN TO "RECF2".
           SELECT F3 ASSIGN TO "RECF3".
           SELECT F4 ASSIGN TO "RECF4".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 10 CHARACTERS.
       01  F1-REC PIC X(10).
       FD  F2
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON WS-LEN.
       01  F2-REC PIC X(10).
       FD  F3
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 4 TO 10 CHARACTERS.
       01  F3-SHORT PIC X(4).
       01  F3-LONG  PIC X(10).
       FD  F4
           LABEL RECORDS ARE STANDARD
           RECORD VARYING.
       01  F4-SHORT PIC X(3).
       01  F4-LONG  PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-LEN PIC 99.
       01  WS-BUF PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "FIXEDTEN10" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF1".
           DISPLAY F1-REC.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "HELLO" TO F2-REC.
           MOVE 5 TO WS-LEN.
           WRITE F2-REC.
           MOVE "ABCDEFGHIJ" TO F2-REC.
           MOVE 10 TO WS-LEN.
           WRITE F2-REC.
           CLOSE F2.
           MOVE 0 TO WS-LEN.
           OPEN INPUT F2.
           READ F2 AT END DISPLAY "EOF2".
           DISPLAY WS-LEN.
           DISPLAY F2-REC(1:5).
           READ F2 AT END DISPLAY "EOF2".
           DISPLAY WS-LEN.
           DISPLAY F2-REC.
           CLOSE F2.
           OPEN OUTPUT F3.
           MOVE "WXYZ" TO F3-SHORT.
           WRITE F3-SHORT.
           MOVE "0123456789" TO F3-LONG.
           WRITE F3-LONG.
           CLOSE F3.
           OPEN INPUT F3.
           READ F3 AT END DISPLAY "EOF3".
           DISPLAY F3-SHORT.
           READ F3 AT END DISPLAY "EOF3".
           DISPLAY F3-LONG.
           CLOSE F3.
           OPEN OUTPUT F4.
           MOVE "ABC" TO F4-SHORT.
           WRITE F4-SHORT.
           MOVE "ABCDEFGH" TO F4-LONG.
           WRITE F4-LONG.
           CLOSE F4.
           OPEN INPUT F4.
           MOVE ALL "?" TO WS-BUF.
           READ F4 INTO WS-BUF AT END DISPLAY "EOF4".
           DISPLAY "[" WS-BUF "]".
           MOVE ALL "?" TO WS-BUF.
           READ F4 INTO WS-BUF AT END DISPLAY "EOF4".
           DISPLAY "[" WS-BUF "]".
           CLOSE F4.
           STOP RUN.
