      *>--- conformance-case
      *> id: g2-report-clause-one-each
      *> rules: [2002-SR-13.16.44.2-2, 2002-GR-13.16.44.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.44.2 SR2 - "Each report-name-1 may appear in only one
      *> REPORT clause." The granted form: R-A and R-B appear in the
      *> REPORTS clause of RPT-A only, R-C in the REPORT clause of RPT-C
      *> only, so the program is accepted and all three reports work
      *> (§2002:13.16.44.3 GR1: "more than one report may be written to
      *> the file"). The refused form is pb1285-report-in-two-clauses.
      *> §2002:13.16.44.3 GR2 - "no OPEN or CLOSE statements shall be
      *> executed that reference the report file" between INITIATE and
      *> TERMINATE; its consequence for CLOSE is §2002:14.8.6.3 GR5: "If
      *> any report is in the active state, the CLOSE statement for that
      *> file is completed and the EC-REPORT-NOT-TERMINATED exception
      *> condition is set to exist" (NF in the 2002 exception table).
      *> Checking is on by the TURN directive (§2002:7.2.19). Every
      *> cite.py --check here is OK.
      *> DERIVATION.
      *>   OPEN OUTPUT of both files succeeds: "OPEN=00 00".
      *>   INITIATE sets each LINE-COUNTER to zero (§2002:8.4.2.14.3 GR3,
      *>   §2002:14.8.20.3 GR1b): "LC=000".
      *>   R-C is TERMINATEd before CLOSE RPT-C, so that CLOSE raises
      *>   nothing; no exception has been raised, so EXCEPTION-STATUS is
      *>   31 spaces (§2002:15.28.2 RV1): "CLOSE-C=00 ES=[" 31 spaces "]".
      *>   R-B is still active (INITIATEd, never TERMINATEd, §2002:
      *>   14.8.20.3 GR4) when CLOSE RPT-A runs: the CLOSE completes,
      *>   status 00, and EC-REPORT-NOT-TERMINATED (24 letters, then 7
      *>   spaces) is the last exception status although R-A itself was
      *>   terminated: "any report" of the file.
      *> A leg fails if a report bound by one clause is refused or not
      *> initiated, if the CLOSE of a file with an active report fails or
      *> raises nothing, or if the CLOSE with no active report raises.
       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPT01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-A ASSIGN TO "g2rpt01a.rpt"
               FILE STATUS IS ST-A.
           SELECT RPT-C ASSIGN TO "g2rpt01c.rpt"
               FILE STATUS IS ST-C.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-A REPORTS ARE R-A R-B.
       FD  RPT-C REPORT IS R-C.
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  ST-C PIC XX.
       01  WS-SRC PIC 99 VALUE 7.
       01  N1 PIC 9.
       01  N2 PIC 9.
       01  N3 PIC 9.
       REPORT SECTION.
       RD  R-A.
       01  DET-A TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       RD  R-B.
       01  DET-B TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       RD  R-C.
       01  DET-C TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-A RPT-C
           DISPLAY "OPEN=" ST-A " " ST-C
           INITIATE R-A R-B R-C
           MOVE LINE-COUNTER OF R-A TO N1
           MOVE LINE-COUNTER OF R-B TO N2
           MOVE LINE-COUNTER OF R-C TO N3
           DISPLAY "LC=" N1 N2 N3
           GENERATE DET-A
           GENERATE DET-C
           TERMINATE R-C
           CLOSE RPT-C
           DISPLAY "CLOSE-C=" ST-C " ES=[" FUNCTION EXCEPTION-STATUS "]"
           TERMINATE R-A
           CLOSE RPT-A
           DISPLAY "CLOSE-A=" ST-A " ES=[" FUNCTION EXCEPTION-STATUS "]"
           STOP RUN.
