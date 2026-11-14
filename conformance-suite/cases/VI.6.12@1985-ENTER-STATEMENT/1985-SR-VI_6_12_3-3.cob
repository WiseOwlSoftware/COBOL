      *>--- conformance-case
      *> id: 1985-SR-VI_6_12_3-3
      *> rules: [1985-SR-VI.6.12.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.3 SR3 (THE ENTER STATEMENT): "The sentence ENTER
      *> COBOL must follow the last other-language statement". A refusal:
      *> the compiler has no other-language mode, so under WiseOwl's choice
      *> not to provide ENTER (§1985:I.1.5.2.5.5) both ENTER sentences below
      *> are refused at 85 with COBOL7240, and no other-language text is ever
      *> compiled in-line (kb/Work PB5308).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENTTST.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE".
           GO TO P3.
       P2.
           ENTER FORTRAN SUBRTN.
           ENTER COBOL.
           DISPLAY "RETURNED".
       P3.
           DISPLAY "AFTER".
           STOP RUN.
