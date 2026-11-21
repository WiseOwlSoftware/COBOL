      *>--- conformance-case
      *> id: pb5924-pseudo-delimiter-split-85
      *> rules: [1985-SR-IV.7.2.2-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "Both characters composing the separator"
      *> An opening == whose two characters straddle a continued line and its
      *> continuation line is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EQEQS.
       PROCEDURE DIVISION.
       P1.
                                                               REPLACE =
      -    =AAA== BY =="OK"==.
           DISPLAY AAA.
           STOP RUN.
