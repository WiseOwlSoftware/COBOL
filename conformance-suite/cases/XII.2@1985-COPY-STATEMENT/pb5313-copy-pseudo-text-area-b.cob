      *>--- conformance-case
      *> id: pb5313-copy-pseudo-text-area-b
      *> rules: [1985-GR-XII.2.4-9]
      *> files: [PB5313LA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR9 (cite.py --check: OK): "Each text word in
      *> pseudo-text-2 that is to be placed into the resultant program
      *> begins in the same area of the resultant program as it appears in
      *> pseudo-text-2". The library word XX begins in area A, but the
      *> paragraph header P-1 that replaces it is written in area B of
      *> pseudo-text-2, so the resultant program holds a paragraph header in
      *> area B: refused at 85 (COBOL3734, the introduction gate of the 2002
      *> removal of the areas; kb/Work PB5313). The accepted twin is
      *> pb5313-copy-pseudo-text-area-a.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5313B.
       PROCEDURE DIVISION.
       P0.
           PERFORM P-1.
           STOP RUN.
           COPY PB5313LA REPLACING ==XX== BY ==P-1==.
