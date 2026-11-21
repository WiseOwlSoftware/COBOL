      *>--- conformance-case
      *> id: 1985-GR-XIII_4_2_1-p1
      *> rules: [1985-GR-XIII.4.2.1-p1, 1985-GR-XIII.4.2.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.2.1 p1; §1985:XIII.4.2.4 p1 (THE CLOSE STATEMENT).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-18.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 XIII.4.2.1 / XIII.4.2.4: CLOSE of a report file, with
      * REEL, UNIT (synonyms: same status), FOR REMOVAL, NO REWIND
      * and LOCK. A disk file is non-reel/unit (D-MEDIUM-85): REEL,
      * UNIT, NO REWIND give 07; LOCK forbids a reopen (38).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "P04RPT.TXT"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-ST PIC XX.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           CLOSE RPT-FILE REEL.
           DISPLAY "REEL " WS-ST.
           CLOSE RPT-FILE UNIT.
           DISPLAY "UNIT " WS-ST.
           CLOSE RPT-FILE REEL FOR REMOVAL.
           DISPLAY "REEL REMOVAL " WS-ST.
           CLOSE RPT-FILE UNIT FOR REMOVAL.
           DISPLAY "UNIT REMOVAL " WS-ST.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE WITH NO REWIND.
           DISPLAY "NO REWIND " WS-ST.
           OPEN EXTEND RPT-FILE.
           DISPLAY "REOPEN " WS-ST.
           CLOSE RPT-FILE WITH LOCK.
           DISPLAY "LOCK " WS-ST.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "AFTER LOCK " WS-ST.
           STOP RUN.
