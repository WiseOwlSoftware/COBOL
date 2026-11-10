      *>--- conformance-case
      *> id: 1985-GR-XI_2_3_4-1
      *> rules: [1985-GR-XI.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.3.4 GR1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ASGSRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT-FILE ASSIGN TO "SORTWK".
       DATA DIVISION.
       FILE SECTION.
       SD  SRT-FILE.
       01  SRT-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "BBBBB" TO SRT-REC.
           RELEASE SRT-REC.
           MOVE "AAAAA" TO SRT-REC.
           RELEASE SRT-REC.
       OUT-PROC SECTION.
       OUT-PARA.
           RETURN SRT-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               DISPLAY SRT-REC
               GO TO OUT-PARA.
       OUT-EXIT.
           EXIT.
