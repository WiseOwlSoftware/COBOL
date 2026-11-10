      *>--- conformance-case
      *> id: 1985-GR-VI_6_23_4-2
      *> rules: [1985-GR-VI.6.23.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR2 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETIDX.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-DATA PIC X(5) VALUE "ABCDE".
       01  T-TAB REDEFINES T-DATA.
           05  T-EL PIC X OCCURS 5 TIMES INDEXED BY IX1 IX2.
       PROCEDURE DIVISION.
       P-MAIN.
           SET IX1 TO 3.
           DISPLAY T-EL (IX1).
           SET IX2 TO IX1.
           DISPLAY T-EL (IX2).
           SET IX2 UP BY 2.
           DISPLAY T-EL (IX2).
           SET IX2 DOWN BY 4.
           DISPLAY T-EL (IX2).
           STOP RUN.
