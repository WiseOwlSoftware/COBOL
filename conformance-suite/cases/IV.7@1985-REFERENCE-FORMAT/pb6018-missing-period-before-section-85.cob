      *>--- conformance-case
      *> id: pb6018-missing-period-before-section-85
      *> rules: [1985-SR-IV.7.3.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). "The section header must start
      *> in area A" (§1985:IV.7.3.2, cite.py: OK), so S1 SECTION in
      *> area A is the next section header and the MOVE sentence lacks
      *> its period: ONE report, at S1 (it was reported at SECTION, the
      *> MOVE having read S1 as a receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPSECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       S0 SECTION.
       P0.
           MOVE "B" TO A
       S1 SECTION.
       P1.
           DISPLAY A.
           STOP RUN.
