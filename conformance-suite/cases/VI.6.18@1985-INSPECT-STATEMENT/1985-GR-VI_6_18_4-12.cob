      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-12
      *> rules: [1985-GR-VI.6.18.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR12 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TALLY85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D5  PIC X(5).
       01  C1  PIC 99.
       01  C2  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAXAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR ALL "A".
           DISPLAY "ALL-1 " C1.
           MOVE "AAAAA" TO D5.
           MOVE 10 TO C1.
           INSPECT D5 TALLYING C1 FOR ALL "AA".
           DISPLAY "ALL-2 " C1.
           MOVE "AAXAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR LEADING "A".
           DISPLAY "LEAD-1 " C1.
           MOVE "XAAAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR LEADING "A".
           DISPLAY "LEAD-2 " C1.
           MOVE "AAXAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR LEADING "A" AFTER INITIAL "X".
           DISPLAY "LEAD-3 " C1.
           MOVE "AXBAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR LEADING "A" AFTER INITIAL "X".
           DISPLAY "LEAD-4 " C1.
           MOVE "AABBA" TO D5.
           MOVE ZERO TO C1 C2.
           INSPECT D5 TALLYING C1 FOR LEADING "A"
                               C2 FOR LEADING "B".
           DISPLAY "LEAD-5 " C1 " " C2.
           MOVE "AAXAA" TO D5.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR CHARACTERS.
           DISPLAY "CHAR-1 " C1.
           MOVE ZERO TO C1 C2.
           INSPECT D5 TALLYING C1 FOR ALL "A"
                               C2 FOR CHARACTERS.
           DISPLAY "CHAR-2 " C1 " " C2.
           MOVE ZERO TO C1.
           INSPECT D5 TALLYING C1 FOR CHARACTERS BEFORE INITIAL "X".
           DISPLAY "CHAR-3 " C1.
           STOP RUN.
