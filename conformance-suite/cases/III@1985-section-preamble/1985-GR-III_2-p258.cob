      *>--- conformance-case
      *> id: 1985-GR-III_2-p258
      *> rules: [1985-GR-III.2-p258, 1985-GR-III.2-p263, 1985-GR-III.2-p266, 1985-GR-III.2-p267]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p258; §1985:III.2 p263; §1985:III.2 p266;
      *> §1985:III.2 p267 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RW85.
      * 1985 III.2 Report File / Report Line / RWCS: an output file
      * whose FD has a REPORT clause; lines numbered from 1 at the top.
      * Expect G05RW.TXT = "HEAD1", "", "  DET03"; LC=03 after GENERATE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "G05RW.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  LC         PIC 99.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 3.
       01  TYPE IS REPORT HEADING.
           05  LINE 1 COLUMN 1 PIC X(5) VALUE "HEAD1".
       01  DL TYPE IS DETAIL.
           05  LINE 3 COLUMN 3 PIC X(5) VALUE "DET03".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PRTF.
           INITIATE RPT.
           GENERATE DL.
           MOVE LINE-COUNTER OF RPT TO LC.
           DISPLAY "LC=" LC.
           TERMINATE RPT.
           CLOSE PRTF.
           STOP RUN.
