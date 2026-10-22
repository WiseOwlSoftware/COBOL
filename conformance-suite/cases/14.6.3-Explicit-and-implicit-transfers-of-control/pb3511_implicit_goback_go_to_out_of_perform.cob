*>--- conformance-case
*> id: pb3511_implicit_goback_go_to_out_of_perform
*> rules: [GR-14.6.3-3, GR-14.9.28.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - ISO 14.6.3: running past the last statement of the source
      *>   element when that procedure is not under the control of another
      *>   statement executes an implicit GOBACK; it never returns to a
      *>   PERFORM whose range control has left.
      *> cite.py --check 14.6.3 "There is also no next executable statement
      *>   after the last statement in a source element when the procedure in
      *>   which it appears is not being executed under the control of some
      *>   other COBOL statement in that source element" -> OK  14.6.3 3)
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> cite.py --check 14.9.28.4 "The range of a PERFORM statement consists
      *>   logically of all those statements that are executed as a result of
      *>   executing the PERFORM statement through execution of the implicit
      *>   transfer of control to the end of the PERFORM statement"
      *>   -> OK  14.9.28.4 1)
      *> Derivation: PERFORM P1 displays P1, whose GO TO P3 leaves the range
      *>   (P1's end is never reached, so the PERFORM never returns). P3 is not
      *>   under the PERFORM's control; after its last statement the implicit
      *>   GOBACK of a main program operates as STOP (14.9.18.4 GR3), so
      *>   AFTER-PERFORM is never displayed. How it can fail: a dispatcher that
      *>   reads the fall-off at the end of the procedure division as the
      *>   PERFORM's completion displays AFTER-PERFORM (and P2 is never shown).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511GO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM P1
           DISPLAY "AFTER-PERFORM"
           STOP RUN.
       P1.
           DISPLAY "P1"
           GO TO P3.
       P2.
           DISPLAY "P2".
       P3.
           DISPLAY "P3".
