      *>--- conformance-case
      *> id: pb6018-statement-continued-in-area-b-85
      *> rules: [1985-SR-IV.7.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). The other side of the rule: the
      *> MOVE's receiver continued on the next line in AREA B is part
      *> of the sentence, and the paragraph-name after the period
      *> begins in area A as it must. MOVE "B" TO A then DISPLAY A
      *> shows B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AREAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           MOVE "B" TO
             A.
       P1.
           DISPLAY A.
           STOP RUN.
