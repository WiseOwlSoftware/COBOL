      *>--- conformance-case
      *> id: g85_bf27_02
      *> rules: [1985-GR-VIII.4.6.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR9 - "The execution of the REWRITE statement
      *> releases a logical record to the operating system." A relative
      *> file in random access mode: record 2 is read and REWRITTEN,
      *> the record area is overwritten, and record 2 is read again
      *> while the file is STILL OPEN, then once more after a CLOSE.
      *> DERIVATION.
      *>  RW-STATUS=00: a successful REWRITE (GR8 updates the I-O
      *>   status; VIII.1.3.4 successful completion is 00).
      *>  SAME-OPEN=NEWB: §1985:VIII.4.5.4 GR15, a format 2 READ makes
      *>   available the record whose relative record number equals the
      *>   relative key; GR9 released NEWB at the REWRITE, so the file
      *>   already holds it - not BBBB (no release) and not ZZZZ (the
      *>   record area moved after the REWRITE was never released).
      *>  REC1/REC3: the neighbours are untouched (AAAA, CCCC).
      *>  REOPEN=NEWB: the released record persists in the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2702.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL-FILE ASSIGN TO "g85bf2702.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RL-KEY
               FILE STATUS IS RL-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  RL-FILE.
       01  RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  RL-STATUS PIC XX VALUE "??".
       01  RL-KEY PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       P-CREATE.
           OPEN OUTPUT RL-FILE.
           MOVE 1 TO RL-KEY.
           MOVE "AAAA" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "BAD WRITE 1".
           MOVE 2 TO RL-KEY.
           MOVE "BBBB" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "BAD WRITE 2".
           MOVE 3 TO RL-KEY.
           MOVE "CCCC" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "BAD WRITE 3".
           CLOSE RL-FILE.
       P-REWRITE.
           OPEN I-O RL-FILE.
           MOVE 2 TO RL-KEY.
           READ RL-FILE INVALID KEY DISPLAY "BAD READ 2".
           MOVE "NEWB" TO RL-REC.
           REWRITE RL-REC INVALID KEY DISPLAY "BAD REWRITE".
           DISPLAY "RW-STATUS=" RL-STATUS.
           MOVE "ZZZZ" TO RL-REC.
           MOVE 2 TO RL-KEY.
           READ RL-FILE INVALID KEY DISPLAY "BAD READ 2".
           DISPLAY "SAME-OPEN=" RL-REC.
           MOVE 1 TO RL-KEY.
           READ RL-FILE INVALID KEY DISPLAY "BAD READ 1".
           DISPLAY "REC1=" RL-REC.
           MOVE 3 TO RL-KEY.
           READ RL-FILE INVALID KEY DISPLAY "BAD READ 3".
           DISPLAY "REC3=" RL-REC.
           CLOSE RL-FILE.
       P-CHECK.
           OPEN INPUT RL-FILE.
           MOVE 2 TO RL-KEY.
           READ RL-FILE INVALID KEY DISPLAY "BAD READ 2".
           DISPLAY "REOPEN=" RL-REC.
           CLOSE RL-FILE.
           STOP RUN.
