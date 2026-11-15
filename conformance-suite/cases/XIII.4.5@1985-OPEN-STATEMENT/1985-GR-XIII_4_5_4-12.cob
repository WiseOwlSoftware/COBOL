      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-12
      *> rules: [1985-GR-XIII.4.5.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR12 (THE OPEN STATEMENT, Report Writer):
      *> "Treatment of a file contained in a multiple file tape
      *> environment is logically equivalent to the treatment of a
      *> sequential file contained in a single file tape environment."
      *> The Report Writer twin of 1985-GR-VII_4_3_4-19. MFT-RPT is a
      *> report file named in MULTIPLE FILE TAPE (POSITION 1); SFT-RPT
      *> is the same report file outside it. Each is opened OUTPUT and
      *> the same report is generated and terminated on it, and both
      *> are then read back as plain sequential files of one-character
      *> records. GR12 requires the two files to be equivalent: every
      *> character pair equal and both ending together ("RECORDS
      *> EQUAL"), with each report line present exactly once on the
      *> file's lines ("ALPHA 1", "BRAVO 1"). The test fails if the
      *> MULTIPLE FILE TAPE clause changes what the report wrote,
      *> or if either report line is lost or written twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFTRW12.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS LF-CH IS 11 CR-CH IS 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MFT-RPT ASSIGN TO MFTRW1.
           SELECT SFT-RPT ASSIGN TO SFTRW2.
           SELECT MFT-IN ASSIGN TO MFTRW1.
           SELECT SFT-IN ASSIGN TO SFTRW2.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS MFT-RPT POSITION 1.
       DATA DIVISION.
       FILE SECTION.
       FD  MFT-RPT LABEL RECORDS ARE STANDARD
           REPORT IS RPT-M.
       FD  SFT-RPT LABEL RECORDS ARE STANDARD
           REPORT IS RPT-S.
       FD  MFT-IN LABEL RECORDS ARE STANDARD.
       01  MFT-CH PIC X.
       FD  SFT-IN LABEL RECORDS ARE STANDARD.
       01  SFT-CH PIC X.
       WORKING-STORAGE SECTION.
       01  M-EOF PIC X VALUE "N".
       01  S-EOF PIC X VALUE "N".
       01  SAME-FLAG PIC X VALUE "Y".
       01  N-ALPHA PIC 9 VALUE 0.
       01  N-BRAVO PIC 9 VALUE 0.
       01  LINE-BUF PIC X(20) VALUE SPACES.
       01  LINE-POS PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT-M.
       01  DETL-MA TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "ALPHA".
       01  DETL-MB TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "BRAVO".
       RD  RPT-S.
       01  DETL-SA TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "ALPHA".
       01  DETL-SB TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "BRAVO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT MFT-RPT.
           INITIATE RPT-M.
           GENERATE DETL-MA.
           GENERATE DETL-MB.
           TERMINATE RPT-M.
           CLOSE MFT-RPT.
           OPEN OUTPUT SFT-RPT.
           INITIATE RPT-S.
           GENERATE DETL-SA.
           GENERATE DETL-SB.
           TERMINATE RPT-S.
           CLOSE SFT-RPT.
           OPEN INPUT MFT-IN SFT-IN.
           PERFORM COMPARE-PAIR UNTIL M-EOF = "Y" OR S-EOF = "Y".
           IF M-EOF NOT = S-EOF
               MOVE "N" TO SAME-FLAG.
           PERFORM TALLY-LINE.
           CLOSE MFT-IN SFT-IN.
           IF SAME-FLAG = "Y"
               DISPLAY "RECORDS EQUAL"
           ELSE
               DISPLAY "RECORDS DIFFER".
           DISPLAY "ALPHA " N-ALPHA.
           DISPLAY "BRAVO " N-BRAVO.
           STOP RUN.
       COMPARE-PAIR.
           READ MFT-IN AT END MOVE "Y" TO M-EOF.
           READ SFT-IN AT END MOVE "Y" TO S-EOF.
           IF M-EOF = "N" AND S-EOF = "N"
               IF MFT-CH NOT = SFT-CH
                   MOVE "N" TO SAME-FLAG
               END-IF
               IF MFT-CH = LF-CH
                   PERFORM TALLY-LINE
               ELSE
                   IF MFT-CH NOT = CR-CH AND LINE-POS < 20
                       ADD 1 TO LINE-POS
                       MOVE MFT-CH TO LINE-BUF (LINE-POS:1).
       TALLY-LINE.
           INSPECT LINE-BUF TALLYING N-ALPHA FOR ALL "ALPHA"
               N-BRAVO FOR ALL "BRAVO".
           MOVE SPACES TO LINE-BUF.
           MOVE 0 TO LINE-POS.
