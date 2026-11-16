      *>--- conformance-case
      *> id: pb5801-use-refmod-group-of-control-in-footing
      *> rules: [SR-14.9.49.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801 review R1-1 - §1985:XIII.3.20.3 SR5: "In control
      *>   footing, page heading, page footing, and report footing report
      *>   groups, SOURCE clauses and associated USE statements must not
      *>   reference any of the following: a. Group data items containing a
      *>   control data item." The control footing's USE BEFORE REPORTING
      *>   procedure DISPLAYs WS-REC (1:2): the statement references the group
      *>   WS-REC, which contains the control WS-KEY, through a reference
      *>   modifier. Refused at 85 (COBOL6825); a compiler that asks only of a
      *>   whole item accepts it. pb5424-use-group-of-control-in-footing is the
      *>   undecorated twin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801GF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO P5801GF.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY PIC X VALUE "A".
           02  WS-OTHER PIC X VALUE "B".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  CF-G TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-USE SECTION.
           USE BEFORE REPORTING CF-G.
       CF-USE-P.
           DISPLAY "USE " WS-REC (1:2).
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
