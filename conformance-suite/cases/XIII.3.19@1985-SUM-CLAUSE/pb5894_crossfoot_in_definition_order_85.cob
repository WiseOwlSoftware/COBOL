      *>--- conformance-case
      *> id: pb5894_crossfoot_in_definition_order_85
      *> rules: [1985-GR-XIII.3.19.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5894. 1985:XIII.3.19.4 GR9a: "Crossfooting is
      *> performed according to the sequence in which sum counters are
      *> defined within the control footing report group. That is, all
      *> crossfooting into the first sum counter defined in the control
      *> footing report group is completed, and then all crossfooting
      *> into the second sum counter defined in the control footing
      *> report group is completed." XIII.3.20.4 GR10 a) crossfoots,
      *> b) rolls forward, c) runs the USE procedure.
      *> DERIVATION. Each footing subtotals WS-A (1 + 2 = 3, then 4)
      *> into SC and T1 (GR9c). Crossfooting then runs in definition
      *> order, each addend read as it stands at that moment:
      *> SA = SB (still 0) + SC = 3; SB = SC = 3; T3 = T2 (still 0)
      *> = 0; T2 = T1 = 3; T4 = T1 = 3; W, several addends defined
      *> earlier in mixed order, = T1 + SB + SC + T3 = 3+3+3+0 = 9.
      *> The second footing: 4 4 4 0 4 4 4 12. Rolling forward into
      *> the FINAL footing reads each counter after its crossfoot:
      *> GA = 3 + 4 = 7, GT = 0 + 0 = 0. A sum counter has an
      *> operational sign (GR1), so DISPLAY shows it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5894A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5894A.OUT".
       DATA DIVISION.
       FILE SECTION.
       FD  PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9  VALUE 1.
       01  WS-A   PIC 99 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL WS-KEY.
       01  D1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE WS-A.
       01  CFK TYPE CF WS-KEY LINE PLUS 1.
           02  SA COLUMN 1  PIC 999 SUM SB SC.
           02  SB COLUMN 5  PIC 999 SUM SC.
           02  SC COLUMN 9  PIC 999 SUM WS-A.
           02  T3 COLUMN 13 PIC 999 SUM T2.
           02  T1 COLUMN 17 PIC 999 SUM WS-A.
           02  T2 COLUMN 21 PIC 999 SUM T1.
           02  T4 COLUMN 25 PIC 999 SUM T1.
           02  W  COLUMN 29 PIC 999 SUM T1 SB SC T3.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           02  GA COLUMN 1  PIC 999 SUM SA.
           02  GT COLUMN 5  PIC 999 SUM T3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CFK-USE SECTION.
           USE BEFORE REPORTING CFK.
       CFK-USE-P.
           DISPLAY "CF " SA " " SB " " SC " " T3 " " T1 " " T2 " "
               T4 " " W.
       CFF-USE SECTION.
           USE BEFORE REPORTING CFF.
       CFF-USE-P.
           DISPLAY "FINAL " GA " " GT.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           MOVE 1 TO WS-A. GENERATE D1.
           MOVE 2 TO WS-A. GENERATE D1.
           MOVE 2 TO WS-KEY.
           MOVE 4 TO WS-A. GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
