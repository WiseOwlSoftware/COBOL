      *>--- conformance-case
      *> id: g85_f26_02
      *> rules: [1985-GR-X.5.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.4 GR1 - "If the EXIT PROGRAM statement is executed
      *>   in a program which is not under the control of a calling
      *>   program, the EXIT PROGRAM statement causes execution of the
      *>   program to continue with the next executable statement."
      *>   cite.py: OK  §1985:X.5.4.4 GR1
      *> G85F2602 is the run unit's first program: no CALL reached it.
      *> Each EXIT PROGRAM is the last statement of its sequence
      *> (§1985:X.5.4.3 SR1, cite.py: OK), so the program is legal.
      *> Derivation, by line:
      *>  1 BEFORE   - before the first EXIT PROGRAM.
      *>  2 AFTER 1  - EXIT PROGRAM in its own sentence continues with
      *>               the next sentence (a compiler that returns or
      *>               stops prints nothing more).
      *>  3 IN IF    - the IF's DISPLAY precedes EXIT PROGRAM.
      *>  4 AFTER 2  - EXIT PROGRAM ending an IF continues after it.
      *>  5 IN P-2   - EXIT PROGRAM inside a PERFORMed paragraph
      *>               continues in that paragraph.
      *>  6 AFTER 3  - the PERFORM still returns at the end of P-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2602.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-1.
           DISPLAY "BEFORE".
           EXIT PROGRAM.
           DISPLAY "AFTER 1".
           MOVE 1 TO WS-N.
           IF WS-N = 1
               DISPLAY "IN IF"
               EXIT PROGRAM.
           DISPLAY "AFTER 2".
           PERFORM P-2.
           DISPLAY "AFTER 3".
           STOP RUN.
       P-2.
           EXIT PROGRAM.
           DISPLAY "IN P-2".
