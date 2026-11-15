      *>--- conformance-case
      *> id: pb5382-replace-literal-on-debugging-line
      *> rules: [1985-GR-XII.3.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 (cite.py --check: OK): "If replacement requires the
      *> continued literal to be continued on a debugging line, the program is
      *> in error." The 80-character literal of pseudo-text-2 replaces XLIT on a
      *> debugging line; area B holds 61 character positions, so the literal
      *> would be continued on a debugging line: refused at 85 (COBOL7039). The
      *> same literal on a source line is 1985-GR-XII_3_4-11 (kb/Work PB5382).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5382L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
           REPLACE ==XLIT== BY
           =="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "BBBBBBBBBBBBBBBBBBBB"==.
       MAIN-PARA.
      D    DISPLAY XLIT.
           STOP RUN.
