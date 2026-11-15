      *>--- conformance-case
      *> id: pb5503-replace-literal-tab-area-b
      *> rules: [1985-GR-XII.3.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 (cite.py --check: OK): "If replacement requires the
      *> continued literal to be continued on a debugging line, the program is
      *> in error." Here the tab follows ==, so it fills to the tab stop at
      *> position 17: the literal begins at offset 9 of the program-text area, in
      *> area B, which holds 61 positions before margin R; the 64-position
      *> literal replacing XLIT on a debugging line would have to be continued
      *> there: refused at 85 (COBOL7039). The file's own column counts the tab
      *> as one position (offset 2, area A) and let it compile (kb/Work PB5503).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5503B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
           REPLACE ==XLIT== BY
       ==	"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAA"==.
       MAIN-PARA.
      D    DISPLAY XLIT.
           STOP RUN.
