*>--- conformance-case
*> id: pb5612_merge_file_open_fatal_by_edition
*> rules: [GR-14.9.24.4-7]
*> accept: [2002, 2014, 2023]
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
      *> Per edition: .out is the 2023 output; .2014.out and .2002.out end at the declarative (the run unit is
      *> terminated abnormally; its stderr and exit status are not compared).
      *> The MERGE names M-A as a USING file while the program holds it open:
      *> §2023:14.9.24.4 GR7 (§2002:14.8.23.3 GR7 is the same rule; cite.py --check): "the EC-SORT-MERGE-FILE-OPEN
      *> exception condition is set to exist and the execution of the MERGE statement terminates".
      *> EC-SORT-MERGE-FILE-OPEN is Fatal (Table 13 / Table 14), and checking is on.
      *> Expected: "DECL EC-SORT-MERGE-FILE-OPEN" at every edition (the declarative runs first);
      *> "AFTER MERGE" only at 2023 (the statement's own rule, item 2).  A test that could fail: a
      *> compiler that applies the 2023 disposition below 2023 prints AFTER MERGE at 2002 and 2014.
      >>TURN EC-SORT-MERGE-FILE-OPEN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612MFO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT M-A ASSIGN TO "pb5612mfo-a.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT M-B ASSIGN TO "pb5612mfo-b.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT G-O ASSIGN TO "pb5612mfo-g.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT S2 ASSIGN TO "pb5612mfo-s.srt".
       DATA DIVISION.
       FILE SECTION.
       FD M-A.
       01 A-REC PIC X(3).
       FD M-B.
       01 B-REC PIC X(3).
       FD G-O.
       01 G-REC PIC X(3).
       SD S2.
       01 S2-REC PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-V SECTION.
           USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-FILE-OPEN.
       D-V-P.
           DISPLAY "DECL " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT M-A M-B.
           MOVE "AAA" TO A-REC. WRITE A-REC.
           MOVE "BBB" TO B-REC. WRITE B-REC.
           CLOSE M-A M-B.
           OPEN INPUT M-A.
           MERGE S2 ON ASCENDING KEY S2-REC
               USING M-A M-B GIVING G-O.
           DISPLAY "AFTER MERGE".
           STOP RUN.
