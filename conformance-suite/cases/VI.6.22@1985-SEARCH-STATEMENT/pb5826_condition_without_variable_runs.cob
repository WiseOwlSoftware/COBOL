      *>--- conformance-case
      *> id: pb5826_condition_without_variable_runs
      *> rules: [1985-SR-VI.6.22.3-3, 1985-SR-VI.6.3.1.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5826G1.
      *> kb/Work PB5826.  §1985:VI.6.3.1.1: "The relation condition must
      *> contain at least one reference to a variable", and
      *> §1985:VI.6.3.1.5: "Arithmetic-expression-1 must contain at
      *> least one reference to a variable" (cite.py OK on both).  Each
      *> condition below references no variable, so at --std 85 it is
      *> flagged by warning COBOL7609 (relation) or COBOL7610 (sign)
      *> (RelationLiteralOperandTests pins the warnings); like
      *> COBOL3146 at 2002 and later (docs/conformance/3/D-RELLITERAL.md)
      *> it is a warning, so the program compiles and each condition is
      *> evaluated as written by the arithmetic rules of §1985:VI.6.3.1:
      *>
      *> T1  1 + 1 = 2            true
      *> T2  3 IS POSITIVE        true
      *> T3  2 * 2 = 4            true
      *> T4  2 * 2 IS NOT ZERO    true
      *> T5  1 = 2                false
      *> T6  0 IS NEGATIVE        false
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  TE PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P0.
           MOVE 123 TO TB.
           SET IX TO 1.
           SEARCH TE AT END DISPLAY "T1 END"
               WHEN 1 + 1 = 2 DISPLAY "T1 HIT".
           SET IX TO 1.
           SEARCH TE AT END DISPLAY "T2 END"
               WHEN 3 IS POSITIVE DISPLAY "T2 HIT".
           IF 2 * 2 = 4 DISPLAY "T3 HIT" ELSE DISPLAY "T3 MISS".
           IF 2 * 2 IS NOT ZERO DISPLAY "T4 HIT" ELSE DISPLAY "T4 MISS".
           IF 1 = 2 DISPLAY "T5 HIT" ELSE DISPLAY "T5 MISS".
           IF 0 IS NEGATIVE DISPLAY "T6 HIT" ELSE DISPLAY "T6 MISS".
           STOP RUN.
