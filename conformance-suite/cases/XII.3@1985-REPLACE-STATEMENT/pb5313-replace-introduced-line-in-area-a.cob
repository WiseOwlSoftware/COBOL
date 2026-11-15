      *>--- conformance-case
      *> id: pb5313-replace-introduced-line-in-area-a
      *> rules: [1985-GR-XII.3.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR10 (cite.py --check: OK): text words a REPLACE
      *> inserts "are placed in the source program according to the rules
      *> for reference format", and the program is judged only after the
      *> REPLACE is processed (GR9). Pseudo-text-2 writes the paragraph
      *> header P2 in area A of a line of its own, so the resultant program
      *> holds P2 at the start of a line, in area A: legal at 85. The words
      *> keep the areas they were written in, as §1985:XII.2.4 GR9 says of
      *> COPY's pseudo-text-2 (kb/Work PB5313; the area is that of the
      *> place pseudo-text-2 writes the word, not of the XXX it replaces).
      *> Expected output: P1 displays A and the replacement's B; control
      *> falls into P2, which displays D and C and stops: A, B, D, C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5313R.
       PROCEDURE DIVISION.
           REPLACE ==XXX== BY ==DISPLAY "B".
       P2.
           DISPLAY "D".==.
       P1.
           DISPLAY "A".
           XXX
           DISPLAY "C".
           STOP RUN.
