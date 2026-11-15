      *>--- conformance-case
      *> id: pb5588_floating_text_in_debugging_comment_85
      *> rules: [1985-GR-XV.2.1.3-2, 1985-GR-VI.4.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5588. §1985:XV.2.1.3 GR2: without WITH DEBUGGING
      *>   MODE "any USE FOR DEBUGGING statements and all associated
      *>   debugging sections are compiled as if they were comment
      *>   lines"; §1985:VI.4.3.4 GR5: "any debugging lines are
      *>   compiled as if they were comment lines". A comment line
      *>   may hold any characters of the computer's character set
      *>   (§1985:III, Comment Line), so the two characters that
      *>   COBOL-2002 made a floating comment indicator are no more
      *>   an error in the debugging section DBG-SEC below, or on
      *>   the debugging line in MAIN-PARA, than in a comment line.
      *>   COBOL-85 has no floating comment indicator, so the same
      *>   characters in program text would be refused. The program
      *>   has no WITH DEBUGGING MODE. Expected output: MAIN alone
      *>   (the debugging line is not compiled, so it displays
      *>   nothing). Fails if either line draws COBOL0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5588FC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SEC SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       DBG-PARA.
           MOVE 1 TO W *> a 2002 floating comment
           .
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           DISPLAY "MAIN".
      D    DISPLAY "DEBUG LINE" *> a 2002 floating comment
           STOP RUN.
