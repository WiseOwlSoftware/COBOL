      *>--- conformance-case
      *> id: g85_bf26_09
      *> rules: [1985-GR-XIII.4.8.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR5 - which USE procedure runs for a report
      *> file. b: RPT-C is open OUTPUT, so its second OPEN OUTPUT (41,
      *> "An OPEN statement is attempted for a file in the open mode")
      *> runs USE-OUTPUT. c: OPEN EXTEND of the absent RPT-B (35) runs
      *> USE-EXTEND. a: RPT-A and RPT-D are named, so the same two
      *> failures on them run USE-FILE and neither mode procedure (the
      *> "except those files referenced by file-name-1" clause). A leg
      *> fails if the wrong procedure, or none, displays its name.
      *> WiseOwl COBOL continues after a critical 3x or 4x status
      *> (docs/conformance/3.5, kb/Work PB5591). RPT-C's report is
      *> initiated, generated and terminated between its two OPENs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2609.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-A ASSIGN TO "bf2609a.dat"
               FILE STATUS IS FS-A.
           SELECT RPT-B ASSIGN TO "bf2609b.dat"
               FILE STATUS IS FS-B.
           SELECT RPT-C ASSIGN TO "bf2609c.dat"
               FILE STATUS IS FS-C.
           SELECT RPT-D ASSIGN TO "bf2609d.dat"
               FILE STATUS IS FS-D.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-A
           REPORT IS REP-A.
       FD  RPT-B
           REPORT IS REP-B.
       FD  RPT-C
           REPORT IS REP-C.
       FD  RPT-D
           REPORT IS REP-D.
       WORKING-STORAGE SECTION.
       01  FS-A PIC XX VALUE "??".
       01  FS-B PIC XX VALUE "??".
       01  FS-C PIC XX VALUE "??".
       01  FS-D PIC XX VALUE "??".
       REPORT SECTION.
       RD  REP-A.
       01  DTL-A TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "AAAAA".
       RD  REP-B.
       01  DTL-B TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "BBBBB".
       RD  REP-C.
       01  DTL-C TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "CCCCC".
       RD  REP-D.
       01  DTL-D TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "DDDDD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FILE SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RPT-A RPT-D.
       D-FILE-1.
           DISPLAY "USE-FILE".
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-1.
           DISPLAY "USE-OUTPUT".
       D-EXT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       D-EXT-1.
           DISPLAY "USE-EXTEND".
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN EXTEND RPT-B.
           DISPLAY "OPEN-B " FS-B.
           OPEN EXTEND RPT-D.
           DISPLAY "OPEN-D " FS-D.
           OPEN OUTPUT RPT-C.
           DISPLAY "OPEN-C1 " FS-C.
           INITIATE REP-C.
           GENERATE DTL-C.
           TERMINATE REP-C.
           OPEN OUTPUT RPT-C.
           DISPLAY "OPEN-C2 " FS-C.
           CLOSE RPT-C.
           OPEN OUTPUT RPT-A.
           DISPLAY "OPEN-A1 " FS-A.
           OPEN OUTPUT RPT-A.
           DISPLAY "OPEN-A2 " FS-A.
           CLOSE RPT-A.
           STOP RUN.
