      *>--- conformance-case
      *> id: pb4224-literal-after-a-condition
      *> rules: [FMT-14.9.19.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4224, review R1-1 - a literal written directly
      *> after the condition of an IF, where THEN or a statement may
      *> begin but a literal does not. ISO 14.5.3.1 "A procedure
      *> division statement begins with the first word of the
      *> statement name" (cite.py --check 14.5.3.1 -> OK). The
      *> condition could also be lengthened (AND, OR, an operator),
      *> which the parser has left behind once the condition is
      *> complete, so the sentence lists no expected words: a list
      *> of the statements alone would claim the operators are
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4224C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF W-N = 1 5 CONTINUE END-IF.
           STOP RUN.
