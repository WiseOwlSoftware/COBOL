      *>--- conformance-case
      *> id: g2_write_sequential_lock_phrase_automatic_neg
      *> rules: [2002-SR-14.8.47.2-21]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR21 - "If automatic locking has been specified
      *> for the write file, neither the WITH LOCK phrase nor the WITH NO
      *> LOCK phrase shall be specified." The MANUAL control
      *> g2_write_sequential_lock_phrase_manual with LOCK MODE IS
      *> AUTOMATIC: the Format 1 WRITE ... WITH LOCK, written in the 2002
      *> order, is refused at 2002 by SR21 (COBOL5993), not by the parser.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR21T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "g2wr21t.prt" ORGANIZATION SEQUENTIAL
               LOCK MODE IS AUTOMATIC FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 5 LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LPF
           MOVE "AAAA" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE ADD 1 TO N
               NOT AT END-OF-PAGE ADD 2 TO N
               WITH LOCK
           END-WRITE
           DISPLAY "W " FS " N=" N
           CLOSE LPF
           STOP RUN.
