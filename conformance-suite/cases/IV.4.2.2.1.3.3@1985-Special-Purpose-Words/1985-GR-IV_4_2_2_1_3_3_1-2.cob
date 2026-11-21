      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_3_3_1-2
      *> rules: [1985-GR-IV.4.2.2.1.3.3.1-2, 1985-SR-IV.4.2.2.1.3.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3.3.1 GR2; §1985:IV.4.2.2.1.3.3.1 p1 (Special
      *> Purpose Words). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-03.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.1.3.3.1-p1 / -2: LINAGE-COUNTER is a
      * special register usable as an identifier of its category,
      * qualified by its file-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P09LINAGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO P09RPT.
           SELECT RPT2 ASSIGN TO P09RPT2.
       DATA DIVISION.
       FILE SECTION.
       FD RPT LINAGE IS 10 LINES.
       01 RPT-REC PIC X(20).
       FD RPT2 LINAGE IS 5 LINES.
       01 RPT2-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01 N PIC 99.
       01 M PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT RPT2.
           MOVE "LINE" TO RPT-REC.
           WRITE RPT-REC.
           WRITE RPT-REC.
           WRITE RPT-REC.
           WRITE RPT2-REC FROM RPT-REC.
           MOVE LINAGE-COUNTER OF RPT TO N.
           DISPLAY "N=" N.
           ADD LINAGE-COUNTER IN RPT2 LINAGE-COUNTER OF RPT GIVING M.
           DISPLAY "M=" M.
           IF LINAGE-COUNTER OF RPT2 = 1 DISPLAY "Q=T"
               ELSE DISPLAY "Q=F".
           CLOSE RPT RPT2.
           STOP RUN.
