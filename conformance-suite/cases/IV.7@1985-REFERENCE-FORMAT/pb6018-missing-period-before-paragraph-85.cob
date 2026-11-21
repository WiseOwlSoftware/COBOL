      *>--- conformance-case
      *> id: pb6018-missing-period-before-paragraph-85
      *> rules: [1985-SR-IV.7.3.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). So P1, which begins its line in
      *> area A and is followed by the period, is the next
      *> paragraph-name ("The paragraph header or paragraph-name starts
      *> in area A", §1985:IV.7.3.3), and the MOVE sentence lacks its
      *> period: ONE report, at P1. It drew COBOL3735 "'P1' begins in
      *> area A" and COBOL1639 "'P1' is not defined", the MOVE having
      *> read P1 as a second receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOPERP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           MOVE "B" TO A
       P1.
           DISPLAY A.
           STOP RUN.
