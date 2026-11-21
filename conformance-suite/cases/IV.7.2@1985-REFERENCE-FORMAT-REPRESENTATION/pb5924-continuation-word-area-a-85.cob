      *>--- conformance-case
      *> id: pb5924-continuation-word-area-a-85
      *> rules: [1985-SR-IV.7.2.2-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "Area A of a continuation line must be blank"
      *> A continuation line that resumes a word in area A is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONTAW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ABCD PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       P1.
                                                           DISPLAY WS-AB
      - CD.
           STOP RUN.
