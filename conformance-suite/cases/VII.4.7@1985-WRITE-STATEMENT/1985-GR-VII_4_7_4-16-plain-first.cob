      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-16-plain-first
      *> rules: [1985-GR-VII.4.7.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5201 -- §1985:VII.4.7.4 GR16: a WRITE without the
      *> ADVANCING phrase acts as AFTER ADVANCING 1 LINE, including the
      *> plain WRITEs that come BEFORE the first WRITE of the same file
      *> that names the phrase.
      *>   python scripts/spec/cite.py --check "§1985:VII.4.7.4 GR16"
      *>   "If the ADVANCING phrase is not used, automatic advancing
      *>   will be provided by the implementor to act as if the user
      *>   had specified AFTER ADVANCING 1 LINE."        -> OK
      *> Whether the file supports vertical positioning is the
      *> implementor's determination (docs/CONFORMANCE.md section 4):
      *> a file the program writes with ADVANCING on
      *> any WRITE is a print file from its OPEN.
      *>   PR-FILE: plain AAA, plain BBB, then AFTER ADVANCING 1 LINE
      *>   CCC. Each record is preceded by one line advance (GR16,
      *>   GR14 AFTER), so the file is LF AAA LF BBB LF CCC and CLOSE
      *>   ends the last line: lines "", AAA, BBB, CCC. A file whose
      *>   first two records share a line (AAABBB) fails the first
      *>   rule; one with no leading advance fails GR16 for AAA.
      *>   TX-FILE: plain WRITEs only, no WRITE of it names ADVANCING,
      *>   so nothing positions it and its two records are adjacent:
      *>   read back through a six-character record, AAABBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5201A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-CR SYM-LF
               ARE 14 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PR-FILE ASSIGN TO "pb5201a-p.prt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RD-FILE ASSIGN TO "pb5201a-p.prt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT TX-FILE ASSIGN TO "pb5201a-t.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT TR-FILE ASSIGN TO "pb5201a-t.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PR-FILE.
       01  PR-REC PIC X(3).
       FD  RD-FILE.
       01  RD-REC PIC X.
       FD  TX-FILE.
       01  TX-REC PIC X(3).
       FD  TR-FILE.
       01  TR-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(6) VALUE SPACES.
       01  WS-PTR PIC 99 VALUE 1.
       01  WS-LINE-NO PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       P-PRINT.
           OPEN OUTPUT PR-FILE.
           MOVE "AAA" TO PR-REC.
           WRITE PR-REC.
           MOVE "BBB" TO PR-REC.
           WRITE PR-REC.
           MOVE "CCC" TO PR-REC.
           WRITE PR-REC AFTER ADVANCING 1 LINE.
           CLOSE PR-FILE.
       P-TEXT.
           OPEN OUTPUT TX-FILE.
           MOVE "AAA" TO TX-REC.
           WRITE TX-REC.
           MOVE "BBB" TO TX-REC.
           WRITE TX-REC.
           CLOSE TX-FILE.
           OPEN INPUT TR-FILE.
           READ TR-FILE AT END DISPLAY "TEXT EMPTY".
           DISPLAY "TEXT [" TR-REC "]".
           CLOSE TR-FILE.
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
