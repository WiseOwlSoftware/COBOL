      *>--- conformance-case
      *> id: g85_f13_02
      *> rules: [1985-FMT-VI.6.14.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.14.2 - the general format of the EXIT statement is
      *> the bare word EXIT. The program uses it as the only sentence of
      *> two paragraphs (§1985:VI.6.14.3 SR1): one ends a PERFORM ... THRU
      *> range, the other is reached by falling through.
      *> Why each leg can fail: the program must translate at 85 (the
      *> format admitted); IN-P1 then AFTER-PERFORM shows the EXIT
      *> paragraph closed the PERFORM range and control came back;
      *> FELL-THROUGH shows control passed through the second EXIT
      *> paragraph into the next one (§1985:VI.6.14.4 GR1: no other
      *> effect on execution).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1302.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           PERFORM P1 THRU P1-X.
           DISPLAY "AFTER-PERFORM CNT=" CNT.
       P2-X.
           EXIT.
       P3.
           DISPLAY "FELL-THROUGH".
           STOP RUN.
       P1.
           ADD 1 TO CNT.
           DISPLAY "IN-P1".
       P1-X.
           EXIT.
