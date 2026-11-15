      *>--- conformance-case
      *> id: g2acc02_report_file_random
      *> rules: [2002-SR-12.3.4.4.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.4.2 SR2 - the report-file arm: "The DYNAMIC and
      *> RANDOM phrases shall not be specified for a sequential file."
      *>   cite.py --check "§2002:12.3.4.4.2 SR2" "The DYNAMIC and
      *>   RANDOM phrases shall not be specified for a sequential file"
      *>   -> OK
      *> §2002:9.1.19: "A report file is an output file having
      *> sequential organization whose file description entry contains
      *> a REPORT clause."  -> cite.py OK §2002:9.1.19
      *> RPTF's FD carries REPORT IS RPT, so RPTF is a sequential file
      *> and its ACCESS MODE IS RANDOM violates SR2. The program is
      *> otherwise legal 2002 report writer source (the same program
      *> with ACCESS MODE IS SEQUENTIAL compiles clean at --std 2002),
      *> so the one diagnosis is SR2. The sequential-file arms are the
      *> pb692-access-* refusals, which refuse at 2002 too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACC02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "G2ACC02.RPT"
               ACCESS MODE IS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD  RPTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 10 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPTF.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPTF.
           STOP RUN.
