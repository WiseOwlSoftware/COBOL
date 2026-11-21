      *>--- conformance-case
      *> id: pb5924-continuation-no-quote-85
      *> rules: [1985-SR-IV.7.2.2-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "the first nonblank character in area B of the continuation line must be a quotation mark"
      *> A continued nonnumeric literal whose continuation line does not begin
      *> with a quotation mark is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONTQN.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ
      -    END".
           STOP RUN.
