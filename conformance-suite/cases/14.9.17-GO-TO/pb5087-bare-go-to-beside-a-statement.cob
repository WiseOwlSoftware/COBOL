      *>--- conformance-case
      *> id: pb5087-bare-go-to-beside-a-statement
      *> rules: [FMT-14.9.17.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5087 - 1985 VI.6.15.3 SR3: the target-less GO TO of
      *>   P1 shares its paragraph with DISPLAY "IN P1", so P1 is no
      *>   single statement paragraph. Expected: COBOL6818 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5087NGT.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "START".
       P1.
           DISPLAY "IN P1"
           GO TO.
       P2.
           DISPLAY "END".
           STOP RUN.
