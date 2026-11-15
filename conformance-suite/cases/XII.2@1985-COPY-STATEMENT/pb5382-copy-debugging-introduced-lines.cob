      *>--- conformance-case
      *> id: pb5382-copy-debugging-introduced-lines
      *> rules: [1985-GR-XII.2.4-9]
      *> files: [PB5382LB.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR9 (cite.py --check: OK): "When a text word specified in
      *> the BY phrase is introduced, it appears on a debugging line if the first
      *> library text word being replaced is specified on a debugging line",
      *> and otherwise "only those text words that are specified on debugging
      *> lines where the debugging line is within pseudo-text-2 appear on
      *> debugging lines". XSTMT stands on a debugging line of PB5382LB, so both
      *> ADD lines are debugging lines, comments without WITH DEBUGGING MODE:
      *> WS-N stays 1. YSTMT stands on a source line, so DISPLAY "ONE" is
      *> source and the debugging line of pseudo-text-2 stays one (TWO is not
      *> displayed), while the text after YSTMT keeps its own source line
      *> (THREE). Why it can fail: the carrier line alone decides (prints 3), or
      *> the text after the replacement joins the debugging line (no THREE)
      *> (kb/Work PB5382).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5382C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB5382LB REPLACING ==XSTMT== BY ==ADD 1 TO WS-N
           ADD 2 TO WS-N==
                                   ==YSTMT== BY ==DISPLAY "ONE"
      D    DISPLAY "TWO"==.
           DISPLAY WS-N.
           STOP RUN.
