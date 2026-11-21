      *>--- conformance-case
      *> id: pb5984_line_delimited_advancing_one_record_per_write_85
      *> rules: [1985-GR-VII.4.7.4-12, 1985-GR-VII.4.7.4-16,
      *>    1985-GR-VII.2.6.3-2, 1985-GR-VII.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5984 - a WRITE with an ADVANCING phrase to a file
      *> whose records the line delimiter frames (RECORD DELIMITER IS
      *> LINE-SEQUENTIAL) releases one record and nothing else at
      *> COBOL-85: the file reads back as exactly the records written.
      *> The rules, checked with cite.py:
      *> - §1985:VII.4.7.4 GR12: "The successor relationship of a
      *>   sequential file is established by the order of execution of
      *>   WRITE statements when the file is created."
      *> - §1985:VII.2.6.3 GR2: "a record is identified by a
      *>   predecessor-successor relationship established when the
      *>   record is placed into the file".
      *> - §1985:VII.4.7.4 GR16: the ADVANCING phrase allows "control of
      *>   the vertical positioning of each line on a representation of
      *>   a printed page", and no phrase acts as AFTER ADVANCING 1
      *>   LINE; it releases no record.
      *> - §1985:VII.2.8.4 GR1: the delimiter method "will not be
      *>   reflected in the record area or the record size used within
      *>   the program", so no blank line, form feed or carriage return
      *>   reaches a record. The positioning has no image on this
      *>   medium (docs/conformance/7/DOC-A.1-150.md).
      *> Three passes write three records each: AFTER 1, AFTER 2 and no
      *> phrase; BEFORE 1, BEFORE 2, BEFORE 1; AFTER 0, AFTER PAGE,
      *> BEFORE PAGE (a fourth, BEFORE 0). EXPECTED: every WRITE 00,
      *> and each pass reads back its records alone, in WRITE order,
      *> each 00 with its own size, then the count (3, 3 and 4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5984A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5984A.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON RL.
       01  R           PIC X(20).
       WORKING-STORAGE SECTION.
       01  FS          PIC XX.
       01  RL          PIC 99.
       01  N           PIC 9.
       01  EOFF        PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R. MOVE 5 TO RL.
           WRITE R AFTER ADVANCING 1 LINE.
           DISPLAY "W " FS.
           MOVE "BRAVO" TO R. MOVE 5 TO RL.
           WRITE R AFTER ADVANCING 2 LINES.
           DISPLAY "W " FS.
           MOVE "CHARLIE" TO R. MOVE 7 TO RL.
           WRITE R.
           DISPLAY "W " FS.
           CLOSE F.
           PERFORM READ-BACK.
           OPEN OUTPUT F.
           MOVE "DELTA" TO R. MOVE 5 TO RL.
           WRITE R BEFORE ADVANCING 1 LINE.
           DISPLAY "W " FS.
           MOVE "ECHO" TO R. MOVE 4 TO RL.
           WRITE R BEFORE ADVANCING 2 LINES.
           DISPLAY "W " FS.
           MOVE "FOXTROT" TO R. MOVE 7 TO RL.
           WRITE R BEFORE ADVANCING 1 LINE.
           DISPLAY "W " FS.
           CLOSE F.
           PERFORM READ-BACK.
           OPEN OUTPUT F.
           MOVE "GOLF" TO R. MOVE 4 TO RL.
           WRITE R AFTER ADVANCING 0 LINES.
           DISPLAY "W " FS.
           MOVE "HOTEL" TO R. MOVE 5 TO RL.
           WRITE R AFTER ADVANCING PAGE.
           DISPLAY "W " FS.
           MOVE "INDIA" TO R. MOVE 5 TO RL.
           WRITE R BEFORE ADVANCING PAGE.
           DISPLAY "W " FS.
           MOVE "JULIET" TO R. MOVE 6 TO RL.
           WRITE R BEFORE ADVANCING 0 LINES.
           DISPLAY "W " FS.
           CLOSE F.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE 0 TO N.
           MOVE "N" TO EOFF.
           OPEN INPUT F.
           PERFORM UNTIL EOFF = "Y"
               MOVE SPACES TO R
               READ F AT END MOVE "Y" TO EOFF
               NOT AT END
                   ADD 1 TO N
                   DISPLAY FS " " RL " [" R "]"
               END-READ
           END-PERFORM.
           CLOSE F.
           DISPLAY "N " N.
