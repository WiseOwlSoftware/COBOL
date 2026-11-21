      *>--- conformance-case
      *> id: pb5924-rel-usage-same-85
      *> rules: [1985-SR-VI.6.3.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.1 (cite.py: OK): "for all other
      *> comparisons, the operands must have the same usage"
      *> Two numeric operands of different usage (permitted regardless
      *> of USAGE), and a numeric and a nonnumeric operand both of usage
      *> DISPLAY: compile and compare.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NC PIC 99 COMP VALUE 12.
       01  ND PIC 99 VALUE 12.
       01  AX PIC XX VALUE "12".
       PROCEDURE DIVISION.
       P0.
           IF NC = ND DISPLAY "T1 Y" ELSE DISPLAY "T1 N".
           IF ND = AX DISPLAY "T2 Y" ELSE DISPLAY "T2 N".
           IF AX = "12" DISPLAY "T3 Y" ELSE DISPLAY "T3 N".
           STOP RUN.
