      *>--- conformance-case
      *> id: pb5903_line_delimited_length_conflict_85
      *> rules: [1985-GR-VII.4.4.4-13, 1985-GR-VII.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5903 - a sequential file whose records the line
      *> delimiter frames (RECORD DELIMITER IS LINE-SEQUENTIAL, Annex A.1
      *> item 150) keeps the 1985 record rules. The rules, each checked
      *> with cite.py:
      *> - §1985:VII.2.8.4 GR1: the clause names "the method of
      *>   determining the length of a variable length record on the
      *>   external medium. Any method used will not be reflected in the
      *>   record area or the record size used within the program." So
      *>   a WRITE writes the record at its size, trailing spaces kept.
      *> - §1985:VII.4.4.4 GR13: a record read that is shorter than the
      *>   minimum size leaves the rest of the record area undefined, a
      *>   longer one "is truncated on the right to the maximum size",
      *>   and "In either of these cases, the READ statement is
      *>   successful and an I-O status is set indicating a record
      *>   length conflict has occurred": '04' (§1985:VII.1.3.5).
      *>   The next READ reads the next record: a truncated line leaves
      *>   no remainder (the remainder is a 2023 line sequential rule,
      *>   §2023:14.9.30.4 GR15, which also pads a short line with no
      *>   conflict; kb/Work PB5903).
      *> - §1985:VII.3.8.4 GR11: after a successful READ, IL holds "the
      *>   number of character positions in the record just read".
      *> The writer's records are 20, 5, 9 and 8 characters long (its
      *> DEPENDING item); the reader's sizes are 8 to 10. EXPECTED:
      *>  R1: 20 characters, over the maximum: 04, ABCDEFGHIJ.
      *>  R2: the NEXT record, SHORT, 5 characters, under the minimum: 04,
      *>   IL 05, the first five positions SHORT (the rest undefined).
      *>  R3: NINE and five spaces, 9 characters (the spaces were
      *>   written): 00, IL 09.
      *>  R4: LASTLINE, 8 characters: 00, IL 08.
      *>  R5: the at end condition: 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5903LS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LW ASSIGN TO "P5903LS.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FSW.
           SELECT LR ASSIGN TO "P5903LS.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  LW
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON OL.
       01  OREC        PIC X(20).
       FD  LR
           RECORD IS VARYING IN SIZE FROM 8 TO 10 CHARACTERS
               DEPENDING ON IL.
       01  IREC        PIC X(10).
       WORKING-STORAGE SECTION.
       01  FSW         PIC XX.
       01  FSR         PIC XX.
       01  OL          PIC 99.
       01  IL          PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LW.
           MOVE 20 TO OL.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO OREC.
           WRITE OREC.
           MOVE 5 TO OL.
           MOVE "SHORT" TO OREC.
           WRITE OREC.
           MOVE 9 TO OL.
           MOVE "NINE" TO OREC.
           WRITE OREC.
           MOVE 8 TO OL.
           MOVE "LASTLINE" TO OREC.
           WRITE OREC.
           CLOSE LW.
           DISPLAY "W " FSW.
           OPEN INPUT LR.
           READ LR AT END DISPLAY "EOF".
           DISPLAY "R1 " FSR " [" IREC "]".
           READ LR AT END DISPLAY "EOF".
           DISPLAY "R2 " FSR " " IL " [" IREC (1:5) "]".
           READ LR AT END DISPLAY "EOF".
           DISPLAY "R3 " FSR " " IL " [" IREC (1:IL) "]".
           READ LR AT END DISPLAY "EOF".
           DISPLAY "R4 " FSR " " IL " [" IREC (1:IL) "]".
           READ LR AT END DISPLAY "R5 EOF " FSR.
           CLOSE LR.
           STOP RUN.
