      *>--- conformance-case
      *> id: pb5212-debugging-line-syntax-both-ways
      *> rules: [1985-GR-VI.4.3.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such that
      *> a syntactically correct program is formed with or without the
      *> debugging lines being considered as comment lines." The DISPLAY
      *> on the debugging line below takes its operand from the line after
      *> it, so with the debugging line a comment the program does not
      *> parse (kb/Work PB5212).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5212B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
      D    DISPLAY
               "X".
           DISPLAY "END".
           STOP RUN.
