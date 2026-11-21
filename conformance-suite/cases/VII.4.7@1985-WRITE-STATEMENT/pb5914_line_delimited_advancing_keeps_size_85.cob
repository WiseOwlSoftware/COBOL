      *>--- conformance-case
      *> id: pb5914_line_delimited_advancing_keeps_size_85
      *> rules: [1985-GR-VII.2.8.4-1, 1985-GR-VII.4.7.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5914 - a WRITE with an ADVANCING phrase (or, in a
      *> file that has one, a plain WRITE) to a file whose records the
      *> line delimiter frames (RECORD DELIMITER IS LINE-SEQUENTIAL)
      *> keeps the record at its size, exactly as the plain WRITE of
      *> kb/Work PB5903 does. The rule, checked with cite.py:
      *> - §1985:VII.2.8.4 GR1: the clause names "the method of
      *>   determining the length of a variable length record on the
      *>   external medium. Any method used will not be reflected in
      *>   the record area or the record size used within the program."
      *>   So the record written is the 10 characters of OREC, the
      *>   trailing spaces among them, whatever the print control.
      *> The writer takes two WRITEs of the 10-character record, one
      *> AFTER ADVANCING and one with no phrase (§1985:VII.4.7.4 GR16:
      *> automatic advancing), then, in a second pass, two BEFORE
      *> ADVANCING. The reader counts the lines that hold a whole
      *> record. EXPECTED:
      *>  W 00
      *>  each record 10 characters, 00, IL 10, spaces kept:
      *>   [AB        ] [EF        ] N 2
      *>   [CD        ] [GH        ] N 4
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5914A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LW ASSIGN TO "P5914A.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FSW.
           SELECT LR ASSIGN TO "P5914A.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FSR.
       DATA DIVISION.
       FILE SECTION.
       FD  LW RECORD CONTAINS 1 TO 10 CHARACTERS.
       01  OREC        PIC X(10).
       FD  LR
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON IL.
       01  IREC        PIC X(10).
       WORKING-STORAGE SECTION.
       01  FSW         PIC XX.
       01  FSR         PIC XX.
       01  IL          PIC 99.
       01  N           PIC 9 VALUE 0.
       01  EOFF        PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LW.
           MOVE "AB" TO OREC.
           WRITE OREC AFTER ADVANCING 1 LINE.
           MOVE "EF" TO OREC.
           WRITE OREC.
           CLOSE LW.
           DISPLAY "W " FSW.
           PERFORM READ-BACK.
           OPEN OUTPUT LW.
           MOVE "CD" TO OREC.
           WRITE OREC BEFORE ADVANCING 1 LINE.
           MOVE "GH" TO OREC.
           WRITE OREC BEFORE ADVANCING 1 LINE.
           CLOSE LW.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE "N" TO EOFF.
           OPEN INPUT LR.
           PERFORM UNTIL EOFF = "Y"
               READ LR AT END MOVE "Y" TO EOFF
               NOT AT END
                   IF IL = 10
                       ADD 1 TO N
                       DISPLAY FSR " " IL " [" IREC "]"
                   END-IF
               END-READ
           END-PERFORM.
           CLOSE LR.
           DISPLAY "N " N.
