      *>--- conformance-case
      *> id: pb5128_footing_page_groups_control_values
      *> rules: [GR-13.18.16.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> COBOL-85 is not an edition of this program: its page heading and page footing USE procedure references the control data-name
      *> WS-KEY, and "In page heading and page footing report groups, SOURCE clauses and USE statements must not
      *> reference control data-names" (§1985:XIII.3.20.3 SR5; refused COBOL6825 at 85 since kb/Work PB5424).
      *> kb/Work PB5128 - which control values the page footing and page heading USE procedures see when a control
      *> footing's page advance reaches them.
      *>
      *> §1985:XIII.3.20.4 GR13 (cite.py --check): a. "During control break processing of a control footing report
      *> group, any references to control data items in a USE procedure or SOURCE clause associated with that control
      *> footing report group are supplied with prior values." c. "All other data item references within report
      *> groups and their USE procedures access the current values that are contained within the data items at the
      *> time the report group is processed."  So at COBOL-85 the page footing and page heading of the advance see
      *> the CURRENT values (B and 3).  §2023:13.18.16.4 GR4 a) (and the 2002 text, 13.16.15.3 GR4 a): "If the
      *> printing of the control footing causes a page advance and the page heading or page footing refers to a
      *> control data item, the value produced is similarly the prior value" (A and 2).
      *>
      *> Derivation. PAGE LIMIT 12, LAST DETAIL 9, FOOTING 9.  Page 1: PH on line 1, CH-MAJ line 2, CH-MIN line 3,
      *> DE lines 4 and 5.  GENERATE 3 (minor break, WS-MIN 2) prints CF-MIN (6), CH-MIN (7), DE (8).  GENERATE 4
      *> breaks WS-MAJ: CF-MIN prints at line 9 and CF-MAJ does not fit above LAST DETAIL 9, so the page advances:
      *> page footing, then page heading.  Their USE procedures run with prior (2002 and later) A 2 or current (85)
      *> B 3.  CH-MAJ and CH-MIN of the new page are current at every edition (B 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5128PGVAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "GENBRK.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS BRK-RPT.
       WORKING-STORAGE SECTION.
       01  WS-MAJ    PIC X VALUE SPACE.
       01  WS-MIN    PIC 9 VALUE 0.
       01  WS-QUIET  PIC X VALUE "N".
       REPORT SECTION.
       RD  BRK-RPT CONTROLS ARE WS-MAJ WS-MIN
           PAGE LIMIT IS 12 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 9 FOOTING 9.
       01  PH-GRP TYPE PAGE HEADING LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  CH-MAJ TYPE CONTROL HEADING WS-MAJ LINE PLUS 1.
           02  COLUMN 1 PIC X(6) VALUE "CH-MAJ".
       01  CH-MIN TYPE CONTROL HEADING WS-MIN LINE PLUS 1.
           02  COLUMN 1 PIC X(6) VALUE "CH-MIN".
       01  DE-GRP TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  CF-MIN TYPE CONTROL FOOTING WS-MIN LINE PLUS 1.
           02  COLUMN 1 PIC X(6) VALUE "CF-MIN".
       01  CF-MAJ TYPE CONTROL FOOTING WS-MAJ LINE PLUS 1.
           02  COLUMN 1 PIC X(6) VALUE "CF-MAJ".
       01  PF-GRP TYPE PAGE FOOTING LINE 11.
           02  COLUMN 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-PH SECTION.
           USE BEFORE REPORTING PH-GRP.
       U-PH-1.
           IF WS-QUIET = "N"
               DISPLAY "PH " WS-MAJ " " WS-MIN.
       U-CH-MAJ SECTION.
           USE BEFORE REPORTING CH-MAJ.
       U-CH-MAJ-1.
           IF WS-QUIET = "N"
               DISPLAY "CH-MAJ " WS-MAJ " " WS-MIN.
       U-CH-MIN SECTION.
           USE BEFORE REPORTING CH-MIN.
       U-CH-MIN-1.
           IF WS-QUIET = "N"
               DISPLAY "CH-MIN " WS-MAJ " " WS-MIN.
       U-DE SECTION.
           USE BEFORE REPORTING DE-GRP.
       U-DE-1.
           IF WS-QUIET = "N"
               DISPLAY "DE " WS-MAJ " " WS-MIN.
       U-CF-MIN SECTION.
           USE BEFORE REPORTING CF-MIN.
       U-CF-MIN-1.
           IF WS-QUIET = "N"
               DISPLAY "CF-MIN " WS-MAJ " " WS-MIN.
       U-CF-MAJ SECTION.
           USE BEFORE REPORTING CF-MAJ.
       U-CF-MAJ-1.
           IF WS-QUIET = "N"
               DISPLAY "CF-MAJ " WS-MAJ " " WS-MIN.
       U-PF SECTION.
           USE BEFORE REPORTING PF-GRP.
       U-PF-1.
           IF WS-QUIET = "N"
               DISPLAY "PF " WS-MAJ " " WS-MIN.
       END DECLARATIVES.
       MAIN-PROC SECTION.
       MAIN-1.
           OPEN OUTPUT RPT-FILE.
           INITIATE BRK-RPT.
           DISPLAY "GENERATE 1".
           MOVE "A" TO WS-MAJ.
           MOVE 1 TO WS-MIN.
           GENERATE DE-GRP.
           DISPLAY "GENERATE 2".
           GENERATE DE-GRP.
           DISPLAY "GENERATE 3".
           MOVE 2 TO WS-MIN.
           GENERATE DE-GRP.
           DISPLAY "GENERATE 4".
           MOVE "B" TO WS-MAJ.
           MOVE 3 TO WS-MIN.
           GENERATE DE-GRP.
           MOVE "Y" TO WS-QUIET.
           TERMINATE BRK-RPT.
           CLOSE RPT-FILE.
           STOP RUN.
