*>--- conformance-case
*> id: pb3511_implicit_goback_two_performs_deep
*> rules: [GR-14.6.3-3, GR-14.9.28.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - ISO 14.6.3: the implicit GOBACK past the last paragraph ends
      *>   the program whatever depth of PERFORM statements is active; neither
      *>   active PERFORM returns.
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> cite.py --check 14.9.28.4 "The range of a PERFORM statement consists
      *>   logically of all those statements that are executed as a result of
      *>   executing the PERFORM statement through execution of the implicit
      *>   transfer of control to the end of the PERFORM statement"
      *>   -> OK  14.9.28.4 1)
      *> Derivation: PERFORM A1 runs A1 (A1), which PERFORMs B1 (B1); B1's GO
      *>   TO LAST-P leaves both ranges, so neither A1's nor B1's end is ever
      *>   reached. LAST-P displays LAST and is the last paragraph: the implicit
      *>   GOBACK ends the run (14.9.18.4 GR3). How it can fail: returning to
      *>   the innermost PERFORM displays AFTER-B1, then A1 completes and
      *>   AFTER-A1 is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511DP.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM A1
           DISPLAY "AFTER-A1"
           STOP RUN.
       A1.
           DISPLAY "A1"
           PERFORM B1
           DISPLAY "AFTER-B1".
       B1.
           DISPLAY "B1"
           GO TO LAST-P.
       MID-P.
           DISPLAY "MID".
       LAST-P.
           DISPLAY "LAST".
