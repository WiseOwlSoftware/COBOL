      *>--- conformance-case
      *> id: 1985-GR-XIII_1_1-p1
      *> rules: [1985-GR-XIII.1.1-p1, 1985-GR-XIII.1.1-p2, 1985-GR-XIII.1.2.1-p2, 1985-GR-XIII.2.3.1-p1, 1985-GR-XIII.3.2.1-p1, 1985-GR-XIII.3.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.1.1 p1; §1985:XIII.1.1 p2; §1985:XIII.1.2.1 p2;
      *> §1985:XIII.2.3.1 p1; §1985:XIII.3.2.1 p1; §1985:XIII.3.4.1 p1
      *> (INTRODUCTION TO THE REPORT WRITER MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-17.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * XIII.1.1-p1/p2, XIII.1.2.1-p2, XIII.2.3.1-p1, XIII.3.2.1-p1,
      * XIII.3.3.1-p1, XIII.3.4.1-p1: a report file named by a file
      * control entry, described by an FD with a REPORT clause naming
      * two reports, accessed by OPEN, INITIATE, GENERATE, SUPPRESS,
      * TERMINATE, USE AFTER STANDARD EXCEPTION PROCEDURE, USE BEFORE
      * REPORTING and CLOSE. R-1's RD gives the page regions and the
      * control CK; a change of CK produces the control groups (CF of
      * A, CH of B). DE-1 is a report group of two lines; DX is
      * suppressed in its USE BEFORE REPORTING procedure. R-2 is the
      * second report of the same file.
      * Expected: see rw_access_85.out (derived in-line below).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWACC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "rwacc.txt".
           SELECT CHK ASSIGN TO "rwacc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-1 R-2.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  CK      PIC X     VALUE SPACE.
       01  AMT     PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LINE PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE CK
           PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 4
           LAST DETAIL 20 FOOTING 25.
       01  PH-1 TYPE PH LINE 2.
           02  COLUMN 1 PIC X(2) VALUE "PG".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  CH-1 TYPE CH CK LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CH".
           02  COLUMN 4 PIC X SOURCE CK.
       01  DE-1 TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "D1".
               03  D-AMT COLUMN 4 PIC 9 SOURCE AMT.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "D2".
       01  DX TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DX".
       01  CF-1 TYPE CF CK LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CF".
           02  COLUMN 4 PIC 99 SUM AMT UPON DE-1.
       RD  R-2.
       01  DE-2 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "R2".
       PROCEDURE DIVISION.
       DECLARATIVES.
       UX SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON PRT.
       UX-1.
           DISPLAY "EXCEPTION".
       UB SECTION.
           USE BEFORE REPORTING DX.
       UB-1.
           SUPPRESS PRINTING.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE "A" TO CK. MOVE 1 TO AMT.
           GENERATE DE-1.
           GENERATE DX.
           MOVE 2 TO AMT.
           GENERATE DE-1.
           MOVE "B" TO CK. MOVE 4 TO AMT.
           GENERATE DE-1.
           TERMINATE R-1.
           INITIATE R-2.
           GENERATE DE-2.
           TERMINATE R-2.
           CLOSE PRT.
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
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC = SYM-FF
                   PERFORM SHOW-LINE
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
               DISPLAY "P" WS-PG " L" WS-LN " " WS-LINE(1:6)
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
