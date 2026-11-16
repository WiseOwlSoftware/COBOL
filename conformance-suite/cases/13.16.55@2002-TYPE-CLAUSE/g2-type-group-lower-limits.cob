      *>--- conformance-case
      *> id: g2-type-group-lower-limits
      *> rules: [2002-GR-13.16.55.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR8 - "The lower limit is defined to be the
      *> lowermost permitted line on the page that may be occupied by the
      *> report group's last line", items a) to g). A relative body group
      *> other than the first since INITIATE is page-fit tested: "If the
      *> trial sum is less than or equal to the lower limit for the
      *> report group, the page fit is declared successful"
      *> (§2002:13.16.33.3 GR4), otherwise a page advance precedes it and
      *> "if that report group is the first body group to be printed on
      *> the current page, the line number is given by the FIRST DETAIL
      *> integer" (§2002:13.16.33.3 GR5b). Each printed report file is
      *> read back byte by byte (the implementor's line feed ends a line,
      *> form feed advances a page) and its non-blank lines DISPLAYed.
      *> R: PAGE LIMIT 14, HEADING 1, FIRST DETAIL 4, LAST CONTROL
      *> HEADING 5, LAST DETAIL 8, FOOTING 11; CONTROL CK; DE, CH and CF
      *> are LINE PLUS 1; CK changes from A to B before the sixth detail.
      *>  b) RH not on a page by itself: lower limit is the line before
      *>     PH's first line 2, i.e. line 1, and RH LINE 1 is on it;
      *>  c) PH lower limit FIRST DETAIL - 1 = 3: PH ends on line 3;
      *>  e) detail lower limit LAST DETAIL 8: D1..D4 on 5..8; D5's trial
      *>     sum 9 > 8, so D5 opens page 2 at FIRST DETAIL 4;
      *>  f) CF lower limit FOOTING 11: CF(A) trial 5 fits on page 2, and
      *>     at TERMINATE CF(B) prints on line 9 of page 3, BELOW LAST
      *>     DETAIL but within FOOTING;
      *>  d) CH lower limit LAST CONTROL HEADING 5: CH(B) trial 6 > 5, so
      *>     CH(B) opens page 3 at line 4 (while D5 at line 4 and CF(A) at
      *>     5 stayed on page 2);
      *>  g) PF lower limit is the page limit: PF's last line PZ is on 14.
      *> R2: PAGE LIMIT 6, FIRST DETAIL 3; its RH has NEXT GROUP NEXT
      *> PAGE, so it "is printed on the first page of the report as the
      *> only report group on that page" (§2002:13.16.35.3 GR3c):
      *>  a) RH on a page by itself: lower limit is the page limit 6, so
      *>     its second line RB is on line 6; DE then opens page 2 at 3.
      *> A leg fails if any group is printed below its lower limit, or a
      *> group that fits (CF(A), CF(B)) is moved to a new page.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2TYP08.TXT".
           SELECT CHK ASSIGN TO "G2TYP08.TXT".
           SELECT PRT2 ASSIGN TO "G2TYP08B.TXT".
           SELECT CHK2 ASSIGN TO "G2TYP08B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       FD  PRT2 REPORT IS R2.
       FD  CHK2.
       01  CHK2-REC PIC X.
       WORKING-STORAGE SECTION.
       01  CK      PIC X     VALUE "A".
       01  N       PIC 9     VALUE 0.
       01  K       PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL IS CK
           PAGE LIMIT IS 14 LINES HEADING 1 FIRST DETAIL 4
           LAST CONTROL HEADING 5 LAST DETAIL 8 FOOTING 11.
       01  RH-G TYPE RH LINE 1.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH-G TYPE PH.
           03  LINE 2.
               05  COLUMN 1 PIC X(2) VALUE "PH".
           03  LINE 3.
               05  COLUMN 1 PIC X(2) VALUE "P3".
       01  CH-G TYPE CH CK LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "CH".
       01  DE-G TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
           03  COLUMN 2 PIC 9 SOURCE N.
       01  CF-G TYPE CF CK LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "CF".
       01  PF-G TYPE PF.
           03  LINE 12.
               05  COLUMN 1 PIC X(2) VALUE "PF".
           03  LINE 14.
               05  COLUMN 1 PIC X(2) VALUE "PZ".
       RD  R2 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3.
       01  RH2-G TYPE RH NEXT GROUP NEXT PAGE.
           03  LINE 1.
               05  COLUMN 1 PIC X(2) VALUE "RA".
           03  LINE 6.
               05  COLUMN 1 PIC X(2) VALUE "RB".
       01  DE2-G TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 9
               MOVE K TO N
               IF K = 6
                   MOVE "B" TO CK
               END-IF
               GENERATE DE-G
           END-PERFORM.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           OPEN OUTPUT PRT2.
           INITIATE R2.
           GENERATE DE2-G.
           TERMINATE R2.
           CLOSE PRT2.
           DISPLAY "R2".
           MOVE "N" TO WS-EOF.
           MOVE 0 TO WS-LN WS-I.
           MOVE 1 TO WS-PG.
           OPEN INPUT CHK2.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK2-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK2.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
                   ADD 1 TO WS-PG
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "P" WS-PG " LINE " WS-LN " [" WS-LINE(1:2) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
