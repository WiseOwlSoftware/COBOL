      *>--- conformance-case
      *> id: g85_bf16_01
      *> rules: [1985-GR-VI.6.18.4-1, 1985-GR-VI.6.18.4-4,
      *>    1985-GR-VI.6.18.4-6, 1985-GR-VI.6.18.4-10,
      *>    1985-GR-VI.6.18.4-11, 1985-GR-VI.6.18.4-14,
      *>    1985-GR-VI.6.18.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR1 GR4 GR6 GR10 GR11 GR14 GR18 - INSPECT
      *> general rules over fixed inputs; each DISPLAY line is keyed
      *> to the rule it pins (G=GR1, I=GR4, P=GR6, L=GR10, C=GR11,
      *> T=GR14, V=GR18). The 1985 text is the oracle:
      *>
      *> GR1 "identifier-1 is treated as if it were a sending data
      *>   item" with §1985:VI.5.8.4 GR3b: a group holding its own
      *>   OCCURS DEPENDING object uses, as a sending item, only the
      *>   part the object specifies; as a receiving item, its
      *>   maximum length. MOVE "5AABAA" fills all six positions,
      *>   then GC=2: the inspected extent is "2AA".
      *>   G1 TALLYING FOR CHARACTERS                       -> 03
      *>      (a maximum-length extent would give 06)
      *>   G2 is format 2, where GR1 alone makes identifier-1 a
      *>      sending item (format 1 has GR9). REPLACING ALL "A"
      *>      BY "B" BEFORE INITIAL "AB": "AB" is not in "2AA", so
      *>      BEFORE acts as absent (GR8b), both A change  -> [2BB]
      *>      (the maximum "2AABAA" has "AB" at 3-4, only
      *>      position 2 would change: [2BA])
      *>   G3 refill, GC=1, TALLYING FOR ALL "A" over "1A"  -> 01
      *>      (positions beyond the count are undefined by
      *>      §1985:VI.5.8.4 GR2, so none is displayed)
      *> GR4 identifier-3..7 act as literal-1..5 with their whole
      *>   content (the trailing space is a character).
      *>   I1 "A A AA" ALL PAT ("A ", identifier-3): 1-2, 3-4  -> 02
      *>   I2 "ABABB " CHARACTERS BEFORE INITIAL DL4 ("B ",
      *>      identifier-4): first "B " at 5-6 (GR8b)         -> 04
      *>   I3 "A A AA" REPLACING ALL PAT BY REP ("* ",
      *>      identifier-5)                                 -> [* * AA]
      *>   I4 "A A AA" CONVERTING FR ("A ") TO TT ("a_")
      *>      (identifier-6, identifier-7)                  -> [a_a_aa]
      *> GR6 each properly matched occurrence is tallied or
      *>   replaced; GR7c resumes right of the match.
      *>   P1 "ABCABCAB" ALL "AB": 1, 4, 7                   -> 03
      *>   P2 same, REPLACING ALL "AB" BY "xy"         -> [xyCxyCxy]
      *>   P3 "AAAAA" ALL "AA": 1-2, 3-4; 5 alone fails      -> 02
      *> GR10 ALL and LEADING carry to each succeeding literal-1.
      *>   L1 "AAXB" FOR LEADING "A" "B": B is LEADING too and is
      *>      not at position 1 (GR12b), so only 1-2 count   -> 02
      *>      (B taken as ALL would give 03)
      *>   L2 "AAXB" FOR LEADING "A" ALL "B": ALL takes over  -> 03
      *>   L3 "BAXB" FOR ALL "A" "B": B, A, B                 -> 03
      *> GR11 identifier-2 is not initialized: C5 VALUE 5.
      *>   C1 "AAB" ALL "A" adds 2                           -> 07
      *>   C2 the same INSPECT again adds 2 more             -> 09
      *> GR14 ALL, LEADING, FIRST carry to each succeeding BY phrase.
      *>   T1 "ABAB" FIRST "A" BY "X" "B" BY "Y": B is FIRST too
      *>      (GR15d), so only its leftmost changes          -> [XYAB]
      *>   T2 "ABCABC" FIRST "A" BY "X" ALL "B" BY "Y" "C" BY "Z"
      *>                                                  -> [XYZAYZ]
      *>   T3 "AABA" LEADING "A" BY "x" "B" BY "y": B is LEADING,
      *>      not at position 1; the A at 4 is not contiguous
      *>      (GR15c)                                        -> [xxBA]
      *> GR18 CONVERTING is a format 2 with one ALL phrase per
      *>   character of literal-4, by ordinal position.
      *>   V1 "CAB" CONVERTING "ABC" TO "zyx"                -> [xzy]
      *>   V2 "AABB" CONVERTING "AB" TO "BA": one statement, each
      *>      position replaced once                        -> [BBAA]
      *>      (two successive replacements would give AAAA)
      *> Each leg fails when its rule is not applied: the
      *> alternative value each would show is named above.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1601.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  GC  PIC 9.
           05  GE  PIC X OCCURS 1 TO 5 TIMES DEPENDING ON GC.
       01  N   PIC 99.
       01  C5  PIC 99 VALUE 5.
       01  X3  PIC X(3).
       01  X4  PIC X(4).
       01  X5  PIC X(5).
       01  X6  PIC X(6).
       01  X8  PIC X(8).
       01  Q   PIC X(6).
       01  W6  PIC X(6).
       01  PAT PIC X(2) VALUE "A ".
       01  DL4 PIC X(2) VALUE "B ".
       01  REP PIC X(2) VALUE "* ".
       01  FR  PIC X(2) VALUE "A ".
       01  TT  PIC X(2) VALUE "a_".
       PROCEDURE DIVISION.
       P0.
      * GR1
           MOVE "5AABAA" TO G.
           MOVE 2 TO GC.
           MOVE 0 TO N.
           INSPECT G TALLYING N FOR CHARACTERS.
           DISPLAY "G1=" N.
           INSPECT G REPLACING ALL "A" BY "B"
               BEFORE INITIAL "AB".
           DISPLAY "G2=[" G "]".
           MOVE "5AAAAA" TO G.
           MOVE 1 TO GC.
           MOVE 0 TO N.
           INSPECT G TALLYING N FOR ALL "A".
           DISPLAY "G3=" N.
      * GR4
           MOVE "A A AA" TO Q.
           MOVE 0 TO N.
           INSPECT Q TALLYING N FOR ALL PAT.
           DISPLAY "I1=" N.
           MOVE "ABABB " TO W6.
           MOVE 0 TO N.
           INSPECT W6 TALLYING N FOR CHARACTERS BEFORE INITIAL DL4.
           DISPLAY "I2=" N.
           INSPECT Q REPLACING ALL PAT BY REP.
           DISPLAY "I3=[" Q "]".
           MOVE "A A AA" TO Q.
           INSPECT Q CONVERTING FR TO TT.
           DISPLAY "I4=[" Q "]".
      * GR6
           MOVE "ABCABCAB" TO X8.
           MOVE 0 TO N.
           INSPECT X8 TALLYING N FOR ALL "AB".
           DISPLAY "P1=" N.
           INSPECT X8 REPLACING ALL "AB" BY "xy".
           DISPLAY "P2=[" X8 "]".
           MOVE "AAAAA" TO X5.
           MOVE 0 TO N.
           INSPECT X5 TALLYING N FOR ALL "AA".
           DISPLAY "P3=" N.
      * GR10
           MOVE "AAXB" TO X4.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR LEADING "A" "B".
           DISPLAY "L1=" N.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR LEADING "A" ALL "B".
           DISPLAY "L2=" N.
           MOVE "BAXB" TO X4.
           MOVE 0 TO N.
           INSPECT X4 TALLYING N FOR ALL "A" "B".
           DISPLAY "L3=" N.
      * GR11
           MOVE "AAB" TO X3.
           INSPECT X3 TALLYING C5 FOR ALL "A".
           DISPLAY "C1=" C5.
           INSPECT X3 TALLYING C5 FOR ALL "A".
           DISPLAY "C2=" C5.
      * GR14
           MOVE "ABAB" TO X4.
           INSPECT X4 REPLACING FIRST "A" BY "X" "B" BY "Y".
           DISPLAY "T1=[" X4 "]".
           MOVE "ABCABC" TO X6.
           INSPECT X6 REPLACING FIRST "A" BY "X"
                                ALL "B" BY "Y" "C" BY "Z".
           DISPLAY "T2=[" X6 "]".
           MOVE "AABA" TO X4.
           INSPECT X4 REPLACING LEADING "A" BY "x" "B" BY "y".
           DISPLAY "T3=[" X4 "]".
      * GR18
           MOVE "CAB" TO X3.
           INSPECT X3 CONVERTING "ABC" TO "zyx".
           DISPLAY "V1=[" X3 "]".
           MOVE "AABB" TO X4.
           INSPECT X4 CONVERTING "AB" TO "BA".
           DISPLAY "V2=[" X4 "]".
           STOP RUN.
