      *>--- conformance-case
      *> id: pb5924-pseudo-text-continuation-85
      *> rules: [1985-SR-IV.7.2.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.5 (cite.py: OK):
      *> "the normal rules for continuation of lines apply"
      *> A hyphen line inside pseudo-text-2 joins WS-AB and CD into the one
      *> text word WS-ABCD, which DISPLAY then shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PSCL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ABCD PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       P1.
           REPLACE ==SHOW-IT== BY
                                                         ==DISPLAY WS-AB
      -    CD==.
           SHOW-IT.
           STOP RUN.
