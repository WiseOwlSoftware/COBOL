      *>--- conformance-case
      *> id: pb5924-pseudo-text-continuation-area-a-85
      *> rules: [1985-SR-IV.7.2.5-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.5 (cite.py: OK):
      *> "area A of the line must be blank"
      *> A hyphen line inside pseudo-text with text in area A is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PSCA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ABCD PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       P1.
           REPLACE ==SHOW-IT== BY
                                                         ==DISPLAY WS-AB
      - CD==.
           SHOW-IT.
           STOP RUN.
