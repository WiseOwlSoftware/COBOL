      *>--- conformance-case
      *> id: pb5503-replace-literal-tab-area-a
      *> rules: [1985-GR-XII.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 (cite.py --check: OK): "If replacement requires the
      *> continued literal to be continued on a debugging line, the program is
      *> in error." The tab opening the pseudo-text-2 line fills
      *> positions 1-8 (tab stops 1, 9, 17), so ==" puts the literal's quote
      *> at position 11, in area A (positions 8-11), and the 65-position literal
      *> fits the 65 positions area A holds before margin R when it replaces XLIT
      *> on a debugging line: no continuation on a debugging line is needed and
      *> the program runs (kb/Work PB5503: the file's column of the quote counts
      *> the tab as one position, 3, and read it in area B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5503A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
           REPLACE ==XLIT== BY
	=="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AA"==.
       MAIN-PARA.
      D    DISPLAY XLIT.
           STOP RUN.
