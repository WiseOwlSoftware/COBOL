      *>--- conformance-case
      *> id: g85_f01_02
      *> rules: [1985-GR-XIII.3.20.4-4, 1985-GR-XIII.3.20.4-6, 1985-GR-XIII.3.20.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR4 - CONTROL HEADING FINAL is processed during
      *> the first GENERATE (not at INITIATE); on a break the control
      *> headings of the break level and lower are processed: R1's CTL
      *> break processes CH CTL and then CH of the more minor CTLM, which
      *> never changes.
      *> §1985:XIII.3.20.4 GR6 - on a break the control footings of the
      *> break level or more minor are presented (CF CTLM, then CF CTL,
      *> on the CTL break); all control footings
      *> are presented at TERMINATE when a GENERATE was executed, CONTROL
      *> FOOTING FINAL once, as the last body group; none when no
      *> GENERATE was executed (report R2).
      *> §1985:XIII.3.20.4 GR8 - REPORT FOOTING is processed once, as the
      *> last group, during TERMINATE, and only if a GENERATE was executed
      *> for the report (R2 has none).
      *> No PAGE clause: each group is LINE PLUS 1 and ends on
      *> LINE-COUNTER + 1 (§1985:XIII.3.10.8 GR4 d, GR6 d). Each USE
      *> BEFORE REPORTING procedure runs just before its group is produced
      *> (§1985:XIII.4.9.4 GR4) and shows the LINE-COUNTER of its report.
      *> A compiler that processes CH FINAL at INITIATE, CF FINAL or RF
      *> before TERMINATE or twice, only the break level's CH or CF on a
      *> break, or any R2 group, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0102.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85F0102.TXT".
           SELECT RPT2-FILE ASSIGN TO "G85F0102B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS R1.
       FD  RPT2-FILE
           REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WS-LC                   PIC 99.
       01  CTL                     PIC X VALUE "A".
       01  CTLM                    PIC X VALUE "1".
       01  CTL2                    PIC X VALUE "A".
       REPORT SECTION.
       RD  R1
           CONTROLS ARE FINAL CTL CTLM.
       01  RH1 TYPE IS REPORT HEADING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RH".
       01  CHF TYPE IS CONTROL HEADING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CH FINAL".
       01  CHC TYPE IS CONTROL HEADING CTL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CH CTL".
           05  COLUMN NUMBER IS 8 PIC X SOURCE CTL.
       01  CHM TYPE IS CONTROL HEADING CTLM LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CH MIN".
       01  DE1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
           05  COLUMN NUMBER IS 4 PIC X SOURCE CTL.
       01  CFM TYPE IS CONTROL FOOTING CTLM LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CF MIN".
       01  CFC TYPE IS CONTROL FOOTING CTL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CF CTL".
       01  CFF TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CF FINAL".
       01  RF1 TYPE IS REPORT FOOTING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RF".
       RD  R2
           CONTROLS ARE FINAL CTL2.
       01  RH2 TYPE IS REPORT HEADING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RH".
       01  CHF2 TYPE IS CONTROL HEADING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CH FINAL".
       01  DE2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "DE".
       01  CFC2 TYPE IS CONTROL FOOTING CTL2 LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(6) VALUE "CF CTL".
       01  CFF2 TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(8) VALUE "CF FINAL".
       01  RF2 TYPE IS REPORT FOOTING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH1 SECTION.
           USE BEFORE REPORTING RH1.
       U-RH1-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 RH       " "LC " WS-LC.
       U-CHF SECTION.
           USE BEFORE REPORTING CHF.
       U-CHF-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CH FINAL " "LC " WS-LC.
       U-CHC SECTION.
           USE BEFORE REPORTING CHC.
       U-CHC-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CH CTL " CTL " LC " WS-LC.
       U-CHM SECTION.
           USE BEFORE REPORTING CHM.
       U-CHM-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CH MIN   " "LC " WS-LC.
       U-DE1 SECTION.
           USE BEFORE REPORTING DE1.
       U-DE1-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 DE " CTL " LC " WS-LC.
       U-CFM SECTION.
           USE BEFORE REPORTING CFM.
       U-CFM-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CF MIN   " "LC " WS-LC.
       U-CFC SECTION.
           USE BEFORE REPORTING CFC.
       U-CFC-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CF CTL   " "LC " WS-LC.
       U-CFF SECTION.
           USE BEFORE REPORTING CFF.
       U-CFF-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 CF FINAL " "LC " WS-LC.
       U-RF1 SECTION.
           USE BEFORE REPORTING RF1.
       U-RF1-P.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 RF       " "LC " WS-LC.
       U-RH2 SECTION.
           USE BEFORE REPORTING RH2.
       U-RH2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 RH       " "LC " WS-LC.
       U-CHF2 SECTION.
           USE BEFORE REPORTING CHF2.
       U-CHF2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 CH FINAL " "LC " WS-LC.
       U-DE2 SECTION.
           USE BEFORE REPORTING DE2.
       U-DE2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 DE       " "LC " WS-LC.
       U-CFC2 SECTION.
           USE BEFORE REPORTING CFC2.
       U-CFC2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 CF CTL   " "LC " WS-LC.
       U-CFF2 SECTION.
           USE BEFORE REPORTING CFF2.
       U-CFF2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 CF FINAL " "LC " WS-LC.
       U-RF2 SECTION.
           USE BEFORE REPORTING RF2.
       U-RF2-P.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 RF       " "LC " WS-LC.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT2-FILE.
           INITIATE R1.
           DISPLAY "R1 INITIATED".
           GENERATE DE1.
           GENERATE DE1.
           MOVE "B" TO CTL.
           GENERATE DE1.
           DISPLAY "R1 BEFORE TERMINATE".
           TERMINATE R1.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "R1 TERMINATED LC " WS-LC.
           INITIATE R2.
           TERMINATE R2.
           MOVE LINE-COUNTER OF R2 TO WS-LC.
           DISPLAY "R2 TERMINATED LC " WS-LC.
           CLOSE RPT-FILE RPT2-FILE.
           STOP RUN.
