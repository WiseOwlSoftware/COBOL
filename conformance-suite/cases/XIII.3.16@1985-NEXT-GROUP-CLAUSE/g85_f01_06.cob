      *>--- conformance-case
      *> id: g85_f01_06
      *> rules: [1985-SR-XIII.3.16.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.16.3 SR3 - with the PAGE clause omitted a relative
      *> NEXT GROUP clause may be specified in a report group: here in the
      *> report heading (NEXT GROUP PLUS 2) and in the detail (NEXT GROUP
      *> PLUS 3). The program must translate and run at 85.
      *> Without a PAGE clause the report heading's final LINE-COUNTER is
      *> its last line plus the NEXT GROUP integer (§1985:XIII.3.10.6 GR5
      *> b) and a body group starts on LINE-COUNTER plus its LINE integer
      *> (§1985:XIII.3.10.8 GR4d and leaves its last line plus the NEXT
      *> GROUP integer (GR6 f). A USE BEFORE REPORTING procedure runs just
      *> before its group is produced (§1985:XIII.4.9.4 GR4) and shows
      *> LINE-COUNTER. A compiler that refuses the relative NEXT GROUP, or
      *> ignores it, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0106.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85F0106.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-LC                   PIC 99.
       REPORT SECTION.
       RD  R1.
       01  RH1 TYPE IS REPORT HEADING
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS PLUS 2.
           05  COLUMN NUMBER IS 1 PIC XX VALUE "RH".
       01  DE1 TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS PLUS 3.
           05  COLUMN NUMBER IS 1 PIC XX VALUE "DE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH1 SECTION.
           USE BEFORE REPORTING RH1.
       U-RH1-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "RH LC " WS-LC.
       U-DE1 SECTION.
           USE BEFORE REPORTING DE1.
       U-DE1-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "DE LC " WS-LC.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           GENERATE DE1.
           GENERATE DE1.
           TERMINATE R1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "END LC " WS-LC.
           CLOSE RPT-FILE.
           STOP RUN.
