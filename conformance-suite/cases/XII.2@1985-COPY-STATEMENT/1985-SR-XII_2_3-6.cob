      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-6
      *> rules: [1985-SR-XII.2.3-6]
      *> files: [PB5085LB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR6 (cite.py --check: OK): "Word-1 or word-2 may be any
      *> single COBOL word except 'COPY'." The REPLACING phrase below names COPY
      *> as word-1, so the program is not conforming: refused (COBOL7006) at 85,
      *> and at 2002 and 2014 whose text keeps the rule (§2002:7.1.2.2 SR10,
      *> §2014:7.2.2.2 SR10). 2023 removed the word operand forms (COBOL0902).
      *> The library text PB5085LB holds one line, DISPLAY "OK". (kb/Work PB5085)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5085CW.
       PROCEDURE DIVISION.
       P1.
           COPY PB5085LB REPLACING COPY BY DISPLAY.
           STOP RUN.
