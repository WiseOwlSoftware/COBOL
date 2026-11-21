      *>--- conformance-case
      *> id: pb5993-replace-literal-operand-cites-1985-85
      *> rules: [1985-FMT-XII.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:XII.3.2 (cite.py: OK)
      *> prints Format 1 as REPLACE { ==pseudo-text-1== BY
      *> ==pseudo-text-2== } ..., so a literal operand is no REPLACE
      *> operand (COBOL1641). The refusal cites the 1985 general
      *> format, never §2023:7.2.4.2 or §2023:7.2.4.3 SR7 (the
      *> partial-word rule, a COBOL-2002 construct). A compiler that
      *> accepts a literal operand, or a refusal citing another
      *> edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993P.
       REPLACE "A" BY "B".
       PROCEDURE DIVISION.
       P-1.
           STOP RUN.
