      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_3-p1
      *> rules: [1985-GR-IV.4.4.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.3 p1 (EXPLICIT AND IMPLICIT SPECIFICATIONS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.4.3-p1: an attribute not specified explicitly
      * takes its default: N has no USAGE clause, so its usage is
      * DISPLAY, the same as the explicit USAGE DISPLAY of M; the
      * group G shows both stored as display characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05ATTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  N       PIC 9(3) VALUE 123.
           05  M       PIC 9(3) USAGE IS DISPLAY VALUE 456.
       01  H REDEFINES G PIC X(6).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "G=" G.
           IF H = "123456" DISPLAY "DISPLAY-USAGE".
           STOP RUN.
