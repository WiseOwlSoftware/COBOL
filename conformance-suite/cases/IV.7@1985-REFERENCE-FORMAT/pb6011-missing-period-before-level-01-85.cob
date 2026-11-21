      *>--- conformance-case
      *> id: pb6011-missing-period-before-level-01-85
      *> rules: [1985-SR-IV.7.4-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6011. §1985:IV.7.4 (cite.py: OK): "The last clause is
      *> always terminated by a separator period", and an entry continued on
      *> a later line goes on in area B (§1985:IV.7.3.3, cite.py: OK:
      *> "Successive sentences or entries either begin in area B"). So the 01
      *> that begins line 6 in area A is the next entry's level-number and
      *> the entry A lacks its period: ONE report, at the 01, where the
      *> period is missing. It drew COBOL3735 "'01' begins in area A" and
      *> four cascades of the VALUE list that read 01 and B as operands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOPER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A"
       01  B PIC X.
       PROCEDURE DIVISION.
       P0.
           DISPLAY A B.
           STOP RUN.
