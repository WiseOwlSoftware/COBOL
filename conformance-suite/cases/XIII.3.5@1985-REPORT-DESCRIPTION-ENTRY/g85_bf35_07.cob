      *>--- conformance-case
      *> id: g85_bf35_07
      *> rules: [1985-GR-XIII.3.5.5-4, 1985-GR-XIII.3.5.5-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.5 GR4 - INITIATE sets PAGE-COUNTER to one; GR5
      *> - PAGE-COUNTER is incremented by one at each page advance.
      *> Pinned by "R1 PAGE-COUNTER AFTER INITIATE 01" (both times: the
      *> second INITIATE follows a MOVE 7 TO PAGE-COUNTER, which
      *> §1985:XIII.3.5.5 SR6 allows, and a TERMINATE, §1985:XIII.4.4.4
      *> GR3), by "AFTER TERMINATE 02" (one advance in R1), and by the
      *> page heading and page footing lines of the read-back, which
      *> SOURCE PAGE-COUNTER: R1 page 1 shows 1 and page 2 shows 2; R2
      *> shows 2 on page 2 and 3 on its report-footing page 3. The
      *> second INITIATE R1 is followed by TERMINATE R1 with no
      *> GENERATE, which produces no report group (§1985:XIII.4.7.4
      *> GR2): R1's file keeps the first run's lines.
      *>
      *> The report layout and its derivation are those of g85_bf35_05
      *> (§1985:XIII.3.20.4 GR2, GR3, GR7; PAGE LIMIT 10 HEADING 1
      *> FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7): R1 advances once
      *> (fifth detail), R2 twice (the report heading alone on page 1,
      *> §1985:XIII.3.10.6 GR4 c; the report footing alone on page 3,
      *> §1985:XIII.3.10.10 GR3 c).
      *>
      *> Fails if INITIATE leaves PAGE-COUNTER at 0 or at the moved 7,
      *> if the first page is counted as an advance (page 1 showing 2),
      *> or if a page advance does not add one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3507.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT1 ASSIGN TO "g85bf3507-1.txt".
           SELECT PRT2 ASSIGN TO "g85bf3507-2.txt".
           SELECT CHK1 ASSIGN TO "g85bf3507-1.txt".
           SELECT CHK2 ASSIGN TO "g85bf3507-2.txt".
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
       01  WS-PC           PIC 99    VALUE 0.
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
           MOVE PAGE-COUNTER IN R1 TO WS-PC.
           DISPLAY "R1 PAGE-COUNTER AFTER INITIATE " WS-PC.
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
           MOVE PAGE-COUNTER IN R1 TO WS-PC.
           DISPLAY "R1 PAGE-COUNTER AFTER TERMINATE " WS-PC.
           MOVE 7 TO PAGE-COUNTER IN R1.
           MOVE PAGE-COUNTER IN R1 TO WS-PC.
           DISPLAY "R1 PAGE-COUNTER MOVED " WS-PC.
           INITIATE R1.
           MOVE PAGE-COUNTER IN R1 TO WS-PC.
           DISPLAY "R1 PAGE-COUNTER AFTER INITIATE " WS-PC.
           TERMINATE R1.
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
