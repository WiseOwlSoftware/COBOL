      *>--- conformance-case
      *> id: pb5990-debugging-line-source-both-ways
      *> rules: [1985-GR-VI.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such that
      *> a syntactically correct program is formed with or without the
      *> debugging lines being considered as comment lines." The unit has
      *> no WITH DEBUGGING MODE, so the debugging line below is a comment
      *> line; it is also a complete statement, so the program is
      *> correct with the line taken as source too (kb/Work PB5990).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "DEBUG ONLY".
           DISPLAY "END".
           STOP RUN.
