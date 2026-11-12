      *>--- conformance-case
      *> id: pb5120_before_after_intersection
      *> rules: [GR-14.9.22.4-9]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5120 - an INSPECT operand with BOTH a BEFORE and an AFTER phrase: where its BEFORE delimiter
      *> is looked for.
      *>
      *> §1985:VI.6.18.4 GR8 (cite.py --check) b: BEFORE "participates only in those comparison cycles which
      *> involve that portion of the content of the data item referenced by identifier-1 from its leftmost
      *> character position" up to the first occurrence of literal-2; c: AFTER from the position right of the
      *> first occurrence of its own literal-2. The operand participates where the two portions intersect (the
      *> 2002 text and the 2014 FCD print the same words): the .out file, 85/2002/2014.
      *> §2023:14.9.22.4 GR9 b: "the first character position that is eligible to participate up to, but not
      *> including, the first occurrence encountered of literal-2", so from 2023 the BEFORE delimiter is looked
      *> for from the AFTER-derived position: the .2023.out file.
      *>
      *> Derivation (positions from 0).
      *>  AYAAXAA, AFTER "X" BEFORE "Y": X at 4 gives AFTER [5,7); Y at 1.
      *>    1985: BEFORE [0,1), the intersection is empty: 00, unchanged.
      *>    2023: no Y from 5, so as if no BEFORE: [5,7): 02, AYAAXZZ.
      *>  AYXAAYA: X at 2 gives [3,7); Y at 1 (1985: empty, 00); 2023: Y from 3 is at 5: [3,5), 02.
      *>  AXAAYAA: X at 1 gives [2,7); Y at 4 either way: [2,4) at every edition, 02.
      *>  BXAYBZ, CHARACTERS AFTER "A" BEFORE "B": A at 2 gives [3,6); B at 0 (1985: empty, 00, BXAYBZ);
      *>    2023: B from 3 is at 4: [3,4), 01, BXA*BZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5120BA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D7  PIC X(7).
       01  D6  PIC X(6).
       01  C1  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AYAAXAA" TO D7.
           MOVE ZERO TO C1.
           INSPECT D7 TALLYING C1 FOR ALL "A"
               AFTER INITIAL "X" BEFORE INITIAL "Y".
           DISPLAY "CROSSED " C1.
           MOVE ZERO TO C1.
           INSPECT D7 TALLYING C1 FOR ALL "A"
               BEFORE INITIAL "Y" AFTER INITIAL "X".
           DISPLAY "CROSSED-BA " C1.
           INSPECT D7 REPLACING ALL "A" BY "Z"
               AFTER INITIAL "X" BEFORE INITIAL "Y".
           DISPLAY "REPLACED " D7.
           MOVE "AYAAXAA" TO D7.
           INSPECT D7 CONVERTING "A" TO "Z"
               AFTER INITIAL "X" BEFORE INITIAL "Y".
           DISPLAY "CONVERTED " D7.
           MOVE "AYXAAYA" TO D7.
           MOVE ZERO TO C1.
           INSPECT D7 TALLYING C1 FOR ALL "A"
               AFTER INITIAL "X" BEFORE INITIAL "Y".
           DISPLAY "REPEATED " C1.
           MOVE "AXAAYAA" TO D7.
           MOVE ZERO TO C1.
           INSPECT D7 TALLYING C1 FOR ALL "A"
               AFTER INITIAL "X" BEFORE INITIAL "Y".
           DISPLAY "ORDERED " C1.
           MOVE "BXAYBZ" TO D6.
           MOVE ZERO TO C1.
           INSPECT D6 TALLYING C1 FOR CHARACTERS
               AFTER INITIAL "A" BEFORE INITIAL "B".
           DISPLAY "CHARACTERS " C1.
           INSPECT D6 REPLACING CHARACTERS BY "*"
               AFTER INITIAL "A" BEFORE INITIAL "B".
           DISPLAY "CHARS-REPLACED " D6.
           STOP RUN.
