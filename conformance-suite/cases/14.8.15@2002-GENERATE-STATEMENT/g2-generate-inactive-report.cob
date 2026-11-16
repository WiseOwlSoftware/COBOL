      *>--- conformance-case
      *> id: g2-generate-inactive-report
      *> rules: [2002-GR-14.8.15.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.15.3 GR7 - "The report associated with data-name-1
      *>   or report-name-1 shall be in the active state. If it is not,
      *>   the EC-REPORT-INACTIVE exception condition is set to exist."
      *> Context: §2002:13.7.3 - "A report is in the active state after
      *>   the successful execution of an INITIATE statement ... and
      *>   before the successful execution of a TERMINATE statement ...
      *>   At any other time, it is in the inactive state."
      *>   §2002:14.5.12.1 - with checking enabled (>>TURN) the condition
      *>   is raised, the declarative runs, and EXCEPTION-STATUS names it
      *>   (§2002:15.28.2 RV1: 31 characters, bracketed below).
      *>   §2002:14.8.15.2 SR2 - report-name-1 needs a CONTROL clause,
      *>   so R1 has CONTROL IS FINAL.
      *>   §2002:14.8.31.3 GR2a - RESUME AT NEXT STATEMENT continues
      *>   after the GENERATE that raised it.
      *> DERIVATION of every .out line:
      *>   GENERATE DL before INITIATE: inactive -> DECL-1 [...], then G1.
      *>   GENERATE R1 before INITIATE: inactive -> DECL-2 [...], then G2.
      *>   INITIATE R1; GENERATE DL: active, no exception -> G3 K=2.
      *>   TERMINATE R1; GENERATE DL: inactive again -> DECL-3 [...], G4.
      *>   K=3 at the end.
      *> CAN FAIL: no exception for an inactive report (missing DECL
      *>   lines, K<3), an exception on the active GENERATE (G3 K=3), or
      *>   a different condition name in the status.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GEN07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "g2gen07.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  K PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL.
       01  DL TYPE DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(5) VALUE "LINE1".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       D1P.
           ADD 1 TO K
           DISPLAY "DECL-" K " [" FUNCTION EXCEPTION-STATUS "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-REPORT-INACTIVE CHECKING ON
           OPEN OUTPUT RPTF
           GENERATE DL
           DISPLAY "G1"
           GENERATE R1
           DISPLAY "G2"
           INITIATE R1
           GENERATE DL
           DISPLAY "G3 K=" K
           TERMINATE R1
           GENERATE DL
           DISPLAY "G4"
           CLOSE RPTF
           DISPLAY "K=" K
           STOP RUN.
