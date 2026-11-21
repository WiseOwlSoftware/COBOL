      *>--- conformance-case
      *> id: pb5924-continuation-quote-85
      *> rules: [1985-SR-IV.7.2.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "the first nonblank character in area B of the continuation line must be a quotation mark"
      *> A nonnumeric literal continued to column 72 resumes after the quotation
      *> mark that begins area B of the continuation line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONTQL.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ
      -    "END".
           STOP RUN.
