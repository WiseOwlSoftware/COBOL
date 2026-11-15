      *>--- conformance-case
      *> id: pb5414_debug_section_subject_entry_85
      *> rules: [1985-GR-XV.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5414. §1985:XV.3.2.4 GR3 a: the debugging section
      *> for procedure-name-1 runs "Immediately before each execution
      *> of the named procedure". Procedure-name-1 is the SECTION S2,
      *> and it is executed once each time control reaches it from
      *> outside: here a PERFORM of S2, then a GO TO S2. The GO TO P1
      *> that loops inside S2 is control moving between S2's own
      *> paragraphs, no new execution of S2, so it runs nothing.
      *> Expected output, in order: the trigger for the PERFORM, N=3
      *> (P1 ran three times, no trigger between), BACK, the trigger
      *> for the GO TO, N=3, and the run ends in S2 (STOP RUN). Why it
      *> can fail: a section subject triggered at its first paragraph's
      *> pc fires on each GO TO P1 (the pre-fix output has two more DS
      *> lines for every visit).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5414ENT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       01  K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS SECTION.
           USE FOR DEBUGGING ON S2.
       DS-1.
           DISPLAY "DS " DEBUG-NAME(1:2).
       END DECLARATIVES.
       S1 SECTION.
       M-1.
           PERFORM S2.
           DISPLAY "BACK".
           MOVE 0 TO N.
           GO TO S2.
       S2 SECTION.
       P1.
           ADD 1 TO N.
           IF N < 3 GO TO P1.
           DISPLAY "N=" N.
       P2.
           IF K = 1 STOP RUN.
           MOVE 1 TO K.
