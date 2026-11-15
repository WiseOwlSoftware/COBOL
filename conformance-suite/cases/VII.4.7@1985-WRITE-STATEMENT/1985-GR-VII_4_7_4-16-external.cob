      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-16-external
      *> rules: [1985-GR-VII.4.7.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5570 -- §1985:VII.4.7.4 GR16: a WRITE without the
      *> ADVANCING phrase acts as AFTER ADVANCING 1 LINE, also for an
      *> EXTERNAL file whose ADVANCING WRITE is in ANOTHER program of
      *> the run unit (one connector per run unit, so one print file).
      *>   python scripts/spec/cite.py --check "§1985:VII.4.7.4 GR16"
      *>   "If the ADVANCING phrase is not used, automatic advancing
      *>   will be provided by the implementor to act as if the user
      *>   had specified AFTER ADVANCING 1 LINE."        -> OK
      *> EXTA writes P1 and P2 plain, EXTB writes A2 AFTER ADVANCING 2
      *> LINES, EXTA writes P3 plain. Each record is preceded by its
      *> advance (1, 1, 2, 1 lines) and CLOSE ends the last line, so
      *> the file is LF P1 LF P2 LF LF A2 LF P3 LF: lines "", P1, P2,
      *> "", A2, P3. A file whose P1 and P2 share a line fails GR16.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5570A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-CR SYM-LF
               ARE 14 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P-FILE ASSIGN TO "pb5570a.prt".
           SELECT RD-FILE ASSIGN TO "pb5570a.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  P-FILE IS EXTERNAL.
       01  P-REC PIC X(4).
       FD  RD-FILE.
       01  RD-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(4) VALUE SPACES.
       01  WS-PTR PIC 99 VALUE 1.
       01  WS-LINE-NO PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       P-PRINT.
           OPEN OUTPUT P-FILE.
           MOVE "P1" TO P-REC.
           WRITE P-REC.
           MOVE "P2" TO P-REC.
           WRITE P-REC.
           CALL "PB5570B".
           MOVE "P3" TO P-REC.
           WRITE P-REC.
           CLOSE P-FILE.
       P-SHOW.
           OPEN INPUT RD-FILE.
           PERFORM P-SHOW-CHAR UNTIL WS-EOF = "Y".
           CLOSE RD-FILE.
           IF WS-PTR > 1
               PERFORM P-SHOW-LINE.
           STOP RUN.
       P-SHOW-CHAR.
           READ RD-FILE
               AT END MOVE "Y" TO WS-EOF
           END-READ.
           IF WS-EOF = "N"
               IF RD-REC = SYM-LF
                   PERFORM P-SHOW-LINE
               ELSE
                   IF RD-REC NOT = SYM-CR
                       STRING RD-REC DELIMITED BY SIZE INTO WS-LINE
                           WITH POINTER WS-PTR.
       P-SHOW-LINE.
           DISPLAY "PRINT LINE " WS-LINE-NO " [" WS-LINE "]".
           ADD 1 TO WS-LINE-NO.
           MOVE SPACES TO WS-LINE.
           MOVE 1 TO WS-PTR.
       END PROGRAM PB5570A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5570B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P-FILE ASSIGN TO "pb5570a.prt".
       DATA DIVISION.
       FILE SECTION.
       FD  P-FILE IS EXTERNAL.
       01  P-REC PIC X(4).
       PROCEDURE DIVISION.
       B-PARA.
           MOVE "A2" TO P-REC.
           WRITE P-REC AFTER ADVANCING 2 LINES.
           EXIT PROGRAM.
       END PROGRAM PB5570B.
