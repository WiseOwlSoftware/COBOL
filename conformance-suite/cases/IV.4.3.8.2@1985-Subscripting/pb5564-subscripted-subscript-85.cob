      *>--- conformance-case
      *> id: pb5564-subscripted-subscript-85
      *> rules: [1985-FMT-IV.4.3.8.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5564. §1985:IV.4.3.8.2.2 prints the subscript as integer-1, data-name-2
      *> [{+|-} integer-2] or index-name-1 [{+|-} integer-3]; §1985:IV.4.3.8.2.3 SR5:
      *> "Data-name-2 may be qualified and must be a numeric elementary item representing an
      *> integer", and §1985:III.2 says a 'data-name' "must not be reference-modified,
      *> subscripted, or qualified unless specifically permitted by the rules of the format".
      *> IE (2) is a subscripted data-name, so ELEM (IE (2)) is no 1985 subscript: it is the
      *> arithmetic-expression subscript of 2002. Expected: COBOL0900 at 85 at the subscript.
      *> Fails if data-name-2 is taken to be any data reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5564SS.
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
           DISPLAY ELEM (IE (2)).
           STOP RUN.
