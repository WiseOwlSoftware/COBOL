      *>--- conformance-case
      *> id: pb4174-expected-words-are-the-editions-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the words COBOL5506 names as expected at a
      *> refused token are the words the TARGETED edition has. ISO
      *> 8.3.2.1 1): "Reserved words shall not be used as user-defined
      *> words or system-names." (cite.py --check 8.3.2.1 -> OK), so a
      *> word 8.9 has not reserved at an edition is a user-defined word
      *> there, never a keyword a diagnostic may send the user to. The
      *> boolean operators B-AND, B-OR and B-XOR are 2002 reservations
      *> (reserved-words.json r85 false) and the shift operators B-SHIFT-L,
      *> B-SHIFT-LC, B-SHIFT-R and B-SHIFT-RC are 2023's (ISO Annex E.2 3):
      *> "The boolean operators B-SHIFT-L, B-SHIFT-R, B-SHIFT-LC and
      *> B-SHIFT-RC have been added", cite.py --check E.2 "B-SHIFT-L" ->
      *> OK). The parse is the superset of the four editions, so after
      *> the open parenthesis of 8.3.5 4) ("parentheses may appear only
      *> in balanced pairs") its expected set holds all seven. At
      *> COBOL-85 the sentence names none of them: only the arithmetic
      *> operators and the right parenthesis. Twin tests at 2002 and
      *> 2023 pin the other lists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174E85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC 999.
       01 N-B PIC 999 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-A = (N-B + 1.
           STOP RUN.
