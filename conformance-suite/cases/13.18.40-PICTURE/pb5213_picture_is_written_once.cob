      *>--- conformance-case
      *> id: pb5213_picture_is_written_once
      *> rules: [FMT-13.18.40.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5213 - the accepted twin of
      *> pb5213-picture-is-written-twice. ISO 13.18.40.2 Format 1
      *> prints PICTURE IS character-string-1 (cite.py --check
      *> "§2023:13.18.40.2" "IS character-string-1 LOCALE" OK, the
      *> Format 2 line, which prints the same head): IS is optional
      *> and written once, anywhere a separator space may stand, a
      *> line end included. A writes PICTURE IS at the end of its line
      *> and the character-string on the next; B writes PIC IS and
      *> the string on one line; C writes no IS. Expected, from the
      *> VALUE clauses: A is WXYZ, B is 007 (PIC 9(3) VALUE 7), C is
      *> 5. It fails when IS, now the lexer's IS token, is not taken
      *> by the PICTURE clause on either line arrangement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5213ONCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PICTURE IS
               X(4) VALUE "WXYZ".
       01  B PIC IS 9(3) VALUE 7.
       01  C PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       P1.
           DISPLAY A "|" B "|" C.
           STOP RUN.
