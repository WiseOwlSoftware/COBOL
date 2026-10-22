      *>--- conformance-case
      *> id: pb3512_group_sender_national_bit_slice
      *> rules: [GR-14.9.25.4-4, GR-8.4.3.3.4-6, GR-13.18.29.4-1,
      *>    GR-13.18.29.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3512 (branch review R1-1, R1-2). A GROUP sender into a
      *> national or bit receiver is ISO §14.9.25.4 GR4's group move:
      *>   cite.py --check 14.9.25.4 "there is no conversion of data
      *>   from one form of internal representation to another"
      *>   -> OK  §14.9.25.4 4)  (General rules)
      *>   cite.py --check 14.9.25.4 "Bit group items and national group
      *>   items are treated as elementary items in the MOVE statement"
      *>   -> OK  §14.9.25.4 4)  (General rules)
      *> so the receiver's STORAGE takes the sending group's bytes and
      *> its national / boolean value is read from them (design D-N8).
      *> A reference-modified receiver is an elementary item too:
      *>   cite.py --check 8.4.3.3.4 "The unique data item is considered
      *>   to be an elementary data item without the JUSTIFIED clause"
      *>   -> OK  §8.4.3.3.4 6)  (General rules)
      *> and a national / bit GROUP is that elementary item's usage:
      *>   cite.py --check 13.18.29.4 "a national group is treated as
      *>   though it were an elementary data item of usage national"
      *>   -> OK  §13.18.29.4 2) b)  (General rules)
      *>   cite.py --check 13.18.29.4 "treated as though it were an
      *>   elementary data item of usage bit"
      *>   -> OK  §13.18.29.4 1) b)  (General rules)
      *> Usage national, usage bit and GROUP-USAGE are new in ISO 2002,
      *> so the test starts there. The sending bytes are the native
      *> alphanumeric codes ("A" = X"41"), a national position is two
      *> UTF-16BE bytes (D-N1), so four sending bytes fill exactly two
      *> national positions and one byte fills eight bit positions.
      *> EXPECTED OUTPUT, derived line by line (every receiver starts
      *> all N"*" or all B"0"; AG is "ABCD", A1 is "A"):
      *>   E  AG into N6(2:2), an elementary national slice: N6(2:2) =
      *>      NX"41424344", N6(1:1) = N"*", N6(4:3) = N"***": T.
      *>      (Before the fix: N"AB", each byte one national character.)
      *>   G  AG into NG(1:2), a national GROUP's slice: NG(1:2) =
      *>      NX"41424344", NG(3:2) = N"**": T.
      *>   W  AG into the whole national group NW: NW = NX"41424344": T.
      *>   B  A1 into BE(1:8), an elementary bit slice: B"01000001": T.
      *>   K  A1 into BG(1:8), a bit GROUP's slice: B"01000001": T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3512RMGRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AG.
          05 AG1 PIC X(4) VALUE "ABCD".
       01 A1.
          05 A11 PIC X(1) VALUE "A".
       01 N6 PIC N(6) VALUE ALL N"*".
       01 NG GROUP-USAGE NATIONAL.
          05 NG1 PIC N(2) VALUE ALL N"*".
          05 NG2 PIC N(2) VALUE ALL N"*".
       01 NW GROUP-USAGE NATIONAL.
          05 NW1 PIC N(2) VALUE ALL N"*".
       01 BE PIC 1(8) USAGE BIT VALUE B"00000000".
       01 BG GROUP-USAGE BIT.
          05 BG1 PIC 1(8) VALUE B"00000000".
       PROCEDURE DIVISION.
           MOVE AG TO N6(2:2)
           IF N6(2:2) = NX"41424344" AND N6(1:1) = N"*"
              AND N6(4:3) = N"***"
              DISPLAY "E T" ELSE DISPLAY "E F" END-IF
           MOVE AG TO NG(1:2)
           IF NG(1:2) = NX"41424344" AND NG(3:2) = N"**"
              DISPLAY "G T" ELSE DISPLAY "G F" END-IF
           MOVE AG TO NW
           IF NW = NX"41424344" DISPLAY "W T" ELSE DISPLAY "W F" END-IF
           MOVE A1 TO BE(1:8)
           IF BE = B"01000001" DISPLAY "B T" ELSE DISPLAY "B F" END-IF
           MOVE A1 TO BG(1:8)
           IF BG = B"01000001" DISPLAY "K T" ELSE DISPLAY "K F" END-IF
           STOP RUN.
