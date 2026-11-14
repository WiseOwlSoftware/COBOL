      *>--- conformance-case
      *> id: pb5472-copy-debugging-line-replaces-mid-line
      *> rules: [1985-GR-XII.2.4-7]
      *> files: [PB5472LB.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR7 (cite.py --check: OK): "Debugging lines are permitted
      *> within library text and pseudo-text". The pseudo-text-2 of the COPY
      *> statement is a debugging line that replaces XX, a word in the MIDDLE of
      *> the library line DISPLAY "[" XX "]". Only the replacement is on a
      *> debugging line, so the program is correct with and without it
      *> (§1985:VI.7.3, checked after all COPY statements are processed): with the
      *> mode on, the unit DISPLAYs [Q]. Why it can fail: the debugging-line
      *> check hides the library text ahead of the replacement with it and
      *> refuses a correct program as COBOL7061 (kb/Work PB5472).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5472A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  QQ PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB5472LB REPLACING ==XX== BY ==
      D        QQ==.
           STOP RUN.
