      *>--- conformance-case
      *> id: pb5382-copy-literal-on-debugging-line
      *> rules: [1985-GR-XII.2.4-9]
      *> files: [PB5382LL.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR9 (cite.py --check: OK): "If replacement requires that
      *> the continued literal be continued on a debugging line, the program is
      *> in error." XLIT stands on a debugging line of PB5382LL, and the
      *> 80-character literal of pseudo-text-2 does not fit the 61 character
      *> positions of area B: refused at 85 (COBOL7039) (kb/Work PB5382).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5382K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB5382LL REPLACING ==XLIT== BY
           =="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "BBBBBBBBBBBBBBBBBBBB"==.
           STOP RUN.
