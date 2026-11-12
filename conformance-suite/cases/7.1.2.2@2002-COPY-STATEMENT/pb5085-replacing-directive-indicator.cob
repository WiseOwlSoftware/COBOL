      *>--- conformance-case
      *> id: pb5085-replacing-directive-indicator
      *> rules: [2002-SR-7.1.2.2-10]
      *> files: [PB5085LD.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR10 (cite.py --check: OK): "Word-1 or word-2 may be any
      *> single COBOL word except 'COPY', the compiler directive indicator, the
      *> floating debug indicator, or the comment indicator." The REPLACING
      *> phrase below names the compiler directive indicator as word-1, so the
      *> program is not conforming: refused (COBOL7006) at 2002 and at 2014
      *> (§2014:7.2.2.2 SR10 keeps the directive indicator). 85 has no such
      *> indicator as a word, and 2023 removed the word operand forms. The
      *> library text PB5085LD holds one line, DISPLAY "OK". (kb/Work PB5085)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5085DI.
       PROCEDURE DIVISION.
       P1.
           COPY PB5085LD REPLACING >> BY DISPLAY.
           STOP RUN.
