      *>--- conformance-case
      *> id: 1985-FMT-XI_4_2_2
      *> rules: [1985-FMT-XI.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.2.2 (THE RELEASE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT-FILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT-FILE.
       01  SRT-REC.
           05  SRT-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-REC PIC X(3).
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC.
           MOVE "BBB" TO WS-REC.
           RELEASE SRT-REC FROM WS-REC.
           MOVE "AAA" TO WS-REC.
           RELEASE SRT-REC FROM WS-REC.
       OUT-PROC.
           RETURN SRT-FILE AT END MOVE "Y" TO WS-EOF.
           DISPLAY SRT-KEY.
           RETURN SRT-FILE AT END MOVE "Y" TO WS-EOF.
           DISPLAY SRT-KEY.
