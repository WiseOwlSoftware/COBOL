      *>--- conformance-case
      *> id: pb5993-nested-variable-table-cites-1985-85
      *> rules: [1985-SR-VI.5.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:VI.5.8.3 SR1b (cite.py:
      *> OK): the OCCURS clause "must not be specified in a data
      *> description entry that ... Has a variable occurrence data item
      *> subordinate to it". G has OCCURS and T, a variable occurrence
      *> data item, is subordinate to it, so G is refused (COBOL0854)
      *> and the refusal cites the clause of the edition the program is
      *> compiled at, never §2023:13.18.38.3. A compiler that accepts
      *> the table, or a refusal citing another edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       01 R.
          05 G OCCURS 3 TIMES.
             10 T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P-1.
           STOP RUN.
