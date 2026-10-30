      *>--- conformance-case
      *> id: pb3845-nbsp-word-unparseable
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3845 - ISO 8.3.2.1 / 8.1.3.2 GR4 / Annex B: the line below holds a no-break space (U+00A0), which
      *> is no character of an Annex B word. The lexer reads it as a word (so the diagnostic can name the
      *> character), the grammar can place no word there, and the parse fails at the NEXT token: the screen of the
      *> word characters must name U+00A0 all the same, from COBOL 2002 (the first Annex B). COBOL-85 has no extended
      *> letters, so there the same word is the 2002 introduction gate (pb3845-extended-letter-unparseable-85).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3845A.
000300 PROCEDURE DIVISION.
000400   
000500     STOP RUN.
