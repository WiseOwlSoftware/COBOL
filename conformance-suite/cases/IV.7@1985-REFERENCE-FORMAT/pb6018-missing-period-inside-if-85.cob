      *>--- conformance-case
      *> id: pb6018-missing-period-inside-if-85
      *> rules: [1985-SR-IV.7.3.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). A sentence that ends inside a
      *> nested statement: the IF's MOVE would read P1 as a receiver.
      *> P1 begins in area A and is followed by the period, so it is
      *> the next paragraph-name and the IF sentence lacks its period:
      *> ONE report, at P1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPIF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           IF A = "A" MOVE "B" TO A
       P1.
           DISPLAY A.
           STOP RUN.
