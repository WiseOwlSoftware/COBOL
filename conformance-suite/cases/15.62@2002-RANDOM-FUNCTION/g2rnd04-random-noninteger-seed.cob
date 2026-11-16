      *>--- conformance-case
      *> id: g2rnd04-random-noninteger-seed
      *> rules: [2002-AR-15.62.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.62.2 AR2 - "If argument-1 is specified, it shall be
      *>   zero or a positive integer." Table 22 of §2002:15.5 types
      *> the argument Int1, and §2002:15.2 AR5 (type 5) asks "An
      *> arithmetic expression that will always result in an integer
      *> value or an integer data item". W-FR PIC 9V9 is not an
      *> integer data item, so the reference is refused at compile
      *> time (the run-time half, a negative integer, is g2rnd02 NEG).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RND04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-FR    PIC 9V9 VALUE 2.5.
       01  R       PIC 9V9(9).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION RANDOM(W-FR)
           STOP RUN.
