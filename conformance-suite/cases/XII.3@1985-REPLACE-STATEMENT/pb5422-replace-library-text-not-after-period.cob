      *>--- conformance-case
      *> id: pb5422-replace-library-text-not-after-period
      *> rules: [1985-SR-XII.3.3-1]
      *> files: [PB5422LB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR1 (cite.py --check: OK): a REPLACE statement "must be
      *> preceded by a separator period except when it is the first statement in
      *> a separately compiled program". PB5422LB begins with a REPLACE, and the
      *> COPY statement that brings it follows DISPLAY "X" with no period; the
      *> COPY statement is not part of the resultant text, so the REPLACE
      *> follows "X": refused at 85 (COBOL0900). The twin whose COPY follows a
      *> separator period is pb5422-replace-library-text-after-period
      *> (kb/Work PB5422).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5422N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "X" COPY PB5422LB.
           DISPLAY AAA.
           STOP RUN.
