      *>--- conformance-case
      *> id: pb5990-debugging-line-not-source-without-mode
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such that
      *> a syntactically correct program is formed with or without the
      *> debugging lines being considered as comment lines." The unit has
      *> no WITH DEBUGGING MODE, so the debugging line below is a comment
      *> line and the rest of the program parses; with the line taken as
      *> source its text is no statement, so the program is refused
      *> (kb/Work PB5990).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    ADD TO GIVING.
           DISPLAY "END".
           STOP RUN.
