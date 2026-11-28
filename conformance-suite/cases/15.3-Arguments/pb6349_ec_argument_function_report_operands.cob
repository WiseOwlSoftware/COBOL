*>--- conformance-case
*> id: pb6349_ec_argument_function_report_operands
*> rules: [AR-15.3-14, GR-14.9.16.4-1, GR-14.9.46.4-3, GR-13.18.53.4-2, GR-13.18.54.4-3, GR-13.18.41.4-2, GR-13.18.12.4-3]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> §2023:15.3 AR14 (cite.py --check passed): "If the evaluation of an argument results in an incorrect value for that
      *> argument or for the returned value according to the rules specified in the function definition and no exception
      *> condition was raised during item identification or expression evaluation, the EC-ARGUMENT-FUNCTION exception
      *> condition is set to exist." The rule attaches to the FUNCTION REFERENCE, wherever it is written, so a reference in
      *> a report group's operand raises when the GENERATE or TERMINATE that evaluates the operand runs. The same sentence
      *> stands in §2002:15.2 and §2014:15.3, which is why this test is accepted from 2002, the edition that introduced
      *> both the condition and the >>TURN directive.
      *>
      *> The violating argument is FUNCTION LOG10 (0): §2023:15.56.3 AR2 (cite.py --check passed): "The value of argument-1
      *> shall be greater than zero."
      *>
      *> WHERE EACH POSITION IS EVALUATED (each quotation passed cite.py --check):
      *>  - SOURCE: §2023:13.18.53.4 GR2, "Arithmetic-expression-1 specifies the operand of an implicit COMPUTE statement that
      *>    is executed implicitly whenever the associated item is printed." GENERATE prints the detail (§2023:14.9.16.4 GR1,
      *>    "Execution of a GENERATE data-name statement causes one instance of the specified detail to be printed"), so a
      *>    detail's SOURCE function runs inside the GENERATE; a control footing's runs inside the TERMINATE that prints it
      *>    (§2023:14.9.46.4 GR3b, "Each control footing is printed, if defined, beginning with the minor control footing").
      *>  - SUM: §2023:13.18.54.4 GR3, "The current content of each addend is implicitly added into the sum counter during
      *>    execution of GENERATE and TERMINATE statements"; GR7c2, adding takes place "whenever any GENERATE statement is
      *>    executed for a detail referenced by the UPON phrase", which names a detail of ANOTHER report in test 7
      *>    (§2023:13.18.54.3 SR7: "Data-name-2 shall be the name of a detail. It may be qualified only by a report-name.").
      *>  - PRESENT WHEN: §2023:13.18.41.4 GR2, "condition-1 of each PRESENT WHEN clause is evaluated before the processing of
      *>    any LINE clauses for the report group".
      *>  - CODE: §2023:13.18.12.4 GR3, "it is evaluated at the start of the processing for each body group".
      *>
      *> WHAT FAILED (kb/Work PB6349). The operands live in the report description, which the statement holds by reference
      *> and the walk that decides whether a statement is guarded for the condition did not enter, so every row below
      *> printed its label and no CAUGHT. A row whose CAUGHT is missing is that defect returning. Each GENERATE evaluates the
      *> operand once, so one CAUGHT follows its label. R-4's SUM is added at each GENERATE (test 4); its footing's SOURCE is
      *> evaluated when the footing is printed, at the TERMINATE (test 8), not at the chronologically first GENERATE, which
      *> prints no control footing (§2023:14.9.16.4 GR4). The reports TERMINATEd last print no footing and evaluate nothing.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6349RO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb6349_1.txt".
           SELECT P2 ASSIGN TO "pb6349_2.txt".
           SELECT P3 ASSIGN TO "pb6349_3.txt".
           SELECT P4 ASSIGN TO "pb6349_4.txt".
           SELECT P5 ASSIGN TO "pb6349_5.txt".
           SELECT P6 ASSIGN TO "pb6349_6.txt".
           SELECT P7 ASSIGN TO "pb6349_7.txt".
           SELECT P8 ASSIGN TO "pb6349_8.txt".
       DATA DIVISION.
       FILE SECTION.
       FD P1 REPORT IS R-1.
       FD P2 REPORT IS R-2.
       FD P3 REPORT IS R-3.
       FD P4 REPORT IS R-4.
       FD P5 REPORT IS R-5.
       FD P6 REPORT IS R-6.
       FD P7 REPORT IS R-7.
       FD P8 REPORT IS R-8.
       WORKING-STORAGE SECTION.
       01 W-Z PIC 9 VALUE 0.
       01 W-ONE PIC 9 VALUE 1.
       01 W-TAB VALUE "ABC".
          05 W-T PIC X OCCURS 3.
       01 W-CC VALUE "AB".
          05 W-C PIC X OCCURS 2.
       REPORT SECTION.
       RD R-1.
       01 DE-1 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE FUNCTION LOG10 (W-Z).
       RD R-2.
       01 DE-2 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE FUNCTION LOG10 (W-Z) + 1.
       RD R-3.
       01 DE-3 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC X SOURCE W-T (FUNCTION LOG10 (W-Z) + 1).
       RD R-4 CONTROL FINAL.
       01 DE-4 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE W-ONE.
       01 TYPE CF FINAL LINE PLUS 1.
          03 COLUMN 1 PIC 9 SUM FUNCTION LOG10 (W-Z).
          03 COLUMN 3 PIC 9 SOURCE FUNCTION LOG10 (W-Z).
       RD R-5.
       01 DE-5 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE W-ONE PRESENT WHEN FUNCTION LOG10 (W-Z) < 5.
       RD R-6 CODE W-C (FUNCTION LOG10 (W-Z) + 1).
       01 DE-6 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE W-ONE.
       RD R-7.
       01 DE-7 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE W-ONE.
       RD R-8 CONTROL FINAL.
       01 DE-8 TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9 SOURCE W-ONE.
       01 TYPE CF FINAL LINE PLUS 1.
          03 COLUMN 1 PIC 9 SUM FUNCTION LOG10 (W-Z) UPON DE-7 OF R-7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT P1 P2 P3 P4 P5 P6 P7 P8.
           INITIATE R-1 R-2 R-3 R-4 R-5 R-6 R-7 R-8.
           DISPLAY "1-SOURCE-FUNCTION".
           GENERATE DE-1.
           DISPLAY "2-SOURCE-EXPRESSION".
           GENERATE DE-2.
           DISPLAY "3-SOURCE-SUBSCRIPT".
           GENERATE DE-3.
           DISPLAY "4-SUM".
           GENERATE DE-4.
           DISPLAY "5-PRESENT-WHEN".
           GENERATE DE-5.
           DISPLAY "6-CODE-SUBSCRIPT".
           GENERATE DE-6.
           DISPLAY "7-SUM-UPON-OTHER-REPORT".
           GENERATE DE-7.
           DISPLAY "8-TERMINATE-SOURCE".
           TERMINATE R-4.
           TERMINATE R-1 R-2 R-3 R-5 R-6 R-7 R-8.
           CLOSE P1 P2 P3 P4 P5 P6 P7 P8.
           DISPLAY "DONE".
           STOP RUN.
