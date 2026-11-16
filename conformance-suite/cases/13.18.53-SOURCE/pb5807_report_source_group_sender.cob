      *>--- conformance-case
      *> id: pb5807_report_source_group_sender
      *> rules: [SR-13.18.53.3-2, GR-13.18.53.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5807 - a report SOURCE clause whose identifier-1 is a GROUP item.  Identifier-1 "specifies the sending data
      *> item of the implicit MOVE statement" (§1985:XIII.3.18.3 SR2; §2023:13.18.53.4 GR1), and a move whose sender is a group
      *> is "treated exactly as if it were an alphanumeric to alphanumeric elementary move, except that there is no conversion
      *> of data from one form of internal representation to another" (§1985:VI.6.19.4 GR5; §2023:14.9.25.4 GR4).  So the
      *> printable item never converts the group's characters by ITS category: a numeric printable item prints them as they
      *> are, a numeric-edited one applies no editing, and the alphanumeric move's alignment (left-justified, space fill on the
      *> right, truncation on the right) fits them to the item's size.
      *>   col  1  PIC 999    SOURCE WS-G  (WS-G = "4X7")   numeric printable item   -> 4X7 (a numeric conversion printed 047)
      *>   col  5  PIC ZZ9    SOURCE WS-G                    numeric-edited, no editing -> 4X7
      *>   col  9  PIC 9(5)   SOURCE WS-G                    longer than the group       -> 4X7 and two spaces on the right
      *>   col 15  PIC XX     SOURCE WS-G                    shorter than the group      -> 4X (truncated on the right)
      *>   col 18  PIC 9(4)   SOURCE WS-H  (12 and "AB")    a group of two leaves        -> 12AB
      *> Each window can fail: a numeric conversion of the group changes the first three windows, and a width that ignores the
      *> alphanumeric fit changes the third and fourth.  The line is `4X7 4X7 4X7   4X 12AB`.
      *> THE READ-BACK IS BYTE-WISE: a one-character record on a second SELECT over the report file is legal at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB5807GRPSRC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5807src.txt".
           SELECT CHK ASSIGN TO "pb5807src.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-G.
           05  WS-G1 PIC X(3) VALUE "4X7".
       01  WS-H.
           05  WS-H1 PIC 99 VALUE 12.
           05  WS-H2 PIC XX VALUE "AB".
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 999 SOURCE WS-G.
           03  COLUMN 5 PIC ZZ9 SOURCE WS-G.
           03  COLUMN 9 PIC 9(5) SOURCE WS-G.
           03  COLUMN 15 PIC XX SOURCE WS-G.
           03  COLUMN 18 PIC 9(4) SOURCE WS-H.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
