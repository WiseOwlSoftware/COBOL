      *>--- conformance-case
      *> id: pb6021-occurs-signed-integer-85
      *> rules: [1985-SR-III.2-p142]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6021. §1985:III.2 p142: "Where the term 'integer'
      *> appears in the general formats, integer must be a numeric
      *> literal which is an integer, and it must be neither signed nor
      *> zero unless explicitly allowed by the rules for that format."
      *> No rule of the OCCURS format allows a sign, so OCCURS +3 is
      *> refused, once, by that rule: COBOL7737 (the sign half of the
      *> integer screen whose zero half is COBOL2386,
      *> 1985-SR-III_2-p142). It used to be the parse error COBOL0001
      *> with COBOL1941 naming the entry's closed clause list.
      *> Control: the same entry with OCCURS 3 compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P6021S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS +3 TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "P6021S ACCEPTED".
           STOP RUN.