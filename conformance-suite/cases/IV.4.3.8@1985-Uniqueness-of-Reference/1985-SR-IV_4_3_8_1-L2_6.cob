      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-L2_6
      *> rules: [1985-SR-IV.4.3.8.1-L2.6]
      *> source-format: fixed
      *>---
      *> §1985:IV.4.3.8.1 SRL2.6: "If explicitly referenced, a
      *> paragraph-name must not be duplicated within a section." P1 is
      *> declared twice in section S1 (lines 20 and 22) and PERFORM P1
      *> on line 18 references it, so the program is refused; the
      *> message names both declarations at the lines that hold the
      *> paragraph-names, not at their first statements (kb/Work
      *> PB5091).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5091DP.
       PROCEDURE DIVISION.
       S1 SECTION.
       P0.
           PERFORM P1.
           STOP RUN.
       P1.
           DISPLAY "A".
       P1.
           DISPLAY "B".
