      *>--- conformance-case
      *> id: pb5924-pseudo-delimiter-one-line-85
      *> rules: [1985-SR-IV.7.2.2-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "Both characters composing the separator"
      *> REPLACE with == ending one line and == beginning the next replaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EQEQL.
       PROCEDURE DIVISION.
       P1.
                                                              REPLACE ==
           AAA== BY =="OK"==.
           DISPLAY AAA.
           STOP RUN.
