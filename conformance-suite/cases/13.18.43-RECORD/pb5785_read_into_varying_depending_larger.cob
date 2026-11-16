      *>--- conformance-case
      *> id: pb5785_read_into_varying_depending_larger
      *> rules: [GR-13.18.43.4-16, GR-14.9.30.4-14, GR-9.1.13.2-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> READ ... INTO of a RECORD IS VARYING ... DEPENDING ON file whose record descriptions (6 and 10
      *> positions) are all shorter than the clause's maximum (20) and than the record read (15).
      *> §1985:VII.3.8.4 GR11: the DEPENDING ON item "will indicate the number of character positions in the
      *>   record just read" (§2023:13.18.43.4 GR15).
      *> §1985:VII.3.8.4 GR12: "the number of character positions in the current record that participate as
      *>   the sending data items in the implicit MOVE statement is determined by the following conditions" -
      *>   the DEPENDING ON item's content (§2023:13.18.43.4 GR16 a), so all 15 positions are sent.
      *> A 15-position record lies inside the clause's 2 TO 20, so the status is 00.
      *> EXPECTED OUTPUT, derived: FS=00 DR=15 W=[ABCDEFGHIJKLMNO     ] (fifteen positions sent, the rest of W
      *>   space-filled, the implied move being an alphanumeric group move) R10=[ABCDEFGHIJ] (the record
      *>   area's ten-position description).
      *> Each leg can fail: the compiler once moved only ten positions under a DEPENDING ON value of 15.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5785B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "PB5785B.DAT".
           SELECT FR ASSIGN TO "PB5785B.DAT" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FW RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON DW.
       01 RW PIC X(20).
       FD FR RECORD IS VARYING IN SIZE FROM 2 TO 20 CHARACTERS
             DEPENDING ON DR.
       01 R6 PIC X(6).
       01 R10 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 DW PIC 99.
       01 DR PIC 99.
       01 W PIC X(20).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FW.
           MOVE "ABCDEFGHIJKLMNOPQRST" TO RW.
           MOVE 15 TO DW.
           WRITE RW.
           CLOSE FW.
           OPEN INPUT FR.
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "FS=" FS " DR=" DR " W=[" W "] R10=[" R10 "]".
           CLOSE FR.
           STOP RUN.
