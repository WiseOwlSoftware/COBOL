      *>--- conformance-case
      *> id: pb5846-linkage-using-operand-position
      *> rules: [1985-GR-X.5.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of pb5846-linkage-subscript-not-referenceable and
      *> pb5846-linkage-refmod-not-referenceable: L2 is an operand of
      *> the USING phrase (§1985:X.5.1 GR4 leg a), so its item is legal
      *> as a subscript and as a reference-modification position. W-G
      *> holds "ABC" and 2, so L1-E (L2-N) is the second occurrence,
      *> "B", and L1 (L2-N:1) is the second character, "B".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5846C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T.
          05 W-E PIC X OCCURS 3 TIMES.
       01 W-G.
          05 W-N PIC 9.
       PROCEDURE DIVISION.
       P0.
           MOVE "ABC" TO W-T.
           MOVE 2 TO W-N.
           CALL "PB5846CS" USING W-T W-G.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5846CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 L1-E PIC X OCCURS 3 TIMES.
       01 L2.
          05 L2-N PIC 9.
       PROCEDURE DIVISION USING L1 L2.
       P1.
           DISPLAY "SUB " L1-E (L2-N).
           DISPLAY "REF " L1 (L2-N:1).
           EXIT PROGRAM.
       END PROGRAM PB5846CS.
       END PROGRAM PB5846C.
