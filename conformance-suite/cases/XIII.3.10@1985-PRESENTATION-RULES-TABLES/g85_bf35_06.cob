      *>--- conformance-case
      *> id: g85_bf35_06
      *> rules: [1985-GR-XIII.3.10.10-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.10 GR3 - first print line position of the
      *> report footing: a) an absolute LINE: that line; b) relative:
      *> the page footing's final LINE-COUNTER + the integer when a
      *> page footing was presented on the page, else integer-5
      *> (FOOTING) + the integer; c) LINE n ON NEXT PAGE: line n of a
      *> page holding no other group; d) no PAGE clause: LINE-COUNTER +
      *> the integer; e) no LINE clause: not presented.
      *>
      *> Six reports, each with one file read back as <report> P<page>
      *> L<line> <text>. Paged ones: PAGE LIMIT 10 HEADING 1 FIRST
      *> DETAIL 3 LAST DETAIL 6 FOOTING 7; each detail is LINE PLUS 1
      *> and lands on FIRST DETAIL line 3 (§1985:XIII.3.10.8 GR4 b).
      *>
      *> RA (rule a): RF LINE 8 -> line 8. RB (rule b, first sentence):
      *> PF LINE 8, final LINE-COUNTER 8 (§1985:XIII.3.10.9 GR5 c); RF
      *> LINE PLUS 2 -> 8 + 2 = line 10. RC (rule b, "Otherwise"): no
      *> PF; RF LINE PLUS 3 -> FOOTING 7 + 3 = line 10 (not LINE-
      *> COUNTER 3 + 3). RN (rule c): PF LINE 8 on page 1, then RF LINE
      *> 2 ON NEXT PAGE on line 2 of page 2, with no page footing on
      *> that page (§1985:XIII.3.20.4 GR7 a). RU (rule d): no PAGE
      *> clause; two details on lines 1 and 2; RF LINE PLUS 2 -> 2 + 2
      *> = line 4. RE (rule e): RF with no LINE clause and no printable
      *> item (§1985:XIII.3.15.3 SR8): its USE procedure runs ("RFE
      *> USE", during TERMINATE, §1985:XIII.3.20.4 GR9 a b) and the
      *> file holds only the detail.
      *>
      *> Fails if any footing line lands elsewhere: RB on 9 (FOOTING 7
      *> + 2, ignoring the page footing), RC on 6 (LINE-COUNTER + 3),
      *> RN on page 1, RU anywhere but 4, or RE presenting a line or
      *> skipping its USE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3506.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTA ASSIGN TO "g85bf3506a.txt".
           SELECT PRTB ASSIGN TO "g85bf3506b.txt".
           SELECT PRTC ASSIGN TO "g85bf3506c.txt".
           SELECT PRTN ASSIGN TO "g85bf3506n.txt".
           SELECT PRTU ASSIGN TO "g85bf3506u.txt".
           SELECT PRTE ASSIGN TO "g85bf3506e.txt".
           SELECT CHKA ASSIGN TO "g85bf3506a.txt".
           SELECT CHKB ASSIGN TO "g85bf3506b.txt".
           SELECT CHKC ASSIGN TO "g85bf3506c.txt".
           SELECT CHKN ASSIGN TO "g85bf3506n.txt".
           SELECT CHKU ASSIGN TO "g85bf3506u.txt".
           SELECT CHKE ASSIGN TO "g85bf3506e.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTA REPORT IS RA.
       FD  PRTB REPORT IS RB.
       FD  PRTC REPORT IS RC.
       FD  PRTN REPORT IS RN.
       FD  PRTU REPORT IS RU.
       FD  PRTE REPORT IS RE.
       FD  CHKA.
       01  CHKA-REC      PIC X.
       FD  CHKB.
       01  CHKB-REC      PIC X.
       FD  CHKC.
       01  CHKC-REC      PIC X.
       FD  CHKN.
       01  CHKN-REC      PIC X.
       FD  CHKU.
       01  CHKU-REC      PIC X.
       FD  CHKE.
       01  CHKE-REC      PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-B            PIC X.
       01  WS-I            PIC 99    VALUE 0.
       01  WS-E            PIC 99    VALUE 0.
       01  WS-P            PIC 9     VALUE 1.
       01  WS-L            PIC 99    VALUE 1.
       01  WS-TAG          PIC X     VALUE SPACE.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       01  WS-TXT          PIC X(3)  VALUE "TXT".
       REPORT SECTION.
       RD  RA PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  D-A TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DA".
       01  TYPE REPORT FOOTING LINE 8.
           02  COLUMN 1 PIC X(3) VALUE "RFA".
       RD  RB PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  D-B TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DB".
       01  TYPE PAGE FOOTING LINE 8.
           02  COLUMN 1 PIC X(3) VALUE "PFB".
       01  TYPE REPORT FOOTING LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFB".
       RD  RC PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  D-C TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DC".
       01  TYPE REPORT FOOTING LINE PLUS 3.
           02  COLUMN 1 PIC X(3) VALUE "RFC".
       RD  RN PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  D-N TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DN".
       01  TYPE PAGE FOOTING LINE 8.
           02  COLUMN 1 PIC X(3) VALUE "PFN".
       01  TYPE REPORT FOOTING LINE 2 ON NEXT PAGE.
           02  COLUMN 1 PIC X(3) VALUE "RFN".
       RD  RU.
       01  D-U TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DD".
       01  TYPE REPORT FOOTING LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFD".
       RD  RE PAGE LIMIT 10 LINES HEADING 1
               FIRST DETAIL 3 LAST DETAIL 6 FOOTING 7.
       01  D-E TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DX".
       01  RF-E TYPE REPORT FOOTING.
           02  RF-E-V PIC X(3) SOURCE WS-TXT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RFE SECTION.
           USE BEFORE REPORTING RF-E.
       U-RFE-P.
           DISPLAY "RFE USE".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRTA PRTB PRTC PRTN PRTU PRTE.
           INITIATE RA RB RC RN RU RE.
           GENERATE D-A.
           GENERATE D-B.
           GENERATE D-C.
           GENERATE D-N.
           GENERATE D-U.
           GENERATE D-U.
           GENERATE D-E.
           TERMINATE RA RB RC RN RU RE.
           CLOSE PRTA PRTB PRTC PRTN PRTU PRTE.
           PERFORM READ-A.
           PERFORM READ-B.
           PERFORM READ-C.
           PERFORM READ-N.
           PERFORM READ-U.
           PERFORM READ-E.
           STOP RUN.
       READ-A.
           MOVE "A" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKA.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKA
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKA-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKA.
           PERFORM SHOW-LINE.
       READ-B.
           MOVE "B" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKB.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKB-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKB.
           PERFORM SHOW-LINE.
       READ-C.
           MOVE "C" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKC.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKC
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKC-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKC.
           PERFORM SHOW-LINE.
       READ-N.
           MOVE "N" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKN.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKN
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKN-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKN.
           PERFORM SHOW-LINE.
       READ-U.
           MOVE "U" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKU.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKU
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKU-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKU.
           PERFORM SHOW-LINE.
       READ-E.
           MOVE "E" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKE.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKE
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKE-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKE.
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
