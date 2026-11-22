      *>--- conformance-case
      *> id: pb5829_sizes_unchanged_by_later_open_85
      *> rules: [1985-GR-VII.4.3.4-25]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5829's lead: the witness of the SECOND half of
      *> §1985:VII.4.3.4 GR25, which g85_bf25_01 cannot fail on.
      *  GR25: "The minimum and maximum record sizes for a file are
      *  established at the time the file is created and must not
      *  subsequently be changed." g85_bf25_01 witnesses the first
      *  half (its S4/S5 re-creation establishes 15/15); no leg of it
      *  opens the existing file, without creating it, through a
      *  description of other sizes, so a compiler that changes the
      *  sizes at such an OPEN prints what it expects. This one does.
      *  F-A creates the file at 10/10 (GR21: "The successful
      *  execution of an OPEN statement with the OUTPUT phrase creates
      *  the file"). OPEN EXTEND through F-B, whose records are 15
      *  characters, is no creation, so the sizes stay 10/10; which
      *  fixed file attributes an OPEN validates is the implementor's
      *  (GR8: "The implementor defines which of the fixed file
      *  attributes are validated"; DOC-A.1-129: a fixed-length
      *  sequential file encodes none, so the OPEN is 00, and nothing
      *  is written). Read back through F-A, each record conforms to
      *  the attributes of the creation, so each READ is 00, not the
      *  04 of §1985:VII.1.3.5 GRL2.1b ("the length of the record
      *  being processed does not conform to the fixed file
      *  attributes for that file"), then 10 at the end.
      *  A compiler that records the sizes in the file and takes them
      *  again from the description at a later OPEN breaks the rule:
      *  after the EXTEND it holds 15/15 and READs the 10-character
      *  records 04, so it fails S3; one that re-frames the records
      *  at the EXTEND fails the record areas.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5829SZ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "pb5829sz.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb5829sz.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F-A
           RECORD CONTAINS 10 CHARACTERS.
       01  A-REC PIC X(10).
       FD  F-B
           RECORD CONTAINS 15 CHARACTERS.
       01  B-REC PIC X(15).
       WORKING-STORAGE SECTION.
       01  A-ST PIC XX.
       01  B-ST PIC XX.
       01  N    PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F-A.
           DISPLAY "S1-CREATE=" A-ST.
           MOVE "REC-ONE" TO A-REC.
           WRITE A-REC.
           MOVE "REC-TWO" TO A-REC.
           WRITE A-REC.
           CLOSE F-A.
           OPEN EXTEND F-B.
           DISPLAY "S2-EXTEND-15=" B-ST.
           CLOSE F-B.
           OPEN INPUT F-A.
           PERFORM READ-A 3 TIMES.
           CLOSE F-A.
           STOP RUN.
       READ-A.
           ADD 1 TO N.
           MOVE ALL "*" TO A-REC.
           READ F-A AT END DISPLAY "S3-END=" A-ST.
           IF A-ST NOT = "10"
               DISPLAY "S3-READ" N "=" A-ST " [" A-REC "]".
