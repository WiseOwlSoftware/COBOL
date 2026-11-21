      *>--- conformance-case
      *> id: pb6018-missing-period-after-go-to-85
      *> rules: [1985-SR-IV.7.3.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6018. §1985:IV.7.3.3 (cite.py: OK): "Successive
      *> sentences or entries either begin in area B", and a sentence
      *> is continued "by starting subsequent line(s) in area B"
      *> (§1985:IV.7.2.2, cite.py: OK). GO TO P2 is Format 1; P1,
      *> beginning its line in area A and followed by the period, is the
      *> next paragraph-name, never a second procedure-name of Format
      *> 2: ONE report, at P1 (it drew COBOL2172, a Format 2 GO TO
      *> lacking DEPENDING, at the period).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPGOTO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           GO TO P2
       P1.
           DISPLAY A.
       P2.
           STOP RUN.
