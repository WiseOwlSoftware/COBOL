      *>--- conformance-case
      *> id: 1985-FMT-XI_4_4_2
      *> rules: [1985-FMT-XI.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.2 (THE SORT STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTDUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "SORTWK".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC.
           05  SR-KEY  PIC 9.
           05  SR-TAG  PIC X.
       WORKING-STORAGE SECTION.
       01  WS-DATA  PIC X(8) VALUE "2A1B2C1D".
       01  WS-TAB REDEFINES WS-DATA.
           05  WS-ENT  PIC X(2) OCCURS 4 TIMES.
       01  WS-I    PIC 9.
       01  WS-EOF  PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               WITH DUPLICATES IN ORDER
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       IN-PARA.
           PERFORM REL-PARA VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 4.
           GO TO IN-EXIT.
       REL-PARA.
           MOVE WS-ENT (WS-I) TO SORT-REC.
           RELEASE SORT-REC.
       IN-EXIT.
           EXIT.
       OUT-SEC SECTION.
       OUT-PARA.
           PERFORM RET-PARA UNTIL WS-EOF = "Y".
           GO TO OUT-EXIT.
       RET-PARA.
           RETURN SORT-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N" DISPLAY SR-KEY SR-TAG.
       OUT-EXIT.
           EXIT.
