      *>--- conformance-case
      *> id: pb5846-sr4-linkage-subscript-not-referenceable
      *> rules: [SR-13.7.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR4 (cite.py --check: OK): Linkage items may be
      *> referenced within the Procedure Division "if, and only if"
      *> they meet the rule's legs; §2023:13.7.3 SR4 is the same rule.
      *> L2 is in no leg (it is no USING operand, no subordinate of
      *> one, no redefinition or renaming of one), and a data-name
      *> written as a subscript is a reference within the Procedure
      *> Division, so the program is refused with COBOL2746 as `DISPLAY
      *> L2-N` is (kb/Work PB5846). The control, pb5846-sr4-linkage-
      *> using-operand-position, names L2 in USING and is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5846XA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T.
          05 W-E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P0.
           MOVE "ABC" TO W-T.
           CALL "PB5846XS" USING W-T.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5846XS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 L1-E PIC X OCCURS 3 TIMES.
       01 L2.
          05 L2-N PIC 9.
       PROCEDURE DIVISION USING L1.
       P1.
           DISPLAY L1-E (L2-N).
           EXIT PROGRAM.
       END PROGRAM PB5846XS.
       END PROGRAM PB5846XA.
