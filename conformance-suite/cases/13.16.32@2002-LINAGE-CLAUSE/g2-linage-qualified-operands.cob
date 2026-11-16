      *>--- conformance-case
      *> id: g2-linage-qualified-operands
      *> rules: [2002-SR-13.16.32.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.32.2 SR1 - "Data-name-1, data-name-2, data-name-3,
      *> data-name-4 may be qualified and shall reference elementary
      *> unsigned numeric integer data items." All four LINAGE operands
      *> are written qualified (PS/FS/TP/BT OF L1); L2 holds same-named
      *> items with other values, so only qualification picks L1.
      *> L1: page size 3, footing 2, margins 0. L2: 50, 40, 9, 9.
      *> §2002:13.16.32.3 GR7d sets LINAGE-COUNTER to one at OPEN
      *> OUTPUT; GR7c3 adds one per WRITE without ADVANCING; GR7 says
      *> it is the line number within the current page body, so with
      *> the page size 3 of L1 (GR2) it never exceeds 3, and GR7c4 and
      *> §2002:14.8.47.3 GR22a reset it to one when it has reached the
      *> page size: after OPEN it is 1, the first WRITE makes it 2, the
      *> second 3, so over eight WRITEs its largest value is 3 (MAXLC).
      *> With L2's page size 50 it would climb to 9.
      *> §2002:14.8.47.3 GR22b: with the footing start 2 of L1 (GR3)
      *> the second WRITE prints or spaces in the footing area, so an
      *> end-of-page condition occurs and imperative-statement-1 runs
      *> (GR23b): EOP-SEEN=Y. With L2's footing 40, no end-of-page
      *> condition occurs in eight WRITEs.
      *> Only these two facts are pinned; the exact WRITE on which each
      *> end-of-page condition falls is not part of this case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LIN02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFL ASSIGN TO "g2lin01.txt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PFL LINAGE IS PS OF L1 LINES WITH FOOTING AT FS OF L1
               LINES AT TOP TP OF L1 LINES AT BOTTOM BT OF L1.
       01  PR PIC X(10).
       WORKING-STORAGE SECTION.
       01  L1.
           05 PS PIC 99 VALUE 3.
           05 FS PIC 99 VALUE 2.
           05 TP PIC 99 VALUE 0.
           05 BT PIC 99 VALUE 0.
       01  L2.
           05 PS PIC 99 VALUE 50.
           05 FS PIC 99 VALUE 40.
           05 TP PIC 99 VALUE 9.
           05 BT PIC 99 VALUE 9.
       01  MAXLC PIC 99 VALUE 0.
       01  EOP-SEEN PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PFL
           PERFORM 8 TIMES
              MOVE "LINE" TO PR
              WRITE PR
                 AT END-OF-PAGE MOVE "Y" TO EOP-SEEN
              END-WRITE
              IF LINAGE-COUNTER > MAXLC
                 MOVE LINAGE-COUNTER TO MAXLC
              END-IF
           END-PERFORM
           CLOSE PFL
           DISPLAY "MAXLC=" MAXLC
           DISPLAY "EOP-SEEN=" EOP-SEEN
           STOP RUN.
