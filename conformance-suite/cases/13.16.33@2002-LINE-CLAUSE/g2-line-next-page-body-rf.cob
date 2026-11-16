      *>--- conformance-case
      *> id: g2-line-next-page-body-rf
      *> rules: [2002-SR-13.16.33.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.33.2 SR8 - "The NEXT PAGE phrase may appear only
      *> in the description of a body group or a report footing".
      *> The granted forms are written on a control heading (CH-F)
      *> and a detail (DE-N), both body groups (§2002:13.16.55.2
      *> SR15), and on the report footing (RF-N); the program must be
      *> ACCEPTED and each phrase acts as 13.16.33.3 says.
      *> R-L: PAGE LIMIT 8, HEADING 1, FIRST DETAIL 2, LAST DETAIL 6,
      *> FOOTING 7; no page heading or footing. INITIATE: PC 1.
      *>  GENERATE 1: CH FINAL is printed first (14.8.15.3 GR4 c));
      *>    it is the chronologically first body group, so no page
      *>    fit test (13.16.33.3 GR4): page 1 line 2 "H1".
      *>    DE-N: its first LINE has NEXT PAGE -> fit unsuccessful
      *>    (GR4 a)) -> page advance (14.8.15.3 GR6): PC 2, LC 0;
      *>    page 2 line 4 "D1".
      *>  GENERATE 2: DE-N again -> advance: PC 3; page 3 line 4 "D2".
      *>  TERMINATE: no control footing; the report footing has an
      *>    absolute LINE with NEXT PAGE -> "the first line is
      *>    printed beginning on a new page" (GR5 a)): page 4 line 3
      *>    "R1".
      *> The printed values are fixed by VALUE and by SOURCE WS-N,
      *> never by PAGE-COUNTER or LINE-COUNTER: §2002:13.16.51.3 GR3
      *> puts the implicit SOURCE MOVE only "before the associated
      *> report line is printed", unordered against the page fit
      *> test and the LINE-COUNTER update, so a counter's printed
      *> value is not fixed by the 2002 text. The page positions are
      *> pinned by the line numbering and PAGE markers below.
      *> The read-back numbers each physical line of each page and
      *> shows each form feed as a PAGE marker. Fails if any NEXT
      *> PAGE is refused, ignored, or advances on the first group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LIN01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2lin01.txt".
           SELECT CHK ASSIGN TO "g2lin01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-L.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  WS-N    PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R-L CONTROL IS FINAL
               PAGE LIMIT 8 LINES HEADING 1 FIRST DETAIL 2
               LAST DETAIL 6 FOOTING 7.
       01  CH-F TYPE CH FINAL.
           02  LINE 2 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "H".
               03  COLUMN 2 PIC X VALUE "1".
       01  DE-N TYPE DE.
           02  LINE 4 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "D".
               03  COLUMN 2 PIC 9 SOURCE WS-N.
       01  RF-N TYPE RF.
           02  LINE 3 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "R".
               03  COLUMN 2 PIC X VALUE "1".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT
           INITIATE R-L
           MOVE 1 TO WS-N
           GENERATE DE-N
           MOVE 2 TO WS-N
           GENERATE DE-N
           TERMINATE R-L
           CLOSE PRT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = X"0A"
                   PERFORM SHOW-LINE
               WHEN CHK-REC = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "---- PAGE ----"
                   MOVE 0 TO WS-LN
               WHEN CHK-REC = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN
           DISPLAY "LINE " WS-LN " [" WS-LINE (1:4) "]"
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
