      *>--- conformance-case
      *> id: pb5924-pd-body-leading-sentence-85
      *> rules: [1985-SR-IV.6.4.1.4.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.1.4.2 (cite.py: OK): "The body of the Procedure Division must conform to one of the following formats"
      *> A sentence precedes the first paragraph-name: the body matches neither format 1 (sections)
      *> nor format 2 ({paragraph-name. [sentence]...}...): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PDLEAD.
       PROCEDURE DIVISION.
           DISPLAY "LEAD".
       P1.
           DISPLAY "P1".
           STOP RUN.
