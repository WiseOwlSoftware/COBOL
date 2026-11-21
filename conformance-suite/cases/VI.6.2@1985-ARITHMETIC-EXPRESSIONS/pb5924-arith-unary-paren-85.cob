      *>--- conformance-case
      *> id: pb5924-arith-unary-paren-85
      *> rules: [1985-SR-VI.6.2.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.2 (cite.py: OK): "must be preceded
      *> by a space and followed by a space"
      *> The 1985 text has no parenthesis exception (2023 8.7.1 adds
      *> one): a unary '-' followed by '(' with no space is refused.
      *> Refused COBOL2993 since landed PB5989 (was a DEFECT at mint).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S9(3) VALUE 5.
       01  M  PIC S9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = -(M) + 9.
           STOP RUN.
