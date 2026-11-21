      *>--- conformance-case
      *> id: pb5924-debug-line-not-source-syntax-85
      *> rules: [1985-SR-VI.7.3-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.7.3 (cite.py: OK): "a syntactically correct program is formed with or without the debugging lines"
      *> No WITH DEBUGGING MODE: a debugging line that is not COBOL syntax when
      *> taken as source makes the program nonconforming; refused at 85.
      *> Refused COBOL7061 since PB5990.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGBAD.
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
      D    ADD TO GIVING.
           DISPLAY "END".
           STOP RUN.
