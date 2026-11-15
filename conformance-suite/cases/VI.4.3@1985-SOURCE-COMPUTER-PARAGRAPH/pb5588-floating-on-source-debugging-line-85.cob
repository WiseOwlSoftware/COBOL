      *>--- conformance-case
      *> id: pb5588-floating-on-source-debugging-line-85
      *> rules: [1985-GR-VI.4.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5588. §1985:VI.4.3.4 GR5 compiles a debugging line
      *>   as a comment line only "[i]f the WITH DEBUGGING MODE clause
      *>   is not specified". This program specifies it, so the
      *>   debugging line in MAIN-PARA is program text, COBOL-85 has no
      *>   floating comment indicator, and the characters that
      *>   COBOL-2002 made one are refused (COBOL0900). The control is
      *>   pb5588_floating_text_in_debugging_comment_85, the same line
      *>   with no mode. Fails if the comment treatment of a debugging
      *>   line is read without asking the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5588SD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "MAIN".
      D    MOVE 2 TO W *> a 2002 floating comment
           STOP RUN.
