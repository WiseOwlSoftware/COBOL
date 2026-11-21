      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_4-p3
      *> rules: [1985-GR-IV.4.3.4-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.4 p3 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). A refusal: the rule forbids line 12 of the
      *> program below, and WiseOwl refuses it at 85 with COBOL0900
      *> there; a compiler that accepts it fails. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.4-p3 REFUSAL: an integer function is used
      * only in an arithmetic expression; MOVE's sending operand is
      * not an arithmetic expression.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTMOV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC               PIC X(5) VALUE "abcde".
       01  N                 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION LENGTH(SRC) TO N.
           DISPLAY N.
           STOP RUN.
