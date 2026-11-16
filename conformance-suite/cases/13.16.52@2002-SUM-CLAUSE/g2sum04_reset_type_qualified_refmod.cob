      *>--- conformance-case
      *> id: g2sum04_reset_type_qualified_refmod
      *> rules: [2002-SR-13.16.52.2-7, 2002-SR-13.16.55.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.2 SR7 - "Data-name-3 may be qualified and
      *>   reference modified" (cite.py --check OK): SUM ... RESET ON
      *>   CX OF GRP(3:2), the INTERMEDIATE of three CONTROL operands
      *>   on the same base item, in the minor footing (a lower level).
      *> §2002:13.16.55.2 SR10 - "Data-name-1 and data-name-2 may be
      *>   qualified and reference modified" (cite.py --check OK):
      *>   TYPE CH CX OF GRP(1:2) / CX OF GRP(5:2) (data-name-1) and
      *>   TYPE CF CX OF GRP(5:2) / (3:2) / (1:2) (data-name-2), each
      *>   the same as one CONTROL operand.
      *> CX is declared twice (GRP and OTHER-GRP), so the qualifier is
      *> required. The three operands share one base item, so only the
      *> reference modification tells them apart: a RESET ON bound by
      *> name to the first CX OF GRP operand (the major) would not reset
      *> RUN at the intermediate break G3 and prints RUN=042 at G5.
      *> DERIVATION (2002 text only). Controls major (1:2), intermediate
      *> (3:2), minor (5:2) (§2002:13.16.15.3 GR1). Footings print
      *> "preceding any control heading and detail groups, in order of
      *> controls from the lowest up to the level of the control break"
      *> and at TERMINATE "from lowest to highest" (§2002:13.16.55.3
      *> GR6e); headings at the first GENERATE "in order of control
      *> levels from highest to lowest" and at a break "from the level
      *> of the control break down to the lowest" (GR6c). In a footing
      *> CX holds its prior value (§2002:13.16.15.3 GR4). Each GENERATE
      *> adds WS-AMT into every counter (§2002:13.16.52.3 GR7c) after
      *> the break processing (§2002:14.8.15.3 GR5). A counter resets
      *> at the end of its own group; RUN, with RESET, "at the end of
      *> the processing of the control footing for the specified level
      *> of control" (§2002:13.16.52.3 GR2), the intermediate.
      *>  G1 AA1111 10  HMJ, HMN; AMT=10
      *>  G2 AA1122 20  minor: MIN=010 RUN=010 K=AA1111 (MIN reset,
      *>                RUN kept); HMN=AA1122; AMT=20
      *>  G3 AA2211 05  intermediate: MIN=020 RUN=030 K=AA1122,
      *>                MID=030 K=AA1122 (RUN reset here); HMN; AMT=05
      *>  G4 AA2211 07  no break; AMT=07
      *>  G5 BB1111 30  major: MIN=012 RUN=012, MID=012, MAJ=042, all
      *>                K=AA2211; HMJ=BB1111, HMN=BB1111; AMT=30
      *>  G6 BB1111 40  AMT=40
      *>  TERMINATE     MIN=070 RUN=070, MID=070, MAJ=070, K=BB1111
      *> Why it can fail: a qualified or reference-modified RESET ON,
      *> TYPE CH or TYPE CF operand refused at 2002; a reference
      *> modification dropped (RUN reset at the major: RUN=042 at G5;
      *> or a footing at the wrong breaks); OTHER-GRP's CX bound (no
      *> breaks at all).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2sum04.txt".
           SELECT CHK ASSIGN TO "g2sum04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  GRP.
           05 CX      PIC X(6) VALUE SPACES.
       01  OTHER-GRP.
           05 CX      PIC X(6) VALUE "ZZZZZZ".
       01  WS-AMT  PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE CX OF GRP(1:2) CX OF GRP(3:2)
                            CX OF GRP(5:2).
       01  TYPE CH CX OF GRP(1:2) LINE PLUS 1.
           02 COLUMN 1  PIC X(4) VALUE "HMJ=".
           02 COLUMN 5  PIC X(6) SOURCE CX OF GRP.
       01  TYPE CH CX OF GRP(5:2) LINE PLUS 1.
           02 COLUMN 1  PIC X(4) VALUE "HMN=".
           02 COLUMN 5  PIC X(6) SOURCE CX OF GRP.
       01  DET-A TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC X(4) VALUE "AMT=".
           02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       01  TYPE CF CX OF GRP(5:2) LINE PLUS 1.
           02 COLUMN 1  PIC X(4) VALUE "MIN=".
           02 COLUMN 5  PIC 999 SUM WS-AMT.
           02 COLUMN 9  PIC X(4) VALUE "RUN=".
           02 COLUMN 13 PIC 999 SUM WS-AMT RESET ON CX OF GRP(3:2).
           02 COLUMN 17 PIC X(2) VALUE "K=".
           02 COLUMN 19 PIC X(6) SOURCE CX OF GRP.
       01  TYPE CF CX OF GRP(3:2) LINE PLUS 1.
           02 COLUMN 1  PIC X(4) VALUE "MID=".
           02 COLUMN 5  PIC 999 SUM WS-AMT.
           02 COLUMN 9  PIC X(2) VALUE "K=".
           02 COLUMN 11 PIC X(6) SOURCE CX OF GRP.
       01  TYPE CF CX OF GRP(1:2) LINE PLUS 1.
           02 COLUMN 1  PIC X(4) VALUE "MAJ=".
           02 COLUMN 5  PIC 999 SUM WS-AMT.
           02 COLUMN 9  PIC X(2) VALUE "K=".
           02 COLUMN 11 PIC X(6) SOURCE CX OF GRP.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT
           INITIATE R-1
           MOVE "AA1111" TO CX OF GRP MOVE 10 TO WS-AMT GENERATE DET-A
           MOVE "AA1122" TO CX OF GRP MOVE 20 TO WS-AMT GENERATE DET-A
           MOVE "AA2211" TO CX OF GRP MOVE 5 TO WS-AMT GENERATE DET-A
           MOVE "AA2211" TO CX OF GRP MOVE 7 TO WS-AMT GENERATE DET-A
           MOVE "BB1111" TO CX OF GRP MOVE 30 TO WS-AMT GENERATE DET-A
           MOVE "BB1111" TO CX OF GRP MOVE 40 TO WS-AMT GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
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
           END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
