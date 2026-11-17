      *>--- conformance-case
      *> id: pb5382-replace-debugging-introduced-lines
      *> rules: [1985-GR-XII.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 (cite.py --check: OK): "the indicator area of the
      *> introduced lines contains the same character as the line on which the
      *> text being replaced begins". XSTMT is replaced on a debugging line, so
      *> both ADD lines are debugging lines, and without WITH DEBUGGING MODE
      *> they are comments: WS-N stays 1. YSTMT is replaced on a source line,
      *> so its first line is a source line and ONE is displayed; the line
      *> written as a debugging line inside its pseudo-text-2 stays one
      *> ("Debugging lines are permitted in pseudo-text", §1985:XII.3.4 GR8;
      *> the COPY twin's GR11 is the same sentence and stands beside
      *> §1985:XII.2.4 GR9's "only those text words that are specified on
      *> debugging lines where the debugging line is within pseudo-text-2
      *> appear on debugging lines in the resultant program"), and "After
      *> all COPY and REPLACE statements have been processed, a debugging
      *> line will be considered to have all the characteristics of a
      *> comment line, if the WITH DEBUGGING MODE clause is not specified"
      *> (§1985:VI.7.3): TWO is not displayed, then THREE, the text after
      *> YSTMT on its own line. Why it can fail: a replacement that hides or
      *> keeps only the text on the carrier line runs ADD 2 (prints 3)
      *> (kb/Work PB5382), and one that gives pseudo-text-2's debugging line
      *> the replaced line's indicator displays TWO (kb/Work PB5887).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5382R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           REPLACE ==XSTMT== BY ==ADD 1 TO WS-N
           ADD 2 TO WS-N==
                   ==YSTMT== BY ==DISPLAY "ONE"
      D    DISPLAY "TWO"==.
       MAIN-PARA.
      D    XSTMT.
           DISPLAY WS-N.
           YSTMT DISPLAY "THREE".
           STOP RUN.
