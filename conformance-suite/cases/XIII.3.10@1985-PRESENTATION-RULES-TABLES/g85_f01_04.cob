      *>--- conformance-case
      *> id: g85_f01_04
      *> rules: [1985-GR-XIII.3.10.6-4, 1985-GR-XIII.3.10.8-5, 1985-GR-XIII.3.10.9-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Next group rules of the presentation rules tables, each in a
      *> form the rule permits, and the LINE-COUNTER it leaves.
      *> Every report: PAGE 20, HEADING 1, FIRST DETAIL 5, LAST DETAIL 9,
      *> FOOTING 10. A USE BEFORE REPORTING procedure runs just before
      *> its group is produced (§1985:XIII.4.9.4 GR4) and shows the
      *> report's PAGE-COUNTER and LINE-COUNTER.
      *> §1985:XIII.3.10.6 GR4 a - RA's report heading LINE 1 NEXT GROUP
      *> 3 (3 > 1, 3 < FIRST DETAIL 5): LINE-COUNTER 3 (rule 5a), so the
      *> PLUS 1 page heading starts on line 4. GR4 b - RB's NEXT GROUP
      *> PLUS 2 (2 + 1 < 5): LINE-COUNTER 3 (rule 5b). GR4 c - RC's NEXT
      *> GROUP NEXT PAGE: the heading is alone on page 1; the page heading
      *> and the detail come on page 2, and no page footing on page 1.
      *> §1985:XIII.3.10.8 GR5 - RA's details carry absolute NEXT GROUP
      *> at both bounds: DE-A 10 (= FOOTING) and DE-B 5 (= FIRST DETAIL).
      *> §1985:XIII.3.10.9 GR4 a - RA's page footing LINE 11 NEXT GROUP 13
      *> (13 > 11, 13 <= 20): LINE-COUNTER 13, report footing PLUS 2 on
      *> 15. GR4 b - RB's NEXT GROUP PLUS 2 (2 + 11 <= 20): the same 13.
      *> A compiler that refuses a legal next group or sets another
      *> LINE-COUNTER fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0104.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "G85F0104A.TXT".
           SELECT FB ASSIGN TO "G85F0104B.TXT".
           SELECT FC ASSIGN TO "G85F0104C.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  FA
           REPORT IS RA.
       FD  FB
           REPORT IS RB.
       FD  FC
           REPORT IS RC.
       WORKING-STORAGE SECTION.
       01  WS-LC                   PIC 99.
       01  WS-PC                   PIC 9.
       REPORT SECTION.
       RD  RA
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 9
           FOOTING 10.
       01  RHA TYPE IS REPORT HEADING
           LINE NUMBER IS 1
           NEXT GROUP IS 3.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RH".
       01  PHA TYPE IS PAGE HEADING
           LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PH".
       01  DEA TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS 10.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DE-A".
       01  DEB TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS 5.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DE-B".
       01  PFA TYPE IS PAGE FOOTING
           LINE NUMBER IS 11
           NEXT GROUP IS 13.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PF".
       01  RFA TYPE IS REPORT FOOTING
           LINE NUMBER IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RF".
       RD  RB
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 9
           FOOTING 10.
       01  RHB TYPE IS REPORT HEADING
           LINE NUMBER IS 1
           NEXT GROUP IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RH".
       01  PHB TYPE IS PAGE HEADING
           LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PH".
       01  DEC TYPE IS DETAIL
           LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
       01  PFB TYPE IS PAGE FOOTING
           LINE NUMBER IS 11
           NEXT GROUP IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PF".
       01  RFB TYPE IS REPORT FOOTING
           LINE NUMBER IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RF".
       RD  RC
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 9
           FOOTING 10.
       01  RHC TYPE IS REPORT HEADING
           LINE NUMBER IS 1
           NEXT GROUP IS NEXT PAGE.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RH".
       01  PHC TYPE IS PAGE HEADING
           LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PH".
       01  DED TYPE IS DETAIL
           LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
       01  PFC TYPE IS PAGE FOOTING
           LINE NUMBER IS 11.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RHA SECTION.
           USE BEFORE REPORTING RHA.
       U-RHA-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA RH   PG " WS-PC " LC " WS-LC.
       U-PHA SECTION.
           USE BEFORE REPORTING PHA.
       U-PHA-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA PH   PG " WS-PC " LC " WS-LC.
       U-DEA SECTION.
           USE BEFORE REPORTING DEA.
       U-DEA-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA DE-A PG " WS-PC " LC " WS-LC.
       U-DEB SECTION.
           USE BEFORE REPORTING DEB.
       U-DEB-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA DE-B PG " WS-PC " LC " WS-LC.
       U-PFA SECTION.
           USE BEFORE REPORTING PFA.
       U-PFA-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA PF   PG " WS-PC " LC " WS-LC.
       U-RFA SECTION.
           USE BEFORE REPORTING RFA.
       U-RFA-P.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA RF   PG " WS-PC " LC " WS-LC.
       U-RHB SECTION.
           USE BEFORE REPORTING RHB.
       U-RHB-P.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB RH   PG " WS-PC " LC " WS-LC.
       U-PHB SECTION.
           USE BEFORE REPORTING PHB.
       U-PHB-P.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB PH   PG " WS-PC " LC " WS-LC.
       U-DEC SECTION.
           USE BEFORE REPORTING DEC.
       U-DEC-P.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB DE   PG " WS-PC " LC " WS-LC.
       U-PFB SECTION.
           USE BEFORE REPORTING PFB.
       U-PFB-P.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB PF   PG " WS-PC " LC " WS-LC.
       U-RFB SECTION.
           USE BEFORE REPORTING RFB.
       U-RFB-P.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB RF   PG " WS-PC " LC " WS-LC.
       U-RHC SECTION.
           USE BEFORE REPORTING RHC.
       U-RHC-P.
           MOVE PAGE-COUNTER OF RC TO WS-PC.
           MOVE LINE-COUNTER OF RC TO WS-LC.
           DISPLAY "RC RH   PG " WS-PC " LC " WS-LC.
       U-PHC SECTION.
           USE BEFORE REPORTING PHC.
       U-PHC-P.
           MOVE PAGE-COUNTER OF RC TO WS-PC.
           MOVE LINE-COUNTER OF RC TO WS-LC.
           DISPLAY "RC PH   PG " WS-PC " LC " WS-LC.
       U-DED SECTION.
           USE BEFORE REPORTING DED.
       U-DED-P.
           MOVE PAGE-COUNTER OF RC TO WS-PC.
           MOVE LINE-COUNTER OF RC TO WS-LC.
           DISPLAY "RC DE   PG " WS-PC " LC " WS-LC.
       U-PFC SECTION.
           USE BEFORE REPORTING PFC.
       U-PFC-P.
           MOVE PAGE-COUNTER OF RC TO WS-PC.
           MOVE LINE-COUNTER OF RC TO WS-LC.
           DISPLAY "RC PF   PG " WS-PC " LC " WS-LC.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT FA FB FC.
           INITIATE RA.
           GENERATE DEA.
           GENERATE DEA.
           GENERATE DEB.
           TERMINATE RA.
           MOVE PAGE-COUNTER OF RA TO WS-PC.
           MOVE LINE-COUNTER OF RA TO WS-LC.
           DISPLAY "RA END  PG " WS-PC " LC " WS-LC.
           INITIATE RB.
           GENERATE DEC.
           TERMINATE RB.
           MOVE PAGE-COUNTER OF RB TO WS-PC.
           MOVE LINE-COUNTER OF RB TO WS-LC.
           DISPLAY "RB END  PG " WS-PC " LC " WS-LC.
           INITIATE RC.
           GENERATE DED.
           TERMINATE RC.
           MOVE PAGE-COUNTER OF RC TO WS-PC.
           MOVE LINE-COUNTER OF RC TO WS-LC.
           DISPLAY "RC END  PG " WS-PC " LC " WS-LC.
           CLOSE FA FB FC.
           STOP RUN.
