      *>--- conformance-case
      *> id: pb5924-rel-usage-comp-alnum-85
      *> rules: [1985-SR-VI.6.3.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.1 (cite.py: OK): "for all other
      *> comparisons, the operands must have the same usage"
      *> A numeric item of usage COMPUTATIONAL compared with an
      *> alphanumeric item of usage DISPLAY: not two numeric operands,
      *> and their usages differ: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NC PIC 99 COMP VALUE 12.
       01  AX PIC XX VALUE "12".
       PROCEDURE DIVISION.
       P0.
           IF NC = AX DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
