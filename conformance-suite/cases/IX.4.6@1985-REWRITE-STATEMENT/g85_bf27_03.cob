      *>--- conformance-case
      *> id: g85_bf27_03
      *> rules: [1985-GR-IX.4.6.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR9 - "The execution of the REWRITE statement
      *> releases a logical record to the operating system." An indexed
      *> file in random access mode: the record with key K002 is read
      *> and REWRITTEN, the record area is overwritten, and K002 is read
      *> again while the file is STILL OPEN, then once more after a
      *> CLOSE.
      *> DERIVATION.
      *>  RW-STATUS=00: a successful REWRITE (GR8 updates the I-O
      *>   status; IX.1.3.4 successful completion is 00).
      *>  SAME-OPEN=K002NEWB: §1985:IX.4.5.4 GR17, a format 2 READ
      *>   makes available the record whose prime key equals the key of
      *>   reference; GR9 released K002NEWB at the REWRITE (IX.4.6.4
      *>   GR14: the record replaced is the one with that prime key),
      *>   so the file holds it - not K002BBBB (no release) and not
      *>   K002ZZZZ (the record area moved after the REWRITE).
      *>  REC1/REC3: the neighbours are untouched.
      *>  REOPEN=K002NEWB: the released record persists in the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2703.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "g85bf2703.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  IX-KEY PIC X(4).
           05  IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01  IX-STATUS PIC XX VALUE "??".
       PROCEDURE DIVISION.
       P-CREATE.
           OPEN OUTPUT IX-FILE.
           MOVE "K001AAAA" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "BAD WRITE 1".
           MOVE "K002BBBB" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "BAD WRITE 2".
           MOVE "K003CCCC" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "BAD WRITE 3".
           CLOSE IX-FILE.
       P-REWRITE.
           OPEN I-O IX-FILE.
           MOVE "K002" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "BAD READ 2".
           MOVE "NEWB" TO IX-VAL.
           REWRITE IX-REC INVALID KEY DISPLAY "BAD REWRITE".
           DISPLAY "RW-STATUS=" IX-STATUS.
           MOVE "ZZZZ" TO IX-VAL.
           MOVE "K002" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "BAD READ 2".
           DISPLAY "SAME-OPEN=" IX-REC.
           MOVE "K001" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "BAD READ 1".
           DISPLAY "REC1=" IX-REC.
           MOVE "K003" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "BAD READ 3".
           DISPLAY "REC3=" IX-REC.
           CLOSE IX-FILE.
       P-CHECK.
           OPEN INPUT IX-FILE.
           MOVE "K002" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "BAD READ 2".
           DISPLAY "REOPEN=" IX-REC.
           CLOSE IX-FILE.
           STOP RUN.
