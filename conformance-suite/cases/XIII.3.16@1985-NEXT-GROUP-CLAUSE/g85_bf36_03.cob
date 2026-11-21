      *>--- conformance-case
      *> id: g85_bf36_03
      *> rules: [1985-GR-XIII.3.16.4-1, 1985-GR-XIII.3.16.4-2,
      *>    1985-GR-XIII.3.16.4-3, 1985-GR-XIII.3.16.4-4,
      *>    1985-GR-XIII.3.10.8-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.16.4 GR1 - "Any positioning of the page specified
      *    by the NEXT GROUP clause takes place after the presentation
      *    of the report group in which the clause appears": each group
      *    below prints where its own LINE clause puts it.
      *  §1985:XIII.3.16.4 GR2 - the RWCS uses NEXT GROUP with the TYPE
      *    and PAGE clauses and LINE-COUNTER "to determine a new value
      *    for LINE-COUNTER" (the values follow §1985:XIII.3.10.6 GR5,
      *    §1985:XIII.3.10.8 GR6 and §1985:XIII.3.10.9 GR5).
      *  §1985:XIII.3.16.4 GR3 - "The NEXT GROUP clause is ignored by
      *    the report writer control system when it is specified on a
      *    control footing report group that is at a level other than
      *    the highest level at which a control break is detected";
      *    §1985:XIII.3.10.8 GR6 b first paragraph: that footing's final
      *    LINE-COUNTER "is the line number on which the final print
      *    line of the control footing report group was presented".
      *  §1985:XIII.3.16.4 GR4 - a body group's NEXT GROUP moves the
      *    next body group; a report heading's moves the page heading; a
      *    page footing's moves the report footing.
      *  R-3 CONTROLS WS-MAJ WS-MIN, PAGE LIMIT 40 HEADING 1 FD 3 LD 30
      *  FOOTING 35; CF-MIN NEXT GROUP PLUS 3, CF-MAJ NEXT GROUP PLUS 2.
      *  Control footings see the prior control values (§1985:XIII.4.3.4
      *  GR6); TERMINATE acts "as though a control break has been sensed
      *  in the most major control data-name" (§1985:XIII.4.7.4 GR1).
      *   A1: DE line 3, LC 3.
      *   A2: minor break, CF-MIN is the highest level: line 4, LC 4 + 3
      *       = 7 (GR2, GR4: it moves the next body group); DE 8.
      *   B1: major break: CF-MIN line 9, NEXT GROUP ignored, LC 9 (GR3;
      *       "BEFORE CF-MAJ" shows 09, not 12); CF-MAJ line 10, LC 12;
      *       DE 13.
      *   TERMINATE: CF-MIN 14 (LC 14, ignored again), CF-MAJ 15.
      *  R-4 PAGE LIMIT 30 HEADING 1 FD 8 LD 15 FOOTING 20:
      *   RH LINE 2 NEXT GROUP 4 -> line 2, LC 4; PH LINE PLUS 1 -> 4 +
      *   1 = 5; DE -> FD 8; at TERMINATE PF LINE 22 NEXT GROUP 24 ->
      *   line 22, LC 24; RF LINE PLUS 2 -> 24 + 2 = 26
      *   (§1985:XIII.3.10.10 GR3b.
      *  FAILS IF a lower-level footing's NEXT GROUP is honoured, or a
      *  heading's or page footing's NEXT GROUP does not move the group
      *  that follows it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3603.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3603.txt".
           SELECT CHK ASSIGN TO "g85bf3603.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-3 R-4.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  WS-MAJ  PIC X     VALUE SPACE.
       01  WS-MIN  PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R-3 CONTROLS ARE WS-MAJ WS-MIN
                PAGE LIMIT 40 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 30 FOOTING 35.
       01  DE-3 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "D".
           02  COLUMN 2 PIC X SOURCE WS-MAJ.
           02  COLUMN 3 PIC 9 SOURCE WS-MIN.
       01  CF-MIN TYPE CF WS-MIN LINE PLUS 1 NEXT GROUP PLUS 3.
           02  COLUMN 1 PIC XX VALUE "FI".
           02  COLUMN 3 PIC 9 SOURCE WS-MIN.
       01  CF-MAJ TYPE CF WS-MAJ LINE PLUS 1 NEXT GROUP PLUS 2.
           02  COLUMN 1 PIC XX VALUE "FA".
           02  COLUMN 3 PIC X SOURCE WS-MAJ.
       RD  R-4 PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 8
                LAST DETAIL 15 FOOTING 20.
       01  RH-4 TYPE RH LINE 2 NEXT GROUP 4.
           02  COLUMN 1 PIC X(3) VALUE "RH4".
       01  PH-4 TYPE PH LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "PH4".
       01  DE-4 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE4".
       01  PF-4 TYPE PF LINE 22 NEXT GROUP 24.
           02  COLUMN 1 PIC X(3) VALUE "PF4".
       01  RF-4 TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RF4".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DMI SECTION.
           USE BEFORE REPORTING CF-MIN.
       DMI-1.
           MOVE LINE-COUNTER OF R-3 TO WS-LC.
           DISPLAY "BEFORE CF-MIN LINE-COUNTER " WS-LC.
       DMA SECTION.
           USE BEFORE REPORTING CF-MAJ.
       DMA-1.
           MOVE LINE-COUNTER OF R-3 TO WS-LC.
           DISPLAY "BEFORE CF-MAJ LINE-COUNTER " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           DISPLAY "==== R-3".
           OPEN OUTPUT PRT.
           INITIATE R-3.
           MOVE "A" TO WS-MAJ.
           MOVE 1 TO WS-MIN.
           GENERATE DE-3.
           PERFORM SHOW-LC.
           MOVE 2 TO WS-MIN.
           GENERATE DE-3.
           PERFORM SHOW-LC.
           MOVE "B" TO WS-MAJ.
           MOVE 1 TO WS-MIN.
           GENERATE DE-3.
           PERFORM SHOW-LC.
           TERMINATE R-3.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-4".
           OPEN OUTPUT PRT.
           INITIATE R-4.
           GENERATE DE-4.
           TERMINATE R-4.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       SHOW-LC.
           MOVE LINE-COUNTER OF R-3 TO WS-LC.
           DISPLAY "AFTER DE-3 " WS-MAJ WS-MIN " LINE-COUNTER " WS-LC.
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
