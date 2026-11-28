*>--- conformance-case
*> id: pb6349_ec_argument_function_report_operands_below_2002
*> rules: [AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> The edition gate under conformance:cases/15.3-Arguments/pb6349_ec_argument_function_report_operands.
      *> §2023:15.3 AR14's EC-ARGUMENT-FUNCTION is only OBSERVABLE where checking can be enabled, and the §2023:7.3
      *> compiler-directive facility - >>TURN (§2023:7.3.25) - is a COBOL-2002 introduction, so the same program (here
      *> one report with a SOURCE function alone) is refused at 1985. The raise itself is edition-independent from 2002
      *> on; this fixture exists so the 2002 floor of the checking half cannot drift unnoticed.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6349ROGATE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb6349g.txt".
       DATA DIVISION.
       FILE SECTION.
       FD P1 REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 W-Z PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-1.
       01 DE-1 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE FUNCTION LOG10 (W-Z).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT P1.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE P1.
           STOP RUN.
