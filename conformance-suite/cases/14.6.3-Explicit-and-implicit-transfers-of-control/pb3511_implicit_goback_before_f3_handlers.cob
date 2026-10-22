*>--- conformance-case
*> id: pb3511_implicit_goback_before_f3_handlers
*> rules: [GR-14.6.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - the implicit GOBACK past the last paragraph of a program that
      *>   also holds an exception-checking PERFORM (format 3, COBOL 2023): the
      *>   end of the source element is its last paragraph, whatever the
      *>   PERFORM's WHEN phrases add, so control that leaves an out-of-line
      *>   PERFORM's range by GO TO and runs past that paragraph ends the run.
      *> cite.py --check 14.6.3 "There is also no next executable statement
      *>   after the last statement in a source element when the procedure in
      *>   which it appears is not being executed under the control of some
      *>   other COBOL statement in that source element" -> OK  14.6.3 3)
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> Derivation: PERFORM P1 displays P1 and its GO TO P3 leaves the range;
      *>   P3 displays P3 and is the last paragraph, so the implicit GOBACK of a
      *>   main program ends the run (14.9.18.4 GR3): AFTER is never displayed
      *>   and the exception-checking PERFORM never executes, so neither
      *>   F3-BODY nor F3-WHEN is displayed. How it can fail: returning to the
      *>   PERFORM displays AFTER and F3-BODY; running on past P3 into the WHEN
      *>   phrase's code displays F3-WHEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511F3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM P1
           DISPLAY "AFTER"
           PERFORM
               DISPLAY "F3-BODY"
           WHEN EC-ALL
               DISPLAY "F3-WHEN"
           END-PERFORM
           STOP RUN.
       P1.
           DISPLAY "P1"
           GO TO P3.
       P2.
           DISPLAY "P2".
       P3.
           DISPLAY "P3".
