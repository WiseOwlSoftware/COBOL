      *>--- conformance-case
      *> id: 1985-GR-XI_4_4_4-7
      *> rules: [1985-GR-XI.4.4.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR7 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTRNG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SR-KEY  PIC 9.
           05  SR-NAME PIC X(5).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       IN-PARA.
           MOVE 3 TO SR-KEY.
           MOVE "CHARL" TO SR-NAME.
           PERFORM REL-PARA.
           MOVE 1 TO SR-KEY.
           MOVE "ALPHA" TO SR-NAME.
           PERFORM REL-PARA.
           MOVE 2 TO SR-KEY.
           MOVE "BRAVO" TO SR-NAME.
           PERFORM REL-PARA.
       OUT-SEC SECTION.
       OUT-PARA.
           RETURN SORT-FILE AT END GO TO OUT-END.
           DISPLAY SR-KEY " " SR-NAME.
           GO TO OUT-PARA.
       OUT-END.
           EXIT.
       REL-SEC SECTION.
       REL-PARA.
           RELEASE SORT-REC.
