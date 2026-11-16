      *>--- conformance-case
      *> id: g85_f07_03
      *> rules: [1985-SR-VI.5.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.6.3 SR1 - "The JUSTIFIED clause can be specified
      *> only at the elementary item level." JUST-A and JUST-B are
      *> elementary items of group WS-G that carry JUSTIFIED (and JUST,
      *> SR2); the compiler must accept them. §1985:VI.5.6.4 GR1 gives
      *> what each MOVE stores: a shorter sender is aligned at the
      *> rightmost position with space fill (line 1: "[    AB]"), a
      *> longer one loses its leftmost characters (line 2: "[CDEFGH]").
      *> Line 3 shows the group holds both items, right-justified, in
      *> place. A compiler that refuses the clause on an elementary
      *> item, or ignores it ("[AB    ]", "[ABCDEF]"), fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0703.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  JUST-A  PIC X(6) JUSTIFIED RIGHT.
           05  JUST-B  PIC X(6) JUST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO JUST-A.
           MOVE "ABCDEFGH" TO JUST-B.
           DISPLAY "[" JUST-A "]".
           DISPLAY "[" JUST-B "]".
           DISPLAY "[" WS-G "]".
           STOP RUN.
