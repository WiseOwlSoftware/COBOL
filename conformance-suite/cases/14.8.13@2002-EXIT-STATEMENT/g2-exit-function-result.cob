      *>--- conformance-case
      *> id: g2-exit-function-result
      *> rules: [2002-GR-14.8.13.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR8 - "The execution of an EXIT FUNCTION
      *>   statement causes the executing function to terminate, and
      *>   control to return to the activating statement. The value in
      *>   the data item referenced by the RETURNING phrase in the
      *>   containing function definition becomes the result of the
      *>   function activation."
      *> §2002:14.8.13.2 SR9 - "An EXIT FUNCTION statement may be
      *>   specified only in a function procedure division": both are
      *>   in FUNCTION-ID source elements.
      *> DERIVATION of every .out line:
      *>   R1=07   G2XF1 moves 7 to RES and executes EXIT FUNCTION; the
      *>           function terminates, so MOVE 99 and G2XF1-NEVER do not
      *>           run, and RES (7) is the result.
      *>   R2=08   the activating statement is the COMPUTE: control
      *>           returns into it with the result 7, then + 1.
      *>   R3=08   G2XF2 with argument 8: N > 5, so RES = 8 and the EXIT
      *>           FUNCTION inside the IF terminates the function before
      *>           MOVE 0 TO RES.
      *>   R4=00   G2XF2 with argument 3: the IF is false, MOVE 0 runs and
      *>           the function ends at its end; result 0.
      *>   DONE    control returned to the activating program each time.
      *> CAN FAIL: an EXIT FUNCTION that ran on (R1=99, R3=00 or a NEVER
      *>   line), a lost RETURNING value (R1=00), or control not
      *>   returning to the activating COMPUTE (R2 wrong or missing).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2XF1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(2).
       PROCEDURE DIVISION RETURNING RES.
       P1.
           MOVE 7 TO RES.
           EXIT FUNCTION.
           MOVE 99 TO RES.
           DISPLAY "G2XF1-NEVER".
       END FUNCTION G2XF1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2XF2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N   PIC 9(2).
       01 RES PIC 9(2).
       PROCEDURE DIVISION USING N RETURNING RES.
       P1.
           IF N > 5
               MOVE N TO RES
               EXIT FUNCTION
           END-IF.
           MOVE 0 TO RES.
       END FUNCTION G2XF2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2XFMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2XF1
           FUNCTION G2XF2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W  PIC 9(2).
       01 A8 PIC 9(2) VALUE 8.
       01 A3 PIC 9(2) VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION G2XF1 TO W.
           DISPLAY "R1=" W.
           COMPUTE W = FUNCTION G2XF1 + 1.
           DISPLAY "R2=" W.
           MOVE FUNCTION G2XF2 (A8) TO W.
           DISPLAY "R3=" W.
           MOVE FUNCTION G2XF2 (A3) TO W.
           DISPLAY "R4=" W.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM G2XFMAIN.
