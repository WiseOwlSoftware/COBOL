      *>--- conformance-case
      *> id: g85_f18_01
      *> rules: [1985-GR-VII.3.3.3-3, 1985-GR-VII.3.3.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.3.3 GR3 - BLOCK CONTAINS ... CHARACTERS sizes
      *> the physical record in character positions "regardless of
      *> the types of characters used to represent the items"; FC
      *> blocks a record of DISPLAY, COMP and PACKED-DECIMAL items
      *> in CHARACTERS.
      *> §1985:VII.3.3.3 GR4 - integer-2 alone is the EXACT physical
      *> record size (FC: 60 CHARACTERS); integer-1 TO integer-2 are
      *> the minimum and maximum (FV: 10 TO 60 CHARACTERS; FR: 2 TO
      *> 5 RECORDS).
      *> The physical record is not visible to the program: the
      *> clause governs only how logical records are grouped on the
      *> medium, so each file must still deliver, READ by READ, the
      *> logical records written, in order, then AT END. Each leg
      *> fails only if its form is refused at 85 or a logical
      *> record is lost or reordered; the physical block, and so
      *> the CHARACTERS unit of §1985:VII.3.3.3 GR3, is not
      *> observable, and this case witnesses only that the form is
      *> accepted and the file works.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1801.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FC ASSIGN TO "g85f1801c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FV ASSIGN TO "g85f1801v.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FR ASSIGN TO "g85f1801r.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  FC
           BLOCK CONTAINS 60 CHARACTERS.
       01  RC.
           05 RC-X PIC X(4).
           05 RC-B PIC 9(4) COMP.
           05 RC-P PIC 9(5) PACKED-DECIMAL.
       FD  FV
           BLOCK CONTAINS 10 TO 60 CHARACTERS.
       01  RV PIC X(10).
       FD  FR
           BLOCK CONTAINS 2 TO 5 RECORDS.
       01  RR PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  K  PIC 9.
       01  DB PIC 9(4).
       01  DP PIC 9(5).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FC.
           MOVE "AAAA" TO RC-X. MOVE 1234 TO RC-B. MOVE 56789 TO RC-P.
           WRITE RC.
           MOVE "BBBB" TO RC-X. MOVE 9 TO RC-B. MOVE 10 TO RC-P.
           WRITE RC.
           MOVE "CCCC" TO RC-X. MOVE 4321 TO RC-B. MOVE 98765 TO RC-P.
           WRITE RC.
           DISPLAY "FC W " FS.
           CLOSE FC.
           OPEN INPUT FC.
           MOVE 0 TO K.
       P1.
           READ FC AT END GO TO P2.
           ADD 1 TO K.
           MOVE RC-B TO DB. MOVE RC-P TO DP.
           DISPLAY "C" K " " RC-X " " DB " " DP.
           GO TO P1.
       P2.
           DISPLAY "FC END " K.
           CLOSE FC.
           OPEN OUTPUT FV.
           MOVE "V1V1V1V1V1" TO RV. WRITE RV.
           MOVE "V2V2V2V2V2" TO RV. WRITE RV.
           MOVE "V3V3V3V3V3" TO RV. WRITE RV.
           MOVE "V4V4V4V4V4" TO RV. WRITE RV.
           DISPLAY "FV W " FS.
           CLOSE FV.
           OPEN INPUT FV.
           MOVE 0 TO K.
       P3.
           READ FV AT END GO TO P4.
           ADD 1 TO K.
           DISPLAY "V" K " " RV.
           GO TO P3.
       P4.
           DISPLAY "FV END " K.
           CLOSE FV.
           OPEN OUTPUT FR.
           MOVE "R1R1R1" TO RR. WRITE RR.
           MOVE "R2R2R2" TO RR. WRITE RR.
           MOVE "R3R3R3" TO RR. WRITE RR.
           DISPLAY "FR W " FS.
           CLOSE FR.
           OPEN INPUT FR.
           MOVE 0 TO K.
       P5.
           READ FR AT END GO TO P6.
           ADD 1 TO K.
           DISPLAY "R" K " " RR.
           GO TO P5.
       P6.
           DISPLAY "FR END " K.
           CLOSE FR.
           STOP RUN.
