      *>--- conformance-case
      *> id: pb5984_line_delimited_linage_one_record_per_write_85
      *> rules: [1985-GR-VII.4.7.4-12, 1985-GR-VII.4.7.4-18,
      *>    1985-GR-VII.3.7.4-9, 1985-GR-VII.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5984 - the LINAGE twin of
      *> pb5984_line_delimited_advancing_one_record_per_write_85: on a
      *> file whose records the line delimiter frames (RECORD DELIMITER
      *> IS LINE-SEQUENTIAL) the logical page has no image on the
      *> medium at COBOL-85, but LINAGE-COUNTER and the end-of-page
      *> conditions still follow it. The rules, checked with cite.py:
      *> - §1985:VII.4.7.4 GR12: the successor relationship "is
      *>   established by the order of execution of WRITE statements",
      *>   so the file reads back as the four records written, no
      *>   margin or page-fill line among them.
      *> - §1985:VII.3.7.4 GR9d: the counter is "set to one at the time
      *>   an OPEN statement with the OUTPUT phrase is executed"; GR9c.3
      *>   adds one for a WRITE with no ADVANCING phrase, GR9c.2 adds
      *>   the ADVANCING amount, GR9c.1 resets it to one for ADVANCING
      *>   PAGE and GR9c.4 when the device is repositioned to the next
      *>   logical page.
      *> - §1985:VII.4.7.4 GR18: an END-OF-PAGE imperative runs when the
      *>   counter reaches the footing; the automatic page overflow
      *>   paragraph after it runs it when a WRITE cannot be
      *>   accommodated in the page body.
      *> LINAGE 3, FOOTING 2. EXPECTED: the plain WRITE takes the
      *> counter from 1 to 2, the footing (EOP 1, LC 2); AFTER 2 would
      *> make 4 > 3, a page overflow (EOP 2, LC 1); AFTER PAGE resets
      *> it (LC 1, no phrase); BEFORE 3 overflows again (EOP 4, LC 1).
      *> Every WRITE 00; the read-back is the four records, 00, sizes
      *> 5, 5, 7, 5, then N 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5984L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5984L.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON RL
           LINAGE IS 3 LINES WITH FOOTING AT 2
               LINES AT TOP 1 LINES AT BOTTOM 1.
       01  R           PIC X(20).
       WORKING-STORAGE SECTION.
       01  FS          PIC XX.
       01  RL          PIC 99.
       01  N           PIC 9 VALUE 0.
       01  EOFF        PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R. MOVE 5 TO RL.
           WRITE R AT END-OF-PAGE DISPLAY "EOP 1".
           DISPLAY "W " FS " LC " LINAGE-COUNTER.
           MOVE "BRAVO" TO R. MOVE 5 TO RL.
           WRITE R AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "EOP 2".
           DISPLAY "W " FS " LC " LINAGE-COUNTER.
           MOVE "CHARLIE" TO R. MOVE 7 TO RL.
           WRITE R AFTER ADVANCING PAGE.
           DISPLAY "W " FS " LC " LINAGE-COUNTER.
           MOVE "DELTA" TO R. MOVE 5 TO RL.
           WRITE R BEFORE ADVANCING 3 LINES
               AT END-OF-PAGE DISPLAY "EOP 4".
           DISPLAY "W " FS " LC " LINAGE-COUNTER.
           CLOSE F.
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
           STOP RUN.
