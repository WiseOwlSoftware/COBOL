      *>--- conformance-case
      *> id: pb5085-replace-not-after-period
      *> rules: [1985-SR-XII.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR1 (cite.py --check: OK): a REPLACE statement "must be
      *> preceded by a separator period except when it is the first statement in
      *> a separately compiled program". Here it follows the literal "A" and a
      *> space, and it is not the program's first statement: refused at 85
      *> (COBOL0900, the introduction gate of the 2002 relaxation, constructs row
      *> replace-anywhere-2002). The 2002 text allows it: pb1358_replace_anywhere
      *> (kb/Work PB5085).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5085RP.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "A" REPLACE ==XX== BY =="B"==.
           STOP RUN.
