      *>--- conformance-case
      *> id: pb5087_bare_go_to_single_statement_paragraph
      *> rules: [FMT-14.9.17.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5087 - 1985 VI.6.15.3 SR3: "A format 1 GO TO
      *>   statement, without procedure-name-1, can only appear in a
      *>   single statement paragraph." P1 is that paragraph, and the
      *>   ALTER (VI.6.15.4 GR2) sets it to proceed to P2 before it
      *>   runs. Expected: "START" then "END". It fails if the
      *>   single-statement paragraph is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5087GT.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "START".
           ALTER P1 TO PROCEED TO P2.
       P1.
           GO TO.
       P2.
           DISPLAY "END".
           STOP RUN.
