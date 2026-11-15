*>--- conformance-case
*> id: pb5612_sort_file_open_fatal_2002
*> rules: [2002-GR-14.8.36.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5612 (VCR row 7.80).  §2002:14.8.36.3 GR8a (cite.py --check): "If the file referenced by file-name-2 is in an
      *> open mode when this phase commences, the EC-SORT-MERGE-FILE-OPEN exception condition is set to exist and the
      *> results of the execution of the SORT statement are undefined."  Here the SORT names G-OUT, GIVING file of phase c),
      *> open (GR8c says the same).  EC-SORT-MERGE-FILE-OPEN is Fatal (Table 14) and checking is on, so the fatal exception
      *> procedure decides (§2002:14.5.12.1.2 2), cite.py --check): "If execution of the declarative completes normally the
      *> execution of the run unit is terminated abnormally"; the 2002 text has no item for a SORT or MERGE statement.
      *> Expected: "DECL=EC-SORT-MERGE-FILE-OPEN", then the run unit ends: "AFTER-SORT" is never displayed (a compiler that
      *> continues after the declarative, the 2023 reading of §2023:14.6.13.1.3 2), prints it).
      >>TURN EC-SORT-MERGE-FILE-OPEN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5612SFO2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G-OUT ASSIGN TO "pb5612sfo2-g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
           SELECT SF ASSIGN TO "pb5612sfo2-f.srt".
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
