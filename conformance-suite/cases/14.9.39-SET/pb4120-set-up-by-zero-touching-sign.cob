      *>--- conformance-case
      *> id: pb4120-set-up-by-zero-touching-sign
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4120 (review lead R1-1 of PB3961) - ISO 8.7.1: the
      *> arithmetic operators are COBOL characters "that shall be
      *> preceded by a space and followed by a space". SET Format 2
      *> takes arithmetic-expression-2 (14.9.39.2), so ZERO -1 there is
      *> the subtraction ZERO - 1 written without its second space,
      *> exactly as Y -1 is: COBOL2993, the sibling of
      *> pb3981-compute-zero-touching-sign. The slot takes no figurative
      *> ZERO, so the parse refused the ZERO itself and the compile died
      *> of a generic parse error there instead. Fails if that bare-ZERO
      *> slot still refuses the ZERO before the sign can be read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4120SS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB VALUE "ABCD".
          05 TE PIC X OCCURS 4 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 3.
           SET IX UP BY ZERO -1.
           STOP RUN.
