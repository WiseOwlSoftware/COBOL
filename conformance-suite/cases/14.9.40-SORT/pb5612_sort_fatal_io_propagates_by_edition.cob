*>--- conformance-case
*> id: pb5612_sort_fatal_io_propagates_by_edition
*> rules: [GR-14.9.40.4-15]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5612, VCR row 7.80 (DialectBehavior.SortMergeStatementDisposesItsFatalConditions).
      *> 2023 -- §2023:14.6.13.1.3 2) (cite.py --check): "If the executed statement is a MERGE or SORT
      *> statement, then the rules for those statements apply."  The verb's rule here says the statement
      *> terminates, so after the declarative the run unit CONTINUES after the statement.
      *> 2014 and 2002 -- §2014:14.6.12.1.2 2) and §2002:14.5.12.1.2 2) (cite.py --check): "If execution of the
      *> declarative completes normally the execution of the run unit is terminated abnormally"; neither edition
      *> has an item for a MERGE or SORT statement, so the statement is not a special case.
      *> Per edition: .out is the 2023 output; .2014.out ends at the declarative (the run unit is
      *> terminated abnormally; its stderr and exit status are not compared).
      *> SSUB's SORT USING a missing file leaves the implicit OPEN's I-O status '35' (a permanent error,
      *> the fatal EC-I-O-PERMANENT-ERROR); EC-I-O checking is on and >>PROPAGATE ON is in effect.
      *> 2014 and 2002: §2014:14.6.12.1.2 3) and §2002:14.5.12.1.2 3) (cite.py --check): "the exception
      *>       condition is propagated as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were
      *>       executed", so SMAIN's declarative runs and, completing normally, ends the run unit
      *>       (item 2): "MAIN CAUGHT" and nothing after.
      *> 2023: item 2) of §2023:14.6.13.1.3 sends the condition to the SORT's rules first; the SORT is terminated, the
      *>       condition is not propagated, and SSUB continues: "SSUB CONTINUED FS=35", "MAIN AFTER".
      >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612PM.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-PERMANENT-ERROR.
       D1P.
           DISPLAY "MAIN CAUGHT " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       M SECTION.
       M1.
           DISPLAY "MAIN START"
           CALL "PB5612PS"
           DISPLAY "MAIN AFTER"
           STOP RUN.
       END PROGRAM PB5612PM.
      >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612PS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INF ASSIGN TO "pb5612-nosuch-in.dat"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS.
           SELECT OUTF ASSIGN TO "pb5612-sort-out.dat"
               ORGANIZATION SEQUENTIAL.
           SELECT SF ASSIGN TO "pb5612-sortwk".
       DATA DIVISION.
       FILE SECTION.
       FD INF.
       01 IR PIC X(4).
       FD OUTF.
       01 OR1 PIC X(4).
       SD SF.
       01 SR.
          05 SK PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
           SORT SF ON ASCENDING KEY SK USING INF GIVING OUTF
           DISPLAY "SSUB CONTINUED FS=" FS
           GOBACK.
       END PROGRAM PB5612PS.
