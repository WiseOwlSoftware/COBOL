      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_2-p2
      *> rules: [1985-SR-XVI.3.3.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.2 p2 (RESTRICTIONS ON PROGRAM FLOW). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p12: a PERFORM in an independent segment (1985 XVI.3.3.2
      * p2): its range may be non-independent sections (S20 THRU
      * S30) or sections of its own independent segment (S60B), and
      * in addition a declarative section its execution causes.
      * Expect: S20 S30 S60B USE S60B-END DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P12.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NOFILE ASSIGN TO "S19NOFILE"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  NOFILE.
       01  NOREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NOFILE.
       ERR-1.
           DISPLAY "USE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       M-1.
           GO TO S60A-1.
       S20 SECTION 20.
       S20-1.
           DISPLAY "S20".
       S30 SECTION 30.
       S30-1.
           DISPLAY "S30".
       S60A SECTION 60.
       S60A-1.
           PERFORM S20 THRU S30.
           PERFORM S60B.
           DISPLAY "DONE".
           STOP RUN.
       S60B SECTION 60.
       S60B-1.
           DISPLAY "S60B".
           OPEN INPUT NOFILE.
           DISPLAY "S60B-END".
