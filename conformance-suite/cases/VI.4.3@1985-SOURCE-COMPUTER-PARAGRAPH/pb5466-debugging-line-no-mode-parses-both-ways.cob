      *>--- conformance-case
      *> id: pb5466-debugging-line-no-mode-parses-both-ways
      *> rules: [1985-GR-VI.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3: "The content of a debugging line must be such
      *> that a syntactically correct program is formed with or without
      *> the debugging lines being considered as comment lines." The
      *> control of pb5466-debugging-line-syntax-no-mode: the unit has
      *> no WITH DEBUGGING MODE, so the debugging lines are comment
      *> lines, and each is a complete statement, so the program formed
      *> with them as source is syntactically correct too and compiles.
      *> Derivation: the debugging lines do not run (§1985:VI.4.3.4
      *> GR5, comment lines without the mode), so only "END" prints.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5466C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1.
       OBJECT-COMPUTER. COMPUTER-1.
       PROCEDURE DIVISION.
       P1.
      D    DISPLAY "DEBUG ONE".
           DISPLAY "END".
      D    DISPLAY
      D        "DEBUG TWO".
           STOP RUN.
