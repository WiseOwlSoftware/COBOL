      *>--- conformance-case
      *> id: g2sum01_sum_cd_addends_timing
      *> rules: [2002-GR-13.16.52.3-6, 2002-GR-13.16.52.3-7,
      *>    2002-GR-13.16.52.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.3 GR6, GR7, GR9 - SUM addends that are items of a
      *> record under a CD (SR4: identifier-1 "shall specify a numeric
      *> data item not defined in the report section"; SR5: an
      *> expression's identifiers "may reference entries in any section
      *> of the data division other than the report section"), and a
      *> rolled total over a SOURCE entry whose operand is a CD item.
      *> The communication facility is obsolete at 2002 (COBOL0903 is a
      *> warning and does not fail the case).
      *>
      *> DERIVATION (2002 text only).  WS-B = 1 throughout.  Three
      *> GENERATEs: DL with CD-N = 5, DL2 with CD-N = 7, DL with CD-N = 3.
      *> INITIATE zeroes every counter (GR2); CFT and RFT print once, at
      *> TERMINATE.
      *>  DL lines: DSRC SOURCE CD-N, DVAL VALUE 2   => 005 2, 003 2
      *>  TA SUM CD-N: GR7 c) 1 "whenever any GENERATE statement is
      *>     executed for the current report or any detail": 5+7+3 => 015
      *>  TB SUM CD-N UPON DL2: GR7 c) 2 "whenever any GENERATE statement
      *>     is executed for a detail referenced by the UPON phrase" => 007
      *>  TC SUM CD-N CD-N: GR7 "added into the sum counter as many times
      *>     as data-name-1 or identifier-1 is referenced": 2 x 15  => 030
      *>  TD SUM CD-N UPON DL2 DL2: GR7 "the adding takes place as many
      *>     times as data-name-2 appears in the UPON phrase": 2 x 7 => 014
      *>  TE SUM DSRC: GR7 a) adding "when the report group description
      *>     containing data-name-1 is processed" (DL only, not DL2);
      *>     GR6 "the value added is that of the operand of the SOURCE or
      *>     VALUE clause" (CD-N at that time): 5+3                 => 008
      *>  TF SUM DVAL: GR6, the VALUE operand 2, twice              => 004
      *>  TG SUM (CD-N + 1) CD-N WS-B: GR9 "the result is the same as when
      *>     all the addends were summed separately ... and the results
      *>     added together": (6+5+1) + (8+7+1) + (4+3+1)            => 036
      *>  TH SUM CD-N SUM WS-B UPON DL2: two SUM phrases, the UPON
      *>     belonging to the second only; GR9: 15 + 1                => 016
      *>  SZ (report footing) SUM TA: GR6 "the value added is that of the
      *>     corresponding sum counter", its own additions "completed
      *>     before"; GR7 a) added when CFT is processed, before GR2's
      *>     reset of TA at the end of CFT                            => 015
      *> Each leg fails on a counter that adds at the wrong GENERATE, adds
      *> a repeated operand or UPON detail once, takes a SOURCE entry's
      *> printed rather than operand value, or reads TA after its reset.
      *> cite.py --check: OK §2002:13.16.52.3 GR2, GR6, GR7, GR9;
      *>   §2002:13.16.52.2 SR4, SR5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum01.txt".
           SELECT CHK ASSIGN TO "g2sum01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-B    PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-REC.
           02  CD-PAD PIC X(84).
           02  CD-N   PIC 9(3).
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  DSRC COLUMN 1 PIC 999 SOURCE CD-N.
           05  DVAL COLUMN 5 PIC 9 VALUE 2.
       01  DL2 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "DL2".
       01  CFT TYPE CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               05  COLUMN 1  PIC X(2) VALUE "A=".
               05  TA COLUMN 3  PIC 999 SUM CD-N.
               05  COLUMN 7  PIC X(2) VALUE "B=".
               05  TB COLUMN 9  PIC 999 SUM CD-N UPON DL2.
               05  COLUMN 13 PIC X(2) VALUE "C=".
               05  TC COLUMN 15 PIC 999 SUM CD-N CD-N.
               05  COLUMN 19 PIC X(2) VALUE "D=".
               05  TD COLUMN 21 PIC 999 SUM CD-N UPON DL2 DL2.
           02  LINE PLUS 1.
               05  COLUMN 1  PIC X(2) VALUE "E=".
               05  TE COLUMN 3  PIC 999 SUM DSRC.
               05  COLUMN 7  PIC X(2) VALUE "F=".
               05  TF COLUMN 9  PIC 999 SUM DVAL.
               05  COLUMN 13 PIC X(2) VALUE "G=".
               05  TG COLUMN 15 PIC 999 SUM (CD-N + 1) CD-N WS-B.
               05  COLUMN 19 PIC X(2) VALUE "H=".
               05  TH COLUMN 21 PIC 999 SUM CD-N SUM WS-B UPON DL2.
       01  RFT TYPE REPORT FOOTING.
           02  LINE PLUS 1.
               05  COLUMN 1  PIC X(2) VALUE "Z=".
               05  SZ COLUMN 3  PIC 999 SUM TA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 5 TO CD-N.
           GENERATE DL.
           MOVE 7 TO CD-N.
           GENERATE DL2.
           MOVE 3 TO CD-N.
           GENERATE DL.
           TERMINATE R.
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
