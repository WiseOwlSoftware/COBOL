      *>--- conformance-case
      *> id: 1985-GR-XI_4_3_4-5
      *> rules: [1985-GR-XI.4.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.4 GR5 (THE RETURN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RETINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SR-A            PIC X(5).
       01  SR-B.
           05  SR-KEY      PIC X(3).
           05  SR-REST     PIC X(2).
       WORKING-STORAGE SECTION.
       01  WS-REC          PIC X(5).
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       IN-PARA.
           MOVE "BBB22" TO SR-A.
           RELEASE SR-A.
           MOVE "AAA11" TO SR-A.
           RELEASE SR-A.
       OUT-SEC SECTION.
       OUT-PARA.
           RETURN SORT-FILE INTO WS-REC
               AT END DISPLAY "NO RECORD 1".
           DISPLAY WS-REC.
           RETURN SORT-FILE INTO WS-REC
               AT END DISPLAY "NO RECORD 2".
           DISPLAY WS-REC.
           RETURN SORT-FILE INTO WS-REC
               AT END DISPLAY "END".
