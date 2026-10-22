*>--- conformance-case
*> id: pb3511_implicit_goback_called_program
*> rules: [GR-14.6.3-3, GR-14.9.18.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB3511 - the implicit GOBACK past the last paragraph of a CALLED
      *>   program returns to the CALL statement (14.9.18.4 GR2), even while
      *>   a PERFORM in the called program is active; it never returns to that
      *>   PERFORM. The second CALL shows the next activation starts clean.
      *> cite.py --check 14.6.3 "an implicit GOBACK statement without any
      *>   optional phrases is executed" -> OK  14.6.3 3)
      *> cite.py --check 14.9.18.4 "The execution of an GOBACK statement causes
      *>   the executing program to terminate, and control to return to the
      *>   calling statement" -> OK  14.9.18.4 2)
      *> Derivation: each CALL runs SA, whose PERFORM P1 displays SUB-P1; its
      *>   GO TO P3 leaves the range and P3 (SUB-P3) is the last paragraph, so
      *>   the implicit GOBACK returns to the CALL: SUB-AFTER is never shown,
      *>   and MAIN-BACK / MAIN-BACK2 follow. Q4 does the same through an EXIT
      *>   PARAGRAPH in the last paragraph (14.9.14.4 GR6 ends the paragraph,
      *>   which is the end of the source element): Q4-SKIPPED and Q4-AFTER are
      *>   never shown. How it can fail: returning to the PERFORM displays
      *>   SUB-AFTER (or Q4-AFTER) before the CALL returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511CM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3511CS"
           DISPLAY "MAIN-BACK"
           CALL "PB3511CS"
           DISPLAY "MAIN-BACK2"
           CALL "PB3511CX"
           DISPLAY "MAIN-BACK3"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511CS.
       PROCEDURE DIVISION.
       SA.
           PERFORM P1
           DISPLAY "SUB-AFTER"
           GOBACK.
       P1.
           DISPLAY "SUB-P1"
           GO TO P3.
       P2.
           DISPLAY "SUB-P2".
       P3.
           DISPLAY "SUB-P3".
       END PROGRAM PB3511CS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3511CX.
       PROCEDURE DIVISION.
       XA.
           PERFORM Q1
           DISPLAY "Q4-AFTER"
           GOBACK.
       Q1.
           DISPLAY "Q1"
           GO TO Q4.
       Q2.
           DISPLAY "Q2".
       Q4.
           DISPLAY "Q4"
           EXIT PARAGRAPH
           DISPLAY "Q4-SKIPPED".
       END PROGRAM PB3511CX.
       END PROGRAM PB3511CM.
