      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_2-p1
      *> rules: [1985-GR-IV.4.4.2-p1, 1985-GR-IV.4.4.2-p4, 1985-GR-IV.4.4.2-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.2 p1; §1985:IV.4.4.2 p4; §1985:IV.4.4.2 p6
      *> (EXPLICIT AND IMPLICIT SPECIFICATIONS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.4.2-p1/p4/p6/L2.5/p7: statements run in written
      * order; GO TO and IF transfer explicitly; ALTER by itself does
      * not transfer, the altered GO TO does; EXIT PROGRAM in the
      * main (not called) program transfers nothing; a called program
      * that reaches its end program header, or has no Procedure
      * Division, executes an implicit EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05FLOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N           PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A1".
           DISPLAY "A2".
           ALTER P2 TO PROCEED TO P4.
           DISPLAY "A3-AFTER-ALTER".
       P2.
           GO TO P3.
       P3.
           DISPLAY "NOT-P3".
       P4.
           DISPLAY "P4".
           IF N = 0 GO TO P5.
           DISPLAY "NOT-IF".
       P5.
           CALL "S05FEND".
           DISPLAY "BACK-FROM-END".
           CALL "S05FNOP".
           DISPLAY "BACK-FROM-NOPD".
           EXIT PROGRAM.
           DISPLAY "AFTER-EXIT-PROGRAM".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05FEND.
       PROCEDURE DIVISION.
       Q1.
           DISPLAY "IN-FEND".
       END PROGRAM S05FEND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05FNOP.
       END PROGRAM S05FNOP.
       END PROGRAM S05FLOW.
