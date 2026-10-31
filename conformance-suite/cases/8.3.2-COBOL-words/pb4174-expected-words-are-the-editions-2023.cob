      *>--- conformance-case
      *> id: pb4174-expected-words-are-the-editions-2023
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the twin of pb4174-expected-words-are-the-
      *> editions-85 at 2023, the edition that reserves the shift
      *> operators (ISO Annex E.2 3): "The boolean operators B-SHIFT-L,
      *> B-SHIFT-R, B-SHIFT-LC and B-SHIFT-RC have been added",
      *> cite.py --check E.2 "B-SHIFT-L" -> OK). After the open
      *> parenthesis of 8.3.5 4) the sentence names all seven boolean
      *> operators: nothing the superset parse holds is hidden from the
      *> edition that has it (ISO 8.3.2.1 1), cite.py --check 8.3.2.1 ->
      *> OK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174E23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC 999.
       01 N-B PIC 999 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-A = (N-B + 1.
           STOP RUN.
