*>--- conformance-case
*> id: pb5602-control-subscripted-2002
*> rules: [2002-SR-13.16.15.2-3, 2002-GR-13.16.15.3-3, 2002-SR-13.16.55.2-10, 2002-SR-13.16.55.2-11,
*>    2002-SR-13.16.55.2-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - at COBOL-2002 a CONTROL clause operand may be a subscripted data-name.
      *> §2002:13.16.15.2 SR3 says only "Data-name-1 may be qualified", and §2002:5.1.3 makes a
      *> data-name-n operand a "User-defined word, including qualification and subscription if
      *> needed"; a table element reference shall be subscripted (§2002:8.4.1.2.2 SR5). The
      *> prohibition arrived with COBOL-2014 (§2014:13.18.16.2 SR3 "Data-name-1 shall not be
      *> subscripted"). The subscripts are integer literals (docs/conformance, D-CLAUSE-SUBSCRIPT).
      *> cite.py: OK  §2002:13.16.15.2 SR3  - "Data-name-1 may be qualified."
      *>
      *> The report has the hierarchy FINAL, T-C(2, 3) (major), T-C(1, 2) (minor), over two
      *> different elements of one two-level table. §2002:13.16.55.2 SR10 makes each TYPE CONTROL
      *> FOOTING operand "the same as one of the operands of the CONTROL clause", and SR14 allows
      *> one control footing for each control data item or FINAL.
      *>
      *> Derived (§2002:13.16.15.3 GR3: the first GENERATE saves each control as its prior control;
      *> later ones compare in order major to minor, and a change at a level breaks that level and
      *> every lower one; a footing's SUM counts the detail amounts since its own last break):
      *>   G1 A X 1  first GENERATE: priors saved                      -> detail "A X 1"
      *>   G2 A Y 2  minor T-C(1, 2) X -> Y                            -> MN=1 , then "A Y 2"
      *>   G3 B Y 3  major T-C(2, 3) A -> B: minor and major break     -> MN=2 MJ=3 , then "B Y 3"
      *>   TERMINATE breaks every level: the minor and the major have each counted only G3 since
      *>   their last break (3), and FINAL has counted all three (1 + 2 + 3 = 6)
      *>                                                               -> MN=3 MJ=3 FN=6
      *> The legs fail separately: T-C(1, 2) and T-C(2, 3) are different elements, so a compiler
      *> that dropped the subscripts (both bound to the whole table, as before kb/Work PB5602 it
      *> refused them) could not tell G1 -> G2 (minor only) from G2 -> G3 (major).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5602-control-subscripted-2002.rpt".
           SELECT RBACK ASSIGN TO "pb5602-control-subscripted-2002.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       FD RBACK.
       01 RB-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 T-ROW OCCURS 2.
             10 T-C PIC X OCCURS 3.
       01 WS-B   PIC 9 VALUE 0.
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-N   PIC 99 VALUE 0.
       01 WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD R-1 CONTROLS ARE FINAL T-C(2, 3) T-C(1, 2).
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X SOURCE T-C(2, 3).
          05 COLUMN 3 PIC X SOURCE T-C(1, 2).
          05 COLUMN 5 PIC 9 SOURCE WS-B.
       01 CFA TYPE CONTROL FOOTING T-C(1, 2) LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "MN=".
          05 COLUMN 4 PIC 9 SUM WS-B.
       01 CFB TYPE CONTROL FOOTING T-C(2, 3) LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "MJ=".
          05 COLUMN 4 PIC 9 SUM WS-B.
       01 CFF TYPE CONTROL FOOTING FINAL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "FN=".
          05 COLUMN 4 PIC 9 SUM WS-B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT
           INITIATE R-1
           MOVE "A" TO T-C(2, 3)
           MOVE "X" TO T-C(1, 2)
           MOVE 1 TO WS-B
           GENERATE DL
           MOVE "A" TO T-C(2, 3)
           MOVE "Y" TO T-C(1, 2)
           MOVE 2 TO WS-B
           GENERATE DL
           MOVE "B" TO T-C(2, 3)
           MOVE "Y" TO T-C(1, 2)
           MOVE 3 TO WS-B
           GENERATE DL
           TERMINATE R-1
           CLOSE RPT
           OPEN INPUT RBACK
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE RBACK
           IF WS-N > 0 PERFORM SHOW-LINE END-IF
           STOP RUN.
       TAKE-BYTE.
           EVALUATE RB-REC
               WHEN X"0A" PERFORM SHOW-LINE
               WHEN X"0D" CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-N
                   MOVE RB-REC TO WS-LINE(WS-N:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES DISPLAY "L=" WS-LINE END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-N.
