*>--- conformance-case
*> id: pb3511_implicit_goback_perform_thru_inverted
*> rules: [GR-14.6.3-3, GR-14.9.28.4-5, GR-14.9.28.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - a PERFORM THRU whose procedure-name-2 is written BEFORE
      *>   procedure-name-1, with procedure-name-1 the last paragraph: control
      *>   runs off the end of the procedure division and never reaches the
      *>   return mechanism, so the PERFORM never returns.
      *> cite.py --check 14.9.28.4 "If procedure-name-2 is specified, the
      *>   return mechanism is after the last statement of procedure-name-2"
      *>   -> OK  14.9.28.4 5) b)
      *> cite.py --check 14.9.28.4 "There is no necessary relationship between
      *>   procedure-name-1 and procedure-name-2 except that a consecutive
      *>   sequence of operations is to be executed beginning at the procedure
      *>   named procedure-name-1 and ending with the execution of the
      *>   procedure named procedure-name-2" -> OK  14.9.28.4 6)
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> Derivation: PERFORM P2 THRU P1 starts at P2 (P2), which is the last
      *>   paragraph; the return mechanism after P1's last statement is never
      *>   reached, so AFTER-PERFORM is not displayed under any reading. What
      *>   ends the run is the docs/CONFORMANCE.md D-PTHRU determination (the
      *>   implicit GOBACK, GnuCOBOL 3.2's behaviour), and P1 never runs. How it
      *>   can fail: a dispatcher that reads running off the end of the
      *>   procedure division as the range completing displays AFTER-PERFORM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511TH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM P2 THRU P1
           DISPLAY "AFTER-PERFORM"
           STOP RUN.
       P1.
           DISPLAY "P1".
       P2.
           DISPLAY "P2".
