      *>--- conformance-case
      *> id: pb3981-compute-zero-touching-sign
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB3981 - ISO 8.7.1: the arithmetic operators are COBOL
      * characters "that shall be preceded by a space and followed by a
      * space". In COMPUTE the right-hand side is an arithmetic
      * expression, so ZERO -1 is the subtraction ZERO - 1 written
      * without its second space, exactly as Y -1 is: COBOL2993. The
      * token rewriter keeps a ZERO figurative before a sign touching
      * its literal (an operand LIST, PB3962), and the compile died of a
      * generic parse error here instead. Fails if the sign is read as
      * the literal's own in an expression slot.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3981C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC S9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = ZERO -1.
           STOP RUN.
       END PROGRAM PB3981C.
