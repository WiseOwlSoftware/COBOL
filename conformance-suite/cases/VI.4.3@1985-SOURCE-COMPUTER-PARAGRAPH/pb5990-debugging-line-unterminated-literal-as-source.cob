      *>--- conformance-case
      *> id: pb5990-debugging-line-unterminated-literal-as-source
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such that
      *> a syntactically correct program is formed with or without the
      *> debugging lines being considered as comment lines." The unit has
      *> no WITH DEBUGGING MODE, so the debugging line below is a comment
      *> line and the rest of the program parses; with the line taken as
      *> source its literal is unterminated (COBOL5563),
      *> so the program is refused: the screens source text gets apply to the
      *> line (kb/Work PB5990).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5990C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "UNTERMINATED
           DISPLAY "END".
           STOP RUN.
