      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-9
      *> rules: [1985-GR-X.5.2.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR9 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9(2) VALUE 5.
       01  B PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPGM" USING A B.
           DISPLAY "B=" B.
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  X PIC 9(2).
       01  Y PIC 9(2).
       PROCEDURE DIVISION USING X Y.
       P2.
           ADD X X GIVING Y.
       P3.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
