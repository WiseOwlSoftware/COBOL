      *>--- conformance-case
      *> id: g2-inspect-tally-counter-overlap
      *> rules: [2002-GR-14.8.21.3-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.3 GR12 - a TALLYING counter (identifier-2) that
      *> occupies the storage of identifier-1, identifier-3 or
      *> identifier-4: "the result of the execution of this statement is
      *> undefined" (also §2002:B.2 item 22 d), and §2002:3.3:
      *> "A COBOL run unit that allows these situations to happen is a
      *> conforming run unit"). The overlap lines D1-D5, RM, F3A-F3C pin
      *> the
      *> DOCUMENTED IMPLEMENTOR CHOICE docs/conformance/3.1/D-INS1 and
      *> D-INS2 (kb/Work PB1907), not a value the 2002 text gives; the
      *> controls C1 and C2 overlap nothing and are defined by
      *> §2002:14.8.21.3 GR7 (operands in order at each position) and
      *> GR11, and the format 3 legs by GR18 (two successive
      *> statements).
      *> Why it can fail: a counter store not seen by a later operand of
      *> the same statement, a format 3 replacing half that reads the
      *> image from before the tallying half, or a non-overlapping pair
      *> run as separate passes (C1 would be 02,00).
      *> The program is the 2023 twin's two pb1907 cases merged; the
      *> format 3 case's T, C and S2 are renamed T9, C9 and S3.
      *> EXPECTED:
      *>   D1  G1 = T(0)+"AAAAA", TALLYING T FOR ALL "A": one operand,
      *>       its count (5) is stored after the scan        [5AAAAA]
      *>   D2  G2 = T(0)+"1A1A1", T FOR ALL "A" T FOR ALL "2": the
      *>       first operand counts two "A" and stores T=2; the second
      *>       sees "21A1A1", counts the "2" in T itself -> T=3
      *>                                                    [31A1A1]
      *>   D3  T3 = "1A2B2", C3 = 1, C3 FOR ALL "A" C3 FOR ALL C3:
      *>       one "A" -> C3=2; the pattern C3 is now "2" and matches
      *>       twice -> C3=4                                    [4]
      *>   D4  one operand, the counter is the INSPECT item: C4 = 11,
      *>       two "1" -> 11+2                                 [13]
      *>   D5  T6 = "AB1AB2", C6 = 1, C6 FOR ALL "A" C6 FOR CHARACTERS
      *>       BEFORE C6: two "A" -> C6=3; BEFORE "3" is absent, so
      *>       the region is the whole image and CHARACTERS counts the
      *>       four characters the "A" operand did not consume
      *>       -> 3+4                                           [7]
      *>   RM  reference-modified: G7 = T(0)+"AA", INSPECT G7(1:3)
      *>       TALLYING T FOR ALL "A" T FOR ALL "2": "0AA" -> T=2,
      *>       then "2AA" -> T=3                              [3AA]
      *>   C1  counters in separate records overlap nothing: the ONE
      *>       cycle of GR7 runs, ALL "BC" is tried first at each
      *>       position and fails, ALL "AB" matches at 1 and 4:
      *>       BC=0 AB=2 (§2002:14.8.21.3 GR7a, 7b)  [00,02]
      *>   C2  the counter is in a REDEFINES of the INSPECT item's
      *>       record, so the compiler cannot prove it disjoint, but
      *>       it sits over a different part: S2RT is S2T, S2D is
      *>       "AAXA". Re-running the cycle changes nothing:
      *>       3 "A" then 1 "X" -> 4                        [4AAXA]
      *> EXPECTED:
      *>   F3A  G5 = T(0)+"AAA", TALLYING T FOR ALL "A" REPLACING ALL
      *>        "A" BY "B": the tally stores T=3 ("3AAA"), the
      *>        replacing half turns "3AAA" into "3BBB"      [3BBB]
      *>   F3B  T9 = "A0A1", C9 = 0, TALLYING C9 FOR ALL "A"
      *>        REPLACING ALL C9 BY "Z": C9 becomes 2 and the replacing
      *>        pattern C9 is
      *>        then "2", which T9 does not contain       [A0A1|2]
      *>   F3C  the counter is in a REDEFINES of identifier-1's
      *>        record, over a different part (S3RT is S3T, the INSPECT
      *>        item is S3D = "AAXA"): the compiler cannot prove that,
      *>        so it re-reads the image; nothing changes. T9=3 and the
      *>        three "A" become "Z"                       [3ZZXZ]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1T PIC 9 VALUE 0.
          05 G1D PIC X(5) VALUE "AAAAA".
       01 G2.
          05 G2T PIC 9 VALUE 0.
          05 G2D PIC X(5) VALUE "1A1A1".
       01 T3   PIC X(5) VALUE "1A2B2".
       01 C3   PIC 9 VALUE 1.
       01 C4   PIC 99 VALUE 11.
       01 T6   PIC X(6) VALUE "AB1AB2".
       01 C6   PIC 9 VALUE 1.
       01 G7.
          05 G7T PIC 9 VALUE 0.
          05 G7D PIC X(2) VALUE "AA".
       01 T8   PIC X(6) VALUE "ABCABC".
       01 C8A  PIC 99 VALUE 0.
       01 C8B  PIC 99 VALUE 0.
       01 S2.
          05 S2T PIC 9 VALUE 0.
          05 S2D PIC X(4) VALUE "AAXA".
       01 S2R REDEFINES S2.
          05 S2RT PIC 9.
          05 S2RD PIC X(4).
       01 G5.
          05 G5T PIC 9 VALUE 0.
          05 G5D PIC X(3) VALUE "AAA".
       01 T9    PIC X(4) VALUE "A0A1".
       01 C9    PIC 9 VALUE 0.
       01 S3.
          05 S3T PIC 9 VALUE 0.
          05 S3D PIC X(4) VALUE "AAXA".
       01 S3R REDEFINES S3.
          05 S3RT PIC 9.
          05 S3RD PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INSPECT G1 TALLYING G1T FOR ALL "A"
           DISPLAY "D1=[" G1 "]"
           INSPECT G2 TALLYING G2T FOR ALL "A" G2T FOR ALL "2"
           DISPLAY "D2=[" G2 "]"
           INSPECT T3 TALLYING C3 FOR ALL "A" C3 FOR ALL C3
           DISPLAY "D3=[" C3 "]"
           INSPECT C4 TALLYING C4 FOR ALL "1"
           DISPLAY "D4=[" C4 "]"
           INSPECT T6 TALLYING C6 FOR ALL "A" C6 FOR CHARACTERS
                      BEFORE C6
           DISPLAY "D5=[" C6 "]"
           INSPECT G7(1:3) TALLYING G7T FOR ALL "A" G7T FOR ALL "2"
           DISPLAY "RM=[" G7 "]"
           INSPECT T8 TALLYING C8A FOR ALL "BC" C8B FOR ALL "AB"
           DISPLAY "C1=[" C8A "," C8B "]"
           INSPECT S2D TALLYING S2RT FOR ALL "A" S2RT FOR ALL "X"
           DISPLAY "C2=[" S2 "]"
           INSPECT G5 TALLYING G5T FOR ALL "A"
                      REPLACING ALL "A" BY "B"
           DISPLAY "F3A=[" G5 "]"
           INSPECT T9 TALLYING C9 FOR ALL "A" REPLACING ALL C9 BY "Z"
           DISPLAY "F3B=[" T9 "|" C9 "]"
           INSPECT S3D TALLYING S3RT FOR ALL "A"
                      REPLACING ALL "A" BY "Z"
           DISPLAY "F3C=[" S3 "]"
           STOP RUN.
