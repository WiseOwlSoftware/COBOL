      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_6-p1
      *> rules: [1985-SR-IV.4.3.8.6-p1, 1985-SR-IV.4.3.8.6-p4, 1985-SR-IV.4.3.8.6-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.6 p1; §1985:IV.4.3.8.6 p4; §1985:IV.4.3.8.6 p5
      *> (Uniqueness of Reference). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.4.3.8.6-p3/p4/p5: a condition-name of a subscripted
      * conditional variable takes the same subscripting, qualified
      * and subscripted as an identifier (condition-name-1 in place of
      * data-name-1), in IF and in SET ... TO TRUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05CSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  E   OCCURS 2 TIMES.
               10  V   PIC X OCCURS 3 TIMES.
                   88  V-ON  VALUE "Y".
       01  G2.
           05  V       PIC X VALUE "N".
               88  V-ON  VALUE "Y".
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "N" TO G1.
           SET V-ON OF V IN E OF G1 (2 3) TO TRUE.
           SET V-ON IN G2 TO TRUE.
           IF V-ON OF G1 (2 3) DISPLAY "G1-2-3-ON".
           IF NOT V-ON IN V OF G1 (1 3) DISPLAY "G1-1-3-OFF".
           IF V-ON OF V OF G2 DISPLAY "G2-ON".
           DISPLAY "G1=" G1.
           STOP RUN.
