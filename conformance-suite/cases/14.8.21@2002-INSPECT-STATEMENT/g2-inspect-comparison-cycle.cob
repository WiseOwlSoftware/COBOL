      *>--- conformance-case
      *> id: g2-inspect-comparison-cycle
      *> rules: [2002-GR-14.8.21.3-7, 2002-GR-14.8.21.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.3 GR7 - the forward comparison cycle of TALLYING
      *> and REPLACING: operands in the order written, left to right
      *> (7a), advance by one when nothing matches (7b), resume right of
      *> a match (7c), stop at the rightmost position (7d), CHARACTERS
      *> as an implied one-character operand (7e); LEADING per GR11b and
      *> FIRST per GR16d.
      *> §2002:14.8.21.3 GR8 - the BEFORE/AFTER regions: no phrase (8a),
      *> BEFORE with and without an occurrence of literal-2 (8b), AFTER
      *> with and without one (8c), and the delimiter's position fixed
      *> before the first cycle.
      *> S = "AABAXBAACABA" (positions 1-12: A A B A X B A A C A B A).
      *> EXPECTED, each derived from the 2002 text:
      *>   C1 ALL "A", no phrase (7a, 8a): A at 1,2,4,7,8,10,12   = 07
      *>   C2 LEADING "A" (GR11b): 1,2 then B at 3 breaks it      = 02
      *>   C3 CHARACTERS BEFORE INITIAL "X" (7e, 8b): X is at 5,
      *>      positions 1-4 each match the implied operand        = 04
      *>   C4 ALL "AB" AFTER INITIAL "X" (8c): region 6-12
      *>      "BAACABA", "AB" only at 10-11                       = 01
      *>   C5/C6 ALL "AA" then ALL "A" (7a order, 7b, 7c, 7d): at 1
      *>      "AA" is tried first and matches 1-2; 4 "A"; 7-8 "AA";
      *>      10 "A"; at 12 "AA" has no second character, "A"
      *>      matches                                    = 02 and 03
      *>   C7 ALL "A" AFTER INITIAL "Q", no Q (8c: never eligible) = 00
      *>   C8 ALL "A" BEFORE INITIAL "Q", no Q (8b: as though no
      *>      BEFORE)                                             = 07
      *>   C9 LEADING "A" AFTER INITIAL "B": first eligible at 4
      *>      (8c), "A" at 4 then X at 5 (GR11b)                  = 01
      *>   T  REPLACING FIRST "A" BY "Z" AFTER INITIAL "B": region
      *>      4-12, its leftmost "A" is 4 (GR16d)       AABZXBAACABA
      *>   U  REPLACING CHARACTERS BY "*" AFTER INITIAL "X" BEFORE
      *>      INITIAL "C": positions 6-8 (7e, 8b, 8c)   AABAX***CABA
      *>   V  "ABABXAB" REPLACING ALL "B" BY "X" BEFORE INITIAL "X":
      *>      the delimiter is the X at 5, fixed before the first
      *>      cycle (8b), so both B at 2 and 4 are replaced even
      *>      though the first replacement writes an X at 2  AXAXXAB
      *>   Z  identifier-4 a zero-length item (8a): ZG is a group
      *>      holding only an OCCURS 0 TO 3 DEPENDING ON ZN table
      *>      with ZN = 0, a zero-length item (§2002:13.16.36.3 GR8);
      *>      passed BY CONTENT its length is its current length
      *>      (§2002:14.7.1.1), so the ANY LENGTH formal P is a
      *>      zero-length item (§2002:13.16.2.3 GR1). ALL "A" AFTER
      *>      INITIAL P and CHARACTERS BEFORE INITIAL P then
      *>      participate as though no phrase were written
      *>                                           Z CZ=07 CY=12 L=0
      *> Why each leg can fail: reversing the operand order gives
      *> C5=00 C6=07; not advancing after a failed cycle or resuming
      *> inside a match changes C5/C6; treating an absent AFTER
      *> delimiter as "no phrase" gives C7=07, an absent BEFORE as
      *> "empty region" gives C8=00; anchoring LEADING at position 1
      *> instead of the first eligible position gives C9=00; FIRST
      *> outside the region gives T=AABAXBAACABA with Z at 1; a
      *> delimiter re-found after a replacement gives V=AXABXAB;
      *> a zero-length AFTER delimiter taken as "no occurrence" gives
      *> CZ=00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S   PIC X(12) VALUE "AABAXBAACABA" GLOBAL.
       01  T   PIC X(12).
       01  U   PIC X(12).
       01  V   PIC X(7) VALUE "ABABXAB".
       01  C1  PIC 99 VALUE 0.
       01  C2  PIC 99 VALUE 0.
       01  C3  PIC 99 VALUE 0.
       01  C4  PIC 99 VALUE 0.
       01  C5  PIC 99 VALUE 0.
       01  C6  PIC 99 VALUE 0.
       01  C7  PIC 99 VALUE 0.
       01  C8  PIC 99 VALUE 0.
       01  C9  PIC 99 VALUE 0.
       01  ZN  PIC 9 VALUE 0.
       01  ZG.
           05  ZE  PIC X OCCURS 0 TO 3 TIMES DEPENDING ON ZN.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S TALLYING C1 FOR ALL "A".
           INSPECT S TALLYING C2 FOR LEADING "A".
           INSPECT S TALLYING C3 FOR CHARACTERS BEFORE INITIAL "X".
           INSPECT S TALLYING C4 FOR ALL "AB" AFTER INITIAL "X".
           INSPECT S TALLYING C5 FOR ALL "AA" C6 FOR ALL "A".
           INSPECT S TALLYING C7 FOR ALL "A" AFTER INITIAL "Q".
           INSPECT S TALLYING C8 FOR ALL "A" BEFORE INITIAL "Q".
           INSPECT S TALLYING C9 FOR LEADING "A" AFTER INITIAL "B".
           MOVE S TO T.
           INSPECT T REPLACING FIRST "A" BY "Z" AFTER INITIAL "B".
           MOVE S TO U.
           INSPECT U REPLACING CHARACTERS BY "*"
               AFTER INITIAL "X" BEFORE INITIAL "C".
           INSPECT V REPLACING ALL "B" BY "X" BEFORE INITIAL "X".
           DISPLAY "C1=" C1 " C2=" C2 " C3=" C3 " C4=" C4.
           DISPLAY "C5=" C5 " C6=" C6 " C7=" C7 " C8=" C8 " C9=" C9.
           DISPLAY "T=" T.
           DISPLAY "U=" U.
           DISPLAY "V=" V.
           CALL "G2INS01Z" USING BY CONTENT ZG.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS01Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CZ  PIC 99 VALUE 0.
       01  CY  PIC 99 VALUE 0.
       01  PL  PIC 9.
       LINKAGE SECTION.
       01  P   PIC X ANY LENGTH.
       PROCEDURE DIVISION USING P.
       MAIN.
           INSPECT S TALLYING CZ FOR ALL "A" AFTER INITIAL P.
           INSPECT S TALLYING CY FOR CHARACTERS BEFORE INITIAL P.
           MOVE FUNCTION LENGTH(P) TO PL.
           DISPLAY "Z CZ=" CZ " CY=" CY " L=" PL.
           EXIT PROGRAM.
       END PROGRAM G2INS01Z.
       END PROGRAM G2INS01.
