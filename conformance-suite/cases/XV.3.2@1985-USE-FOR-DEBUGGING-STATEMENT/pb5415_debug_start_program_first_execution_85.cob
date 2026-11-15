      *>--- conformance-case
      *> id: pb5415_debug_start_program_first_execution_85
      *> rules: [1985-GR-XV.3.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5415. §1985:XV.3.2.4 GR16: DEBUG-CONTENTS holds
      *> 'START PROGRAM' when "the first execution of the first
      *> nondeclarative procedure in the program causes the debugging
      *> section to be executed". A program called again in its
      *> last-used state does not execute that procedure for the first
      *> time, so the section for S1 still runs (each execution of S1)
      *> but DEBUG-CONTENTS stays spaces (GR11). After CANCEL the
      *> program is in its initial state, and its next execution of S1
      *> is the first again. Expected output, in order: the trigger
      *> with START PROGRAM, IN S1; the trigger with spaces, IN S1; the
      *> trigger with START PROGRAM again after CANCEL, IN S1. Why it
      *> can fail: START PROGRAM emitted in every activation's entry
      *> code names it on the second line too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5415M.
       PROCEDURE DIVISION.
       M1.
           CALL "PB5415S".
           CALL "PB5415S".
           CANCEL "PB5415S".
           CALL "PB5415S".
           STOP RUN.
       END PROGRAM PB5415M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5415S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON S1.
       DBG-1.
           DISPLAY "[" DEBUG-NAME(1:2) "][" DEBUG-CONTENTS(1:13) "]".
       END DECLARATIVES.
       S1 SECTION.
       S1-P.
           DISPLAY "IN S1".
           EXIT PROGRAM.
       END PROGRAM PB5415S.
