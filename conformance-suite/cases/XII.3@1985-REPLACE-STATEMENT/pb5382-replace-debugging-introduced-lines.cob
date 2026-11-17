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
      *> so its first line is a source line and ONE is displayed. Whether
      *> the line written as a debugging line inside its pseudo-text-2
      *> stays one the 1985 text leaves open: GR11 can be read to give it
      *> the replaced line's indicator (reading P), or, as the same
      *> sentence must be read in COPY beside §1985:XII.2.4 GR9's "only
      *> those text words that are specified on debugging lines where the
      *> debugging line is within pseudo-text-2 appear on debugging lines
      *> in the resultant program", to speak only of the additional lines
      *> (reading Q). CLAUDE.md rule 1's precedence chooses Q, as GnuCOBOL
      *> does (determination D-85-REPLACE-DEBUGGING-LINES), so the line is
      *> a debugging line and, without WITH DEBUGGING MODE, a comment
      *> (§1985:VI.7.3): TWO is not displayed, then THREE, the text after
      *> YSTMT on its own line. Why it can fail: a replacement that hides or
      *> keeps only the text on the carrier line runs ADD 2 (prints 3)
      *> (kb/Work PB5382), and one that takes reading P displays TWO
      *> (kb/Work PB5887).
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
