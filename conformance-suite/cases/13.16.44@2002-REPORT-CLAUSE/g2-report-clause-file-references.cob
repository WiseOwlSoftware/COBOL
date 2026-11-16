      *>--- conformance-case
      *> id: g2-report-clause-file-references
      *> rules: [2002-SR-13.16.44.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.44.2 SR3 - "The subject of a file description
      *> entry that specifies a REPORT clause may be referenced in the
      *> procedure division only by the USE statement, the CLOSE
      *> statement, or the OPEN statement with the OUTPUT or EXTEND
      *> phrase." The granted form: RPF is named by every permitted
      *> reference and by nothing else - USE AFTER STANDARD ERROR
      *> PROCEDURE ON RPF, OPEN OUTPUT RPF, CLOSE RPF, OPEN EXTEND RPF
      *> and CLOSE RPF again. The report is produced by INITIATE,
      *> GENERATE and TERMINATE, which name the report and its group,
      *> not the file. The program is accepted and runs each leg in
      *> turn; no I-O error occurs, so the declarative is never entered
      *> and "RPF ERROR" never appears. The case fails if any permitted
      *> reference is refused (each leg is a distinct statement form)
      *> or if a run-time error enters the declarative. The refused
      *> references (READ and other statements) are
      *> cases/13.4.5-File-description/pb1171-report-file-io-reference,
      *> refused at 2002 (COBOL2577).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPT02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "G2RPT02.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-TEXT PIC X(5) VALUE "FIRST".
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(5) SOURCE W-TEXT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RPF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPF.
       RPF-ERR-P.
           DISPLAY "RPF ERROR".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RPF
           DISPLAY "OPENED OUTPUT"
           INITIATE RPT
           GENERATE DL
           TERMINATE RPT
           CLOSE RPF
           DISPLAY "CLOSED"
           MOVE "AGAIN" TO W-TEXT
           OPEN EXTEND RPF
           DISPLAY "OPENED EXTEND"
           INITIATE RPT
           GENERATE DL
           TERMINATE RPT
           CLOSE RPF
           DISPLAY "CLOSED " W-TEXT
           STOP RUN.
