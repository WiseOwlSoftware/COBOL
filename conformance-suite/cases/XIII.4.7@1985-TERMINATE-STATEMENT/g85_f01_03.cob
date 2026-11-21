      *>--- conformance-case
      *> id: g85_f01_03
      *> rules: [1985-GR-XIII.4.7.4-2, 1985-GR-XIII.4.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.7.4 GR3 - when the RWCS must advance to a new page
      *> to present a body group it processes the page footing and page
      *> heading: during GENERATE (the 4th detail) and during TERMINATE
      *> (CF FINAL, LINE PLUS 2, does not fit above FOOTING 5).
      *> §1985:XIII.4.7.4 GR2 - R2 is initiated and terminated with no
      *> GENERATE: TERMINATE produces no group and does no related
      *> processing (no USE procedure runs, no page advance).
      *> PAGE 6, HEADING 1, FIRST DETAIL 2, LAST DETAIL 4, FOOTING 5.
      *> Detail fit: trial sum LINE-COUNTER + 1 must not exceed LAST
      *> DETAIL 4; CF FINAL: LINE-COUNTER + 2 must not exceed FOOTING 5
      *> (§1985:XIII.3.10.8 GR3b. A USE BEFORE REPORTING procedure runs
      *> before its group is produced (§1985:XIII.4.9.4 GR4); PAGE-COUNTER
      *> steps and LINE-COUNTER is reset to zero on each page advance.
      *> A compiler that skips PF/PH on either advance, or produces any R2
      *> group, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0103.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85F0103.TXT".
           SELECT RPT2-FILE ASSIGN TO "G85F0103B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS R1.
       FD  RPT2-FILE
           REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WS-LC                   PIC 99.
       01  WS-PC                   PIC 9.
       REPORT SECTION.
       RD  R1
           CONTROL IS FINAL
           PAGE LIMIT IS 6 LINES
           HEADING 1
           FIRST DETAIL 2
           LAST DETAIL 4
           FOOTING 5.
       01  PH1 TYPE IS PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PH".
       01  DE1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
       01  CFF TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CF FINAL".
       01  PF1 TYPE IS PAGE FOOTING LINE NUMBER IS 6.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PF".
       RD  R2
           CONTROL IS FINAL
           PAGE LIMIT IS 6 LINES
           HEADING 1
           FIRST DETAIL 2
           LAST DETAIL 4
           FOOTING 5.
       01  PH2 TYPE IS PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PH".
       01  DE2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
       01  CFF2 TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CF FINAL".
       01  PF2 TYPE IS PAGE FOOTING LINE NUMBER IS 6.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-PH1 SECTION.
           USE BEFORE REPORTING PH1.
       U-PH1-P.
           MOVE PAGE-COUNTER OF R1 TO WS-PC.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 PH       " "PG " WS-PC " LC " WS-LC.
       U-DE1 SECTION.
           USE BEFORE REPORTING DE1.
       U-DE1-P.
           MOVE PAGE-COUNTER OF R1 TO WS-PC.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 DE       " "PG " WS-PC " LC " WS-LC.
       U-CFF SECTION.
           USE BEFORE REPORTING CFF.
       U-CFF-P.
           MOVE PAGE-COUNTER OF R1 TO WS-PC.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CF FINAL " "PG " WS-PC " LC " WS-LC.
       U-PF1 SECTION.
           USE BEFORE REPORTING PF1.
       U-PF1-P.
           MOVE PAGE-COUNTER OF R1 TO WS-PC.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 PF       " "PG " WS-PC " LC " WS-LC.
       U-PH2 SECTION.
           USE BEFORE REPORTING PH2.
       U-PH2-P.
           MOVE PAGE-COUNTER OF R2 TO WS-PC.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 PH       " "PG " WS-PC " LC " WS-LC.
       U-DE2 SECTION.
           USE BEFORE REPORTING DE2.
       U-DE2-P.
           MOVE PAGE-COUNTER OF R2 TO WS-PC.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 DE       " "PG " WS-PC " LC " WS-LC.
       U-CFF2 SECTION.
           USE BEFORE REPORTING CFF2.
       U-CFF2-P.
           MOVE PAGE-COUNTER OF R2 TO WS-PC.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 CF FINAL " "PG " WS-PC " LC " WS-LC.
       U-PF2 SECTION.
           USE BEFORE REPORTING PF2.
       U-PF2-P.
           MOVE PAGE-COUNTER OF R2 TO WS-PC.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 PF       " "PG " WS-PC " LC " WS-LC.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT2-FILE.
           INITIATE R1.
           PERFORM 6 TIMES
               GENERATE DE1
           END-PERFORM.
           DISPLAY "R1 BEFORE TERMINATE".
           TERMINATE R1.
           MOVE PAGE-COUNTER OF R1 TO WS-PC.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 TERMINATED PG " WS-PC " LC " WS-LC.
           INITIATE R2.
           TERMINATE R2.
           MOVE PAGE-COUNTER OF R2 TO WS-PC.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 TERMINATED PG " WS-PC " LC " WS-LC.
           CLOSE RPT-FILE RPT2-FILE.
           STOP RUN.
