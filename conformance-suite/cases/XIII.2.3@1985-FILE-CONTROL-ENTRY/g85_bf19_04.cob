      *>--- conformance-case
      *> id: g85_bf19_04
      *> rules: [1985-GR-XIII.2.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.3.4 GR2 - OPTIONAL applies only to a report file
      *>   opened in the extend mode; required for a report file not
      *>   necessarily present.
      *>   cite.py: OK §1985:XIII.2.3.4 GR2 "The OPTIONAL phrase applies
      *>   only to a report file opened in the extend mode".
      *> A report file is sequential (§1985:XIII.2.3.4 GR4 "A report file
      *>   has sequential organization"), so its I-O status values are the
      *>   Sequential I-O ones.
      *> Derivation (each line of the .out):
      *>   OPEN EXTEND of the absent OPTIONAL RPT-EX: OPEN Table 1
      *>   (§1985:XIII.4.5.4 GR1) EXTEND (optional file)/unavailable "Open
      *>   causes the file to be created"; status 05, §1985:VII.1.3.5
      *>   GRL2.1 "An OPEN statement is successfully executed but the
      *>   referenced optional file is not present".
      *>   §1985:XIII.4.5.4 GR14 "the successful execution of an OPEN
      *>   statement with an EXTEND phrase creates the file", so the
      *>   second OPEN EXTEND finds it available: Normal open, 00.
      *>   OPEN OUTPUT of the absent OPTIONAL RPT-OU: OPTIONAL does not
      *>   apply (GR2); Table 1 OUTPUT/unavailable creates the file: 00.
      *>   OPEN EXTEND of the absent non-optional RPT-RQ: Table 1 "Open is
      *>   unsuccessful", 35, §1985:VII.1.3.5 GRL2.3 "is attempted on a
      *>   non-optional file that is not present".
      *> Why each leg can fail: ignoring OPTIONAL gives 35 on the first
      *>   OPEN; not creating the file gives 35 on the reopen; applying
      *>   OPTIONAL to OUTPUT gives 05 there; treating RPT-RQ as optional
      *>   gives 05. RPT-RQ's 35 is critical: what follows its USE
      *>   procedure is the implementor's, so nothing is DISPLAYed after.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1904.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL RPT-EX
               ASSIGN TO "BF1904E.DAT"
               FILE STATUS IS ST-EX.
           SELECT OPTIONAL RPT-OU
               ASSIGN TO "BF1904O.DAT"
               FILE STATUS IS ST-OU.
           SELECT RPT-RQ
               ASSIGN TO "BF1904R.DAT"
               FILE STATUS IS ST-RQ.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-EX
           REPORT IS REP-EX.
       FD  RPT-OU
           REPORT IS REP-OU.
       FD  RPT-RQ
           REPORT IS REP-RQ.
       WORKING-STORAGE SECTION.
       01  ST-EX PIC XX VALUE "ZZ".
       01  ST-OU PIC XX VALUE "ZZ".
       01  ST-RQ PIC XX VALUE "ZZ".
       REPORT SECTION.
       RD  REP-EX.
       01  DL-EX TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "EX01".
       RD  REP-OU.
       01  DL-OU TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "OU01".
       RD  REP-RQ.
       01  DL-RQ TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "RQ01".
       PROCEDURE DIVISION.
       DECLARATIVES.
       REQ-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RPT-RQ.
       REQ-ERR-PARA.
           DISPLAY "REQ OPEN EXTEND " ST-RQ.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
      * EXTEND on an absent OPTIONAL report file: 05, created.
           OPEN EXTEND RPT-EX.
           DISPLAY "RPT OPEN EXTEND " ST-EX.
           INITIATE REP-EX.
           GENERATE DL-EX.
           TERMINATE REP-EX.
           CLOSE RPT-EX.
      * The file now exists: a second OPEN EXTEND is a normal open.
           OPEN EXTEND RPT-EX.
           DISPLAY "RPT EXTEND REOPEN " ST-EX.
           CLOSE RPT-EX.
      * OUTPUT: the OPTIONAL phrase does not apply; normal create.
           OPEN OUTPUT RPT-OU.
           DISPLAY "RPT OPEN OUTPUT " ST-OU.
           CLOSE RPT-OU.
      * A report file not declared OPTIONAL and not present: 35.
           OPEN EXTEND RPT-RQ.
           STOP RUN.
