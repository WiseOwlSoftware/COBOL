      *>--- conformance-case
      *> id: pb3605_move_space_nonnumeric
      *> rules: [SR-14.9.25.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3605 - the scope of ISO §14.9.25.3 SR5's SPACE arm,
      *> which holds at every edition (its negative is
      *> pb3605-move-space-numeric): only a NUMERIC or NUMERIC-EDITED
      *> receiving item is refused.
      *>   cite.py --check 14.9.25.3 "In all other cases, the move of an
      *>   alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE,
      *>   LOW-VALUE, ALL "literal", or ALL symbolic-character) to either
      *>   a numeric item or a numeric-edited item is prohibited"
      *>   -> OK §14.9.25.3 5)
      *>   cite.py --check 8.4.3.3.4 "the categories numeric and
      *>   numeric-edited are considered class and category national if
      *>   the usage is national; otherwise they are considered class and
      *>   category alphanumeric" -> OK §8.4.3.3.4 6) c)
      *>   cite.py --check 14.9.25.4 "is treated exactly as if it were an
      *>   alphanumeric to alphanumeric elementary move"
      *>   -> OK §14.9.25.4 4)
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   X=[   ]  an alphanumeric receiver: SR5 does not reach it.
      *>   R=[  3]  a reference-modified view of the numeric item N (123)
      *>            is category alphanumeric (8.4.3.3.4 6) c)), so
      *>            N (1:2) takes two spaces and the third digit stays.
      *>   G=[   ]  a group receiver: a move that is not elementary is an
      *>            alphanumeric-to-alphanumeric move (14.9.25.4 GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3605S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ABC".
       01 N PIC 9(3) VALUE 123.
       01 G.
          05 GN PIC 9(3) VALUE 456.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO X.
           DISPLAY "X=[" X "]".
           MOVE SPACE TO N (1:2).
           DISPLAY "R=[" N "]".
           MOVE SPACES TO G.
           DISPLAY "G=[" G "]".
           STOP RUN.
