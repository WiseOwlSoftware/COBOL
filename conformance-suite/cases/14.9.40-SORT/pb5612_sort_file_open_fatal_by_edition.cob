*>--- conformance-case
*> id: pb5612_sort_file_open_fatal_by_edition
*> rules: [GR-14.9.40.4-9]
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
      *> The SORT names G-OUT as its GIVING file while the program holds it open:
      *> §2023:14.9.40.4 GR9 (§2002:14.8.36.3 GR8a is the same rule; cite.py --check): "the
      *> EC-SORT-MERGE-FILE-OPEN exception condition is set to exist".  Fatal; checking is on.
      *> Expected: "DECL=EC-SORT-MERGE-FILE-OPEN" at every edition; "AFTER-SORT GS=00" only at 2023
      *> (the open GIVING file was never touched, so its FILE STATUS is still the program's own OPEN's '00').
      >>TURN EC-SORT-MERGE-FILE-OPEN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612SFO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G-OUT ASSIGN TO "pb5612sfo-g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
           SELECT SF ASSIGN TO "pb5612sfo-f.srt".
       DATA DIVISION.
       FILE SECTION.
       FD G-OUT.
       01 G-REC PIC X(3).
       SD SF.
       01 SF-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 GS    PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-V SECTION.
           USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-FILE-OPEN.
       D-V-P.
           DISPLAY "DECL=" FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT G-OUT.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS FEED GIVING G-OUT.
           DISPLAY "AFTER-SORT GS=" GS.
           STOP RUN.
       FEED.
           MOVE "QQQ" TO SF-REC.
           RELEASE SF-REC.
