      *>--- conformance-case
      *> id: g85_bf36_01
      *> rules: [1985-GR-XIII.3.10.6-3, 1985-GR-XIII.3.10.6-5,
      *>    1985-GR-XIII.3.10.7-3, 1985-GR-XIII.3.10.6-p1,
      *>    1985-GR-XIII.3.10.7-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.10.6 GR3 - report heading first print line: a.
      *    absolute "on the line number specified by the integer of its
      *    LINE NUMBER clause"; b. relative, paged: "adding the integer
      *    of the first LINE NUMBER clause and the value obtained by
      *    subtracting 1 from the value of integer-2 of the HEADING
      *    phrase"; c. no LINE NUMBER clause: "The report heading report
      *    group is not presented"; d. unpaged: "adding the content of
      *    its LINE-COUNTER (in this case, zero) to the integer of the
      *    first LINE NUMBER clause".
      *  §1985:XIII.3.10.6 GR5 - its final LINE-COUNTER: a. absolute
      *    NEXT GROUP: "places the NEXT GROUP integer into
      *    LINE-COUNTER"; b. relative: the sum of the NEXT GROUP integer
      *    and the final print line; c. NEXT GROUP NEXT PAGE: "places
      *    zero into LINE-COUNTER"; d. none: the final print line; e.
      *    "LINE-COUNTER is unaffected by the processing of a
      *    nonprintable report group". §1985:XIII.3.10.6 GR4 c: with
      *    NEXT GROUP NEXT PAGE the report heading is "presented
      *    entirely by itself on the first page".
      *  §1985:XIII.3.10.7 GR3 - page heading first print line: a.
      *    absolute integer; b. relative: after a report heading on the
      *    same page "the sum of the final LINE-COUNTER setting
      *    established by the report heading report group and the
      *    integer of the first LINE NUMBER clause of the page heading
      *    report group", "Otherwise" integer + HEADING - 1. (Folio
      *    XIII-31 is transcribed from the GovInfo scan, kb/Work
      *    PB5789. Its b. is also what §1985:XIII.3.16.4 GR4 and
      *    §1985:XIII.3.10.6 GR5 require: "The NEXT GROUP clause of a
      *    report heading report group can affect the location at
      *    which the page heading report group is presented".)
      *    §1985:XIII.3.10.7 GR4 a:
      *    the page heading's final LINE-COUNTER is its final print
      *    line.
      *  Body groups: §1985:XIII.3.10.8 GR4 b, LINE-COUNTER below FIRST
      *    DETAIL puts the first body group on the FIRST DETAIL line.
      *  §1985:XIII.4.9.4 GR4 - a USE BEFORE REPORTING procedure runs
      *    "just before the named report group is produced", so each
      *    "BEFORE PH" line shows the report heading's final
      *    LINE-COUNTER.
      *  DERIVATION (PAGE LIMIT 20; LINE-COUNTER 0 after INITIATE):
      *   R-A HEADING 2 FD 8: RH LINE 3 NEXT GROUP 5 -> line 3 (3a), LC
      *       5 (5a); PH LINE PLUS 1 -> 5 + 1 = 6; DE -> FD 8.
      *   R-B HEADING 2 FD 8: RH LINE PLUS 2, LINE PLUS 1, NEXT GROUP
      *       PLUS 1 -> 2 + (2 - 1) = 3 (3b) and 4; LC 4 + 1 = 5 (5b);
      *       PH LINE 6 (PH 3a); DE -> 8.
      *   R-C HEADING 2 FD 6: RH LINE 2 NEXT GROUP NEXT PAGE -> page 1
      *       line 2 alone; LC 0 (5c); page 2: PH LINE PLUS 2, no report
      *       heading on this page -> 2 + (2 - 1) = 3; DE -> FD 6.
      *   R-D HEADING 2 FD 8: RH LINE 2, LINE PLUS 1 -> 2, 3; LC 3 (5d);
      *       PH LINE PLUS 2 -> 3 + 2 = 5; DE -> 8.
      *   R-G HEADING 3 FD 8: RH with no LINE (nonprintable, 3c) - not
      *       presented, LC stays 0 (5e); PH LINE PLUS 1 -> 1 + (3 - 1)
      *       = 3 (the "Otherwise" arm); DE -> 8.
      *   R-E unpaged: RH LINE PLUS 2 NEXT GROUP PLUS 3 -> 0 + 2 = 2
      *       (3d), LC 2 + 3 = 5 (5b); DE LINE PLUS 1 -> 6, LC 6.
      *   R-F unpaged: RH LINE PLUS 1, LINE PLUS 1 -> 1, 2 (3d), LC 2
      *       (5d); DE LINE PLUS 2 -> 4, LC 4.
      *  FAILS IF the heading formula, a NEXT GROUP arm or the
      *  "presented on this page" test is wrong: each moves a line or a
      *  counter.
      *  The read-back numbers each physical line (a form feed starts a
      *  new page) and shows only non-blank lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3601.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3601.txt".
           SELECT CHK ASSIGN TO "g85bf3601.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-A R-B R-C R-D R-G R-E R-F.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-A PAGE LIMIT 20 LINES HEADING 2 FIRST DETAIL 8
                LAST DETAIL 15 FOOTING 17.
       01  RH-A TYPE RH LINE 3 NEXT GROUP 5.
           02  COLUMN 1 PIC X(4) VALUE "RHA".
       01  PH-A TYPE PH LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "PHA".
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DEA".
       RD  R-B PAGE LIMIT 20 LINES HEADING 2 FIRST DETAIL 8
                LAST DETAIL 15 FOOTING 17.
       01  RH-B TYPE RH NEXT GROUP PLUS 1.
           02  LINE PLUS 2.
               03  COLUMN 1 PIC X(4) VALUE "RHB1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RHB2".
       01  PH-B TYPE PH LINE 6.
           02  COLUMN 1 PIC X(4) VALUE "PHB".
       01  DE-B TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DEB".
       RD  R-C PAGE LIMIT 20 LINES HEADING 2 FIRST DETAIL 6
                LAST DETAIL 15 FOOTING 17.
       01  RH-C TYPE RH LINE 2 NEXT GROUP NEXT PAGE.
           02  COLUMN 1 PIC X(4) VALUE "RHC".
       01  PH-C TYPE PH LINE PLUS 2.
           02  COLUMN 1 PIC X(4) VALUE "PHC".
       01  DE-C TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DEC".
       RD  R-D PAGE LIMIT 20 LINES HEADING 2 FIRST DETAIL 8
                LAST DETAIL 15 FOOTING 17.
       01  RH-D TYPE RH.
           02  LINE 2.
               03  COLUMN 1 PIC X(4) VALUE "RHD1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RHD2".
       01  PH-D TYPE PH LINE PLUS 2.
           02  COLUMN 1 PIC X(4) VALUE "PHD".
       01  DE-D TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DED".
       RD  R-G PAGE LIMIT 20 LINES HEADING 3 FIRST DETAIL 8
                LAST DETAIL 15 FOOTING 17.
       01  RH-G TYPE RH.
           02  RH-G-N PIC 9 SOURCE WS-PG.
       01  PH-G TYPE PH LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "PHG".
       01  DE-G TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DEG".
       RD  R-E.
       01  RH-E TYPE RH LINE PLUS 2 NEXT GROUP PLUS 3.
           02  COLUMN 1 PIC X(4) VALUE "RHE".
       01  DE-E TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DEE".
       RD  R-F.
       01  RH-F TYPE RH.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RHF1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RHF2".
       01  DE-F TYPE DE LINE PLUS 2.
           02  COLUMN 1 PIC X(4) VALUE "DEF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE BEFORE REPORTING PH-A.
       DA-1.
           MOVE LINE-COUNTER OF R-A TO WS-LC.
           DISPLAY "BEFORE PH-A LINE-COUNTER " WS-LC.
       DB SECTION.
           USE BEFORE REPORTING PH-B.
       DB-1.
           MOVE LINE-COUNTER OF R-B TO WS-LC.
           DISPLAY "BEFORE PH-B LINE-COUNTER " WS-LC.
       DC SECTION.
           USE BEFORE REPORTING PH-C.
       DC-1.
           MOVE LINE-COUNTER OF R-C TO WS-LC.
           DISPLAY "BEFORE PH-C LINE-COUNTER " WS-LC.
       DD SECTION.
           USE BEFORE REPORTING PH-D.
       DD-1.
           MOVE LINE-COUNTER OF R-D TO WS-LC.
           DISPLAY "BEFORE PH-D LINE-COUNTER " WS-LC.
       DG SECTION.
           USE BEFORE REPORTING PH-G.
       DG-1.
           MOVE LINE-COUNTER OF R-G TO WS-LC.
           DISPLAY "BEFORE PH-G LINE-COUNTER " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           DISPLAY "==== R-A".
           OPEN OUTPUT PRT.
           INITIATE R-A.
           GENERATE DE-A.
           TERMINATE R-A.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-B".
           OPEN OUTPUT PRT.
           INITIATE R-B.
           GENERATE DE-B.
           TERMINATE R-B.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-C".
           OPEN OUTPUT PRT.
           INITIATE R-C.
           GENERATE DE-C.
           TERMINATE R-C.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-D".
           OPEN OUTPUT PRT.
           INITIATE R-D.
           GENERATE DE-D.
           TERMINATE R-D.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-G".
           OPEN OUTPUT PRT.
           INITIATE R-G.
           GENERATE DE-G.
           TERMINATE R-G.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-E".
           OPEN OUTPUT PRT.
           INITIATE R-E.
           GENERATE DE-E.
           MOVE LINE-COUNTER OF R-E TO WS-LC.
           DISPLAY "AFTER DE-E LINE-COUNTER " WS-LC.
           TERMINATE R-E.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-F".
           OPEN OUTPUT PRT.
           INITIATE R-F.
           GENERATE DE-F.
           MOVE LINE-COUNTER OF R-F TO WS-LC.
           DISPLAY "AFTER DE-F LINE-COUNTER " WS-LC.
           TERMINATE R-F.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE "N" TO WS-EOF.
           MOVE 0 TO WS-LN.
           MOVE 0 TO WS-I.
           MOVE 1 TO WS-PG.
           MOVE SPACES TO WS-LINE.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC = SYM-FF
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   ADD 1 TO WS-PG
                   MOVE 0 TO WS-LN
               ELSE
                   IF CHK-REC NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE CHK-REC TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "PAGE " WS-PG " LINE " WS-LN " [" WS-LINE(1:4)
                   "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
