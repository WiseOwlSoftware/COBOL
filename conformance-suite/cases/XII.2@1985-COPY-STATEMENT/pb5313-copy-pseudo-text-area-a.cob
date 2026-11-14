      *>--- conformance-case
      *> id: pb5313-copy-pseudo-text-area-a
      *> rules: [1985-GR-XII.2.4-9, 1985-GR-XII.2.4-10]
      *> accept: [85]
      *> files: [PB5313LB.cpy, PB5313LC.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR9 (cite.py --check: OK): "Each text word in
      *> pseudo-text-2 that is to be placed into the resultant program
      *> begins in the same area of the resultant program as it appears in
      *> pseudo-text-2", and GR10 places the words "according to the rules
      *> for reference format". The library words XX and YY begin in area B;
      *> the paragraph headers that replace them begin in area A of
      *> pseudo-text-2 - P-1 on a line of its own, P-2 after the opening
      *> delimiter in area A - so each begins its resultant line in area A
      *> and the program is legal at 85 (kb/Work PB5313). Expected output:
      *> PERFORM P-1 displays IN Q, PERFORM P-2 displays IN R.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5313C.
       PROCEDURE DIVISION.
       P0.
           PERFORM P-1.
           PERFORM P-2.
           STOP RUN.
       P-0.
           COPY PB5313LB REPLACING ==XX== BY ==DISPLAY "P0".
       P-1==.
           COPY PB5313LC REPLACING ==YY== BY
       ==P-2.==.
