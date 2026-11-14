      *>--- conformance-case
      *> id: pb5242_report_description_2002_forms
      *> rules: [SR-13.18.39.3-1, GR-13.18.39.4-2, FMT-13.18.39.2,
      *>    FMT-13.18.12.2, GR-13.18.12.4-1, SR-13.18.63.3-17]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - the 2002 forms of the report description that
      *> COBOL-85 does not have, written together and run: CODE IS, the
      *> page width, IS before a PAGE phrase's integer, the DE and CH
      *> abbreviations, the LAST CONTROL HEADING phrase and the VALUES
      *> spelling. Each is refused at --std 85 (COBOL0900; the refusals
      *> pb5242-*-85). RULES (cite.py --check, each OK):
      *> 13.18.39.3 SR1: "FIRST DE is synonymous with FIRST DETAIL, LAST
      *>   CH is synonymous with LAST CONTROL HEADING".
      *> 13.18.39.4 GR2 b): "Integer-2 is the page width."
      *> 13.18.12.4 GR1: "literal-1 or identifier-1 is automatically
      *>   placed in the first characters of each logical record written
      *>   to the report file for this report."
      *> 13.18.63.3 SR17: "The words VALUE and VALUES are equivalent."
      *> DERIVATION. The page is lines 1-6: HEADING 1, FIRST DETAIL 2,
      *> LAST DETAIL 4, FOOTING 5. The page heading prints on line 1, so
      *> the three details print on lines 2, 3 and 4, each showing the
      *> LINE-COUNTER of its line. The fourth detail would start on line
      *> 5, below LAST DETAIL (13.18.39.4 GR2 f)), so the page advances:
      *> the page footing prints on its absolute line 6; page 2 opens
      *> with the page heading (PAGE-COUNTER 2), the detail on line 2.
      *> TERMINATE prints the page footing of page 2. Every printed line
      *> starts with the code AB (GR1); line 5 has no report line. Each
      *> leg fails if a form is misread: DE or CH read as another phrase
      *> moves the details, a lost width or IS shifts nothing but a lost
      *> CODE drops AB, and VALUES ARE not taken as VALUE prints no PH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242p.txt".
           SELECT CHK ASSIGN TO "pb5242p.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-A.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-A CODE IS "AB"
           PAGE LIMIT IS 6 LINES 12 COLUMNS HEADING IS 1
           FIRST DE 2 LAST CH 3 LAST DE IS 4 FOOTING 5.
       01  PH-G TYPE PH LINE 1.
           02  COLUMN 1 PIC XX VALUES ARE "PH".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "D".
           02  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  PF-G TYPE PF LINE 6.
           02  COLUMN 1 PIC XX VALUE "PF".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-A.
           GENERATE DE-A.
           GENERATE DE-A.
           GENERATE DE-A.
           GENERATE DE-A.
           TERMINATE R-A.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN CHK-REC = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "---- PAGE ----"
                   MOVE 0 TO WS-LN
               WHEN CHK-REC = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:6) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
