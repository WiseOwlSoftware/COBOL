      *>--- conformance-case
      *> id: g85_f28_04
      *> rules: [1985-SR-XIII.3.5.6-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.6 SR2 - LINE-COUNTER in a SOURCE clause of the
      *> Report Section, and in Procedure Division contexts that admit a
      *> data item with an integral value. cite.py --check
      *> "§1985:XIII.3.5.6 SR2" "In the Procedure Division, LINE-COUNTER
      *> may be used in any context in which a data item with an integral
      *> value may appear" -> OK. The SOURCE leg is the accepted entry
      *> DL; each Procedure Division leg is a sending use (only the RWCS
      *> changes the content).
      *> DERIVED: INITIATE sets LINE-COUNTER to zero
      *> (§1985:XIII.3.5.6 GR4 -> OK): INIT=0000. GENERATE DL is absolute
      *> LINE 5 with no NEXT GROUP (Table 3: rules 3a, 4a, 6d). 0 < 5, so
      *> the current page (§1985:XIII.3.10.8 GR3a -> OK); presented on
      *> line 5 (§1985:XIII.3.10.8 GR4a -> OK); the final setting is that
      *> line (§1985:XIII.3.10.8 GR6d -> OK), so LINE-COUNTER = 5:
      *>   MOVE LINE-COUNTER TO W           -> MOVE=0005
      *>   IF LINE-COUNTER = 5              -> IF=TRUE
      *>   COMPUTE W = LINE-COUNTER * 3 + 1 -> COMPUTE=0016
      *>   ADD LINE-COUNTER 10 GIVING W     -> ADD=0015
      *>   PERFORM P-COUNT LINE-COUNTER TIMES -> TIMES=0005
      *>   WS-ENT (LINE-COUNTER) subscript  -> SUBSCRIPT=ABC
      *> A compiler that refuses one of these contexts, or holds a value
      *> other than the RWCS's, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2804.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  W      PIC 9(4) VALUE 0.
       01  WS-CNT PIC 9(4) VALUE 0.
       01  WS-TBL.
           02  WS-ENT PIC X(3) OCCURS 9 TIMES.
       REPORT SECTION.
       RD  RPT1
           PAGE LIMIT IS 60 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS 5.
           02  COLUMN NUMBER IS 1 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT1.
           MOVE LINE-COUNTER TO W.
           DISPLAY "INIT=" W.
           GENERATE DL.
           MOVE LINE-COUNTER TO W.
           DISPLAY "MOVE=" W.
           IF LINE-COUNTER = 5
               DISPLAY "IF=TRUE"
           ELSE
               DISPLAY "IF=FALSE".
           COMPUTE W = LINE-COUNTER * 3 + 1.
           DISPLAY "COMPUTE=" W.
           ADD LINE-COUNTER 10 GIVING W.
           DISPLAY "ADD=" W.
           PERFORM P-COUNT LINE-COUNTER TIMES.
           DISPLAY "TIMES=" WS-CNT.
           MOVE "ABC" TO WS-ENT (LINE-COUNTER).
           DISPLAY "SUBSCRIPT=" WS-ENT (5).
           TERMINATE RPT1.
           CLOSE RPT-FILE.
           STOP RUN.
       P-COUNT.
           ADD 1 TO WS-CNT.
