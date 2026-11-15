*>--- conformance-case
*> id: pb5612_sort_io_status_fatal_by_edition
*> rules: [GR-14.9.40.4-15]
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
      *> The SORT's GIVING file cannot be created (its directory does not exist): the implicit OPEN
      *> leaves I-O status '30', a permanent error and so the fatal EC-I-O-PERMANENT-ERROR.
      *> §2023:14.9.40.4 GR15 (cite.py --check): "If a fatal exception condition exists for file-name-3 as a
      *> result of the implicit OPEN during file initiation, the SORT is terminated".  EC-I-O checking is
      *> ON and no declarative applies.
      *> 2023: item 2) hands the condition to the SORT, which terminates; the run unit continues, so
      *>       "AFTER BN-ST=30" is displayed.
      *> 2014 and 2002: no applicable declarative, so §2014:14.6.12.1.2 4) and §2002:14.5.12.1.2 4) (cite.py
      *>       --check): "If checking for the exception condition is enabled, execution of the run unit is
      *>       terminated abnormally" -- nothing is displayed.
      >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612SIO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BAD-N ASSIGN TO "pb5612-no-such-dir/n.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BN-ST.
           SELECT SRT-FILE ASSIGN TO "pb5612sio-f.srt".
       DATA DIVISION.
       FILE SECTION.
       FD BAD-N.
       01 BN-REC PIC X(3).
       SD SRT-FILE.
       01 SRT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 BN-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               GIVING BAD-N.
           DISPLAY "AFTER BN-ST=" BN-ST.
           STOP RUN.
       FEED.
           MOVE "Z" TO SRT-REC RELEASE SRT-REC.
           MOVE "Y" TO SRT-REC RELEASE SRT-REC.
