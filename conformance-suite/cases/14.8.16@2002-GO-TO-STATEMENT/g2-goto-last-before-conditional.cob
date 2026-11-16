      *>--- conformance-case
      *> id: g2-goto-last-before-conditional
      *> rules: [2002-SR-14.8.16.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR2 - "If a GO TO statement represented by
      *>   format 1 appears in a consecutive sequence of imperative
      *>   statements within a sentence, it shall appear as the last
      *>   statement in that sequence."
      *> §2002:14.4 - "Any statement with a conditional phrase that is
      *>   not terminated by its explicit scope terminator is a
      *>   conditional statement"; table 13 gives CALL the conditional
      *>   phrase ON OVERFLOW and RECEIVE the phrase NO DATA (both
      *>   2002-only forms). A conditional statement ends the imperative
      *>   sequence, so each GO TO below is the LAST statement of its
      *>   sequence and the sentence is legal.
      *> P0: DISPLAY, GO TO, then CALL ... ON OVERFLOW (conditional).
      *> P1: DISPLAY, GO TO, then RECEIVE ... NO DATA (conditional; a CD
      *>     FOR INPUT, obsolete at 2002 - COBOL0903 warnings expected).
      *> P2: DISPLAY, GO TO, then IF without END-IF (conditional).
      *> P3: DISPLAY, MOVE, GO TO last in the sentence.
      *> DERIVATION (§2002:14.8.16.3 GR1, "control is transferred to
      *>   procedure-name-1"): each GO TO runs after the statements before
      *>   it and the conditional statement after it never runs: L1 P1 P2
      *>   P3, then "P4 2" (the MOVE before the P3 GO TO ran).
      *> CAN FAIL: a compiler that ends the sequence only at a period, or
      *>   that treats CALL ON OVERFLOW or RECEIVE NO DATA as imperative,
      *>   refuses this program (COBOL2967); OVF, ND or IF in the output
      *>   means a GO TO did not transfer control. The refused sides are
      *>   g2-goto-not-last-send, -exit-function and -scope-terminated-call.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOTO01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X     PIC X(4) VALUE SPACES.
       01  WS-N  PIC 9    VALUE 1.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "L1" GO TO P1 CALL "NOSUCHPGM" ON OVERFLOW
               DISPLAY "OVF".
       P1.
           DISPLAY "P1" GO TO P2 RECEIVE CDIN MESSAGE INTO X
               NO DATA DISPLAY "ND".
       P2.
           DISPLAY "P2" GO TO P3 IF WS-N = 1 DISPLAY "IF".
       P3.
           DISPLAY "P3" MOVE 2 TO WS-N GO TO P4.
       P4.
           DISPLAY "P4 " WS-N.
           STOP RUN.
