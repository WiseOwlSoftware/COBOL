      *>--- conformance-case
      *> id: pb5564-subscripted-subscript-plus-integer-85
      *> rules: [1985-FMT-IV.4.3.8.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5564. The data-name-2 [{+|-} integer-2] form of §1985:IV.4.3.8.2.2, whose
      *> data-name-2 "may be qualified" (§1985:IV.4.3.8.2.3 SR5) and so, by §1985:III.2, not
      *> subscripted. ELEM (IE (2) + 1) writes a subscripted data-name before + 1: the 2002
      *> arithmetic-expression subscript. Expected: COBOL0900 at 85. Fails if the + integer
      *> arm takes any data reference as data-name-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5564SP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 5 TIMES.
       01  IET.
           05  IE PIC 9 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO TBL.
           MOVE "123" TO IET.
           DISPLAY ELEM (IE (2) + 1).
           STOP RUN.
