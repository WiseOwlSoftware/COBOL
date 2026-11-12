      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-8
      *> rules: [1985-GR-VI.6.18.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR8 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPBA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W  PIC X(9).
       01  W2 PIC X(7) VALUE "XAYAXAY".
       01  W3 PIC X(5).
       01  C1 PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCABCABC" TO W.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A".
           DISPLAY "T1 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A" BEFORE "C".
           DISPLAY "T2 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A" AFTER "C".
           DISPLAY "T3 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A" BEFORE "Z".
           DISPLAY "T4 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A" AFTER "Z".
           DISPLAY "T5 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR CHARACTERS BEFORE "B".
           DISPLAY "T6 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR CHARACTERS AFTER "B".
           DISPLAY "T7 " C1.
           MOVE 0 TO C1.
           INSPECT W2 TALLYING C1 FOR ALL "A" AFTER "X" BEFORE "Y".
           DISPLAY "T8 " C1.
           MOVE 0 TO C1.
           INSPECT W TALLYING C1 FOR ALL "A" BEFORE "CA".
           DISPLAY "T9 " C1.
           INSPECT W REPLACING ALL "A" BY "Z" AFTER "C".
           DISPLAY "R1 " W.
           MOVE "ABCABCABC" TO W.
           INSPECT W REPLACING CHARACTERS BY "*" BEFORE "C".
           DISPLAY "R2 " W.
           MOVE "AACAC" TO W3.
           INSPECT W3 REPLACING ALL "A" BY "C" BEFORE "C".
           DISPLAY "R3 " W3.
           MOVE "ABABA" TO W3.
           INSPECT W3 REPLACING ALL "A" BY "Q" AFTER "Z".
           DISPLAY "R4 " W3.
           STOP RUN.
