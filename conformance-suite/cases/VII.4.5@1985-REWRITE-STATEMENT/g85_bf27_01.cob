      *>--- conformance-case
      *> id: g85_bf27_01
      *> rules: [1985-GR-VII.4.5.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR8 - "The execution of the REWRITE statement
      *> releases a logical record to the operating system." A
      *> sequential mass storage file is updated in place: record 2 is
      *> read and REWRITTEN, the record area is then overwritten, the
      *> file is read on, closed and read back.
      *> DERIVATION.
      *>  RW-STATUS=00: the REWRITE is successful (GR7 updates the
      *>   I-O status; VII.1.3.5 successful completion is 00).
      *>  NEXT=CCCC: §1985:VII.4.5.4 GR6, the file position indicator
      *>   is not affected, so the next READ takes record 3.
      *>  REC1..REC3: GR8 released the replaced record to the operating
      *>   system at the REWRITE, so the file holds NEWB as record 2,
      *>   not BBBB (no update) and not ZZZZ (the record area, moved
      *>   after the REWRITE, is not what was released). REWRITE
      *>   replaces (VII.4.5.1), so the file still has 3 records and
      *>   the fourth READ is at end, I-O status 10.
      *> Each leg fails if the record is never released (BBBB), is
      *> taken from the record area later (ZZZZ), or is added (no EOF).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2701.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "g85bf2701.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE "??".
       PROCEDURE DIVISION.
       P-CREATE.
           OPEN OUTPUT SQ-FILE.
           MOVE "AAAA" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "BBBB" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "CCCC" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
       P-REWRITE.
           OPEN I-O SQ-FILE.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           MOVE "NEWB" TO SQ-REC.
           REWRITE SQ-REC.
           DISPLAY "RW-STATUS=" SQ-STATUS.
           MOVE "ZZZZ" TO SQ-REC.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "NEXT=" SQ-REC.
           CLOSE SQ-FILE.
       P-CHECK.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "REC1=" SQ-REC.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "REC2=" SQ-REC.
           READ SQ-FILE AT END DISPLAY "UNEXPECTED AT END".
           DISPLAY "REC3=" SQ-REC.
           READ SQ-FILE AT END DISPLAY "EOF=" SQ-STATUS.
           CLOSE SQ-FILE.
           STOP RUN.
