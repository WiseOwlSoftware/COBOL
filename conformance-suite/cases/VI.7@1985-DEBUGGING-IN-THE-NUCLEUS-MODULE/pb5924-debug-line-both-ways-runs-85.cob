      *>--- conformance-case
      *> id: pb5924-debug-line-both-ways-runs-85
      *> rules: [1985-SR-VI.7.3-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.7.3 (cite.py: OK): "a syntactically correct program is formed with or without the debugging lines"
      *> A debugging line that is a complete statement either way compiles;
      *> without the mode it is a comment line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGOK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ.
       OBJECT-COMPUTER. XYZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "DEBUG ONLY".
           DISPLAY "END".
           STOP RUN.
