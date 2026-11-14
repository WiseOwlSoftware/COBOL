      *>--- conformance-case
      *> id: pb5472-copy-debugging-line-replacement-needed
      *> rules: [1985-GR-XII.2.4-7]
      *> files: [PB5472LC.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3 (cite.py --check: OK): "The content of a debugging line
      *> must be such that a syntactically correct program is formed with or
      *> without the debugging lines being considered as comment lines." The
      *> debugging line of pseudo-text-2 replaces the operand of MOVE, so the
      *> program without it is MOVE TO QQ, which is not correct: COBOL7061 at
      *> --std 85, the control of pb5472-copy-debugging-line-replaces-mid-line
      *> (kb/Work PB5472).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5472B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  QQ PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY PB5472LC REPLACING ==XX== BY ==
      D        QQ==.
           STOP RUN.
