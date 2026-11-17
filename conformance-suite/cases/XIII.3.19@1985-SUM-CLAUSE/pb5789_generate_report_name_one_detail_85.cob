      *>--- conformance-case
      *> id: pb5789_generate_report_name_one_detail_85
      *> rules: [1985-GR-XIII.3.19.4-11, 1985-GR-XIII.3.19.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5789. GENERATE report-name on a report with one
      *>   detail report group: subtotalled, the detail not presented
      *>   (§1985:XIII.3.20.4 GR11 d).
      *> DERIVATION. §1985:XIII.3.19.4 GR11: "The subtotalling that
      *>   occurs when a GENERATE report-name statement is executed is
      *>   discussed in the appropriate paragraph"; §1985:XIII.3.20.4
      *>   GR11 a: "The report writer control system performs any
      *>   subtotalling that has been designated for the detail report
      *>   group". SK SUM WS-A has no UPON, so it subtotals on every
      *>   GENERATE (§1985:XIII.3.19.4 GR9c): 1 + 2 = 3 before the
      *>   break on WS-KEY, "CF-K +003"; then 4, "CF-K +004" at
      *>   TERMINATE. SF SUM SK rolls each forward (GR9b): "CF-F +007".
      *>   A sum counter DISPLAYs with its operational sign (GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5789OD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5789OD.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-KEY PIC 9 VALUE 1.
       01 WS-A   PIC 99 VALUE 0.
       REPORT SECTION.
       RD R1 CONTROL IS FINAL WS-KEY.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC XX VALUE "DE".
       01 CFK TYPE CF WS-KEY LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-KEY.
          02 SK COLUMN 3 PIC 999 SUM WS-A.
       01 CFF TYPE CF FINAL LINE PLUS 1.
          02 SF COLUMN 3 PIC 999 SUM SK.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CK-USE SECTION.
           USE BEFORE REPORTING CFK.
       CK-USE-P.
           DISPLAY "CF-K " SK.
       CF-USE SECTION.
           USE BEFORE REPORTING CFF.
       CF-USE-P.
           DISPLAY "CF-F " SF.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           MOVE 1 TO WS-A.
           GENERATE R1.
           MOVE 2 TO WS-A.
           GENERATE R1.
           MOVE 2 TO WS-KEY.
           MOVE 4 TO WS-A.
           GENERATE R1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
