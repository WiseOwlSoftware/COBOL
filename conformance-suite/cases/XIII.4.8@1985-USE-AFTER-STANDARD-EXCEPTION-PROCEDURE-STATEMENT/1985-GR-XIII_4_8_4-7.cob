      *>--- conformance-case
      *> id: 1985-GR-XIII_4_8_4-7
      *> rules: [1985-GR-XIII.4.8.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR7 (THE USE AFTER STANDARD EXCEPTION
      *> PROCEDURE STATEMENT). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USENEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-A ASSIGN TO "RWNONEA"
               FILE STATUS IS FS-A.
           SELECT RPT-B ASSIGN TO "RWNONEB"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-A
           REPORT IS REP-A.
       FD  RPT-B
           REPORT IS REP-B.
       WORKING-STORAGE SECTION.
       01  FS-A PIC XX.
       01  FS-B PIC XX.
       REPORT SECTION.
       RD  REP-A.
       01  DTL-A TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  REP-B.
       01  DTL-B TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-A SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT-A.
       ERR-A-PARA.
           DISPLAY "USE A " FS-A.
           OPEN EXTEND RPT-B.
       ERR-B SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT-B.
       ERR-B-PARA.
           DISPLAY "USE B " FS-B.
           STOP RUN.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           OPEN EXTEND RPT-A.
           STOP RUN.
