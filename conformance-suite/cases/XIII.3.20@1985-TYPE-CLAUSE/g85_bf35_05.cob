      *>--- conformance-case
      *> id: g85_bf35_05
      *> rules: [1985-GR-XIII.3.20.4-2, 1985-GR-XIII.3.20.4-3, 1985-GR-XIII.3.20.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR2 - the report heading is processed once
      *> per report, as its first group, during the chronologically
      *> first GENERATE; GR3 - the page heading is the first group of
      *> each page, except a) not on a page holding only a report
      *> heading or only a report footing, b) the second group after a
      *> report heading not on a page by itself; GR7 - the page footing
      *> is the last group of each page, except a) not on a page
      *> holding only a report heading or only a report footing, b)
      *> second to last before a report footing not on a page by
      *> itself.
      *>
      *> PAGE LIMIT 10 HEADING 1 FIRST DETAIL 3 LAST DETAIL 6 FOOTING
      *> 7. Each report file is read back as <report> P<page> L<line>
      *> <text> (a form feed starts a new page). R1: RH LINE 1, PH LINE
      *> PLUS 1, detail LINE PLUS 1, PF LINE 9, RF LINE PLUS 1; six
      *> GENERATEs. R2: RH LINE 1 NEXT GROUP NEXT PAGE, PH LINE 1, PF
      *> LINE 9, RF LINE 2 ON NEXT PAGE; one GENERATE.
      *>
      *> DERIVATION R1. "RH USE" is DISPLAYed between "AFTER INITIATE"
      *> and "AFTER GENERATE 1", once (GR2; its USE procedure runs as
      *> the group is processed, §1985:XIII.3.20.4 GR9 a). Page 1: RH
      *> on line 1 (§1985:XIII.3.10.6 GR3 a), final LINE-COUNTER 1 (GR5
      *> d); PH second, on 1 + 1 = line 2 (GR3 b; §1985:XIII.3.10.7 GR3
      *> b, folio XIII-31, kb/Work PB5789); details on
      *> lines 3 to 6 (the first on FIRST DETAIL, §1985:XIII.3.10.8 GR4
      *> b, then LINE-COUNTER + 1, GR4 d). The fifth detail's trial sum
      *> 7 exceeds LAST DETAIL 6, so a page advance (§1985:XIII.3.10.8
      *> GR3 b): PF last on page 1, line 9 (§1985:XIII.3.10.9 GR3 a).
      *> Page 2: PH first, line 1 (no report heading on it: 1 + HEADING
      *> 1 - 1); details 5 and 6 on lines 3, 4; at TERMINATE PF on line
      *> 9 is second to last (GR7 b), and RF on 9 + 1 = line 10
      *> (§1985:XIII.3.10.10 GR3 b).
      *>
      *> DERIVATION R2. Page 1 holds the report heading alone
      *> (§1985:XIII.3.10.6 GR4 c): no PH (GR3 a) and no PF (GR7 a) on
      *> it. Page 2: PH line 1, the detail on FIRST DETAIL line 3, PF
      *> line 9. Page 3 holds the report footing alone on line 2
      *> (§1985:XIII.3.10.10 GR3 c): no PH (GR3 a), no PF (GR7 a). PH
      *> and PF show PAGE-COUNTER (one per page advance).
      *>
      *> Fails if the report heading is processed at INITIATE or more
      *> than once, if a page heading or page footing appears on R2's
      *> page 1 or page 3, if R1's page heading precedes the report
      *> heading, or if R1's report footing precedes the last page
      *> footing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3505.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT1 ASSIGN TO "g85bf3505-1.txt".
           SELECT PRT2 ASSIGN TO "g85bf3505-2.txt".
           SELECT CHK1 ASSIGN TO "g85bf3505-1.txt".
           SELECT CHK2 ASSIGN TO "g85bf3505-2.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT1 REPORT IS R1.
       FD  PRT2 REPORT IS R2.
       FD  CHK1.
       01  CHK1-REC      PIC X.
       FD  CHK2.
       01  CHK2-REC      PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-B            PIC X.
       01  WS-I            PIC 99    VALUE 0.
       01  WS-E            PIC 99    VALUE 0.
       01  WS-P            PIC 9     VALUE 1.
       01  WS-L            PIC 99    VALUE 1.
       01  WS-TAG          PIC X     VALUE SPACE.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       01  WS-N            PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  RH1 TYPE REPORT HEADING LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "RH".
       01  TYPE PAGE HEADING LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "PH ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  D-1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE ".
           02  COLUMN 4 PIC 9 SOURCE WS-N.
       01  TYPE PAGE FOOTING LINE 9.
           02  COLUMN 1 PIC X(3) VALUE "PF ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  TYPE REPORT FOOTING LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "RF ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       RD  R2 PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  TYPE REPORT HEADING LINE 1 NEXT GROUP NEXT PAGE.
           02  COLUMN 1 PIC X(2) VALUE "RH".
       01  TYPE PAGE HEADING LINE 1.
           02  COLUMN 1 PIC X(3) VALUE "PH ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  D-2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE ".
           02  COLUMN 4 PIC 9 SOURCE WS-N.
       01  TYPE PAGE FOOTING LINE 9.
           02  COLUMN 1 PIC X(3) VALUE "PF ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  TYPE REPORT FOOTING LINE 2 ON NEXT PAGE.
           02  COLUMN 1 PIC X(3) VALUE "RF ".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH1 SECTION.
           USE BEFORE REPORTING RH1.
       U-RH1-P.
           DISPLAY "RH USE".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT1 PRT2.
           INITIATE R1 R2.
           DISPLAY "AFTER INITIATE".
           MOVE 1 TO WS-N.
           GENERATE D-1.
           DISPLAY "AFTER GENERATE 1".
           PERFORM 5 TIMES
               ADD 1 TO WS-N
               GENERATE D-1
           END-PERFORM.
           MOVE 1 TO WS-N.
           GENERATE D-2.
           TERMINATE R1 R2.
           CLOSE PRT1 PRT2.
           PERFORM READ-1.
           PERFORM READ-2.
           STOP RUN.
       READ-1.
           MOVE "1" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHK1.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK1
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK1-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK1.
           PERFORM SHOW-LINE.
       READ-2.
           MOVE "2" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHK2.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK2-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK2.
           PERFORM SHOW-LINE.
       RESET-RB.
           MOVE "N" TO WS-EOF.
           MOVE 1 TO WS-P.
           MOVE 1 TO WS-L.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
           MOVE SPACES TO WS-LINE.
       TAKE-BYTE.
           IF WS-B = SYM-LF
               PERFORM SHOW-LINE
               ADD 1 TO WS-L
           ELSE
               IF WS-B = SYM-FF
                   PERFORM SHOW-LINE
                   ADD 1 TO WS-P
                   MOVE 1 TO WS-L
               ELSE
                   IF WS-B NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE WS-B TO WS-LINE (WS-I:1)
                       IF WS-B NOT = SPACE
                           MOVE WS-I TO WS-E
                       END-IF
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY WS-TAG " P" WS-P " L" WS-L " "
                   WS-LINE (1:WS-E)
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
