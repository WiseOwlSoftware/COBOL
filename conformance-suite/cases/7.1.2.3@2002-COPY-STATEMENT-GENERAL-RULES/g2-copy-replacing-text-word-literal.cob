      *>--- conformance-case
      *> id: g2-copy-replacing-text-word-literal
      *> rules: [2002-GR-7.1.2.3-8, 2002-GR-7.1.2.3-9,
      *>     2002-GR-7.1.2.3-10]
      *> accept: [2002]
      *> files: [G2CRTW.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR8 - "Each matched occurrence of pseudo-text-1,
      *> text-1, word-1, literal-3, or partial-word-1 in the library
      *> text is replaced" by the corresponding BY operand. §2002:7.1.2.3
      *> GR9 - "text-1, word-1, and literal-3 are treated as pseudo-text
      *> containing only text-1, word-1, or literal-3". §2002:7.1.2.3
      *> GR10 - the comparison cycle. The text-1, word-1 and literal-3
      *> operands exist in the 2002 format only. Library text G2CRTW:
      *>   DISPLAY "[" T (1) T(2) T ( I + 1 ) "|" xxw XXW-2
      *>     "|" "L1" "l1" "|" A1   OF G1, "]".
      *> Each leg, and why it can fail (TT = "abc", T OCCURS 3):
      *>   T (1) - text-1 T(1) is the text-words T ( 1 ) (parentheses
      *>     are separators in text manipulation, §2002:7.1.1.4; GR10c
      *>     "Each sequence of one or more space separators is
      *>     considered to be a single space"), so it matches; BY T(3)
      *>     -> c. Unmatched it would print a.
      *>   T(2) - matches no operand (GR10d tries each in turn), so
      *>     GR10e copies it -> b.
      *>   T ( I + 1 ) - text-1 T(I + 1) matches; BY T(1) -> a. The
      *>     earlier match of T(1) must not have rewritten its own
      *>     replacement (GR10f restarts after the matched words).
      *>   xxw - word-1 XXW matches by GR10c "each lowercase letter is
      *>     equivalent to its corresponding uppercase letter"; BY YYW
      *>     -> yw. XXW-2 is one text-word, not XXW (GR10b "equal,
      *>     character for character") -> x2.
      *>   "L1" - literal-3 matches; BY "R1" -> R1. "l1" does not: the
      *>     case equivalence of GR10c excepts literal content -> l1.
      *>   A1   OF G1, - text-1 A1 OF G1 matches across the run of
      *>     spaces (GR10c); BY A1 IN G2 -> h. Unmatched -> g.
      *> EXPECTED: [cba|ywx2|R1l1|h]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY17.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT VALUE "abc".
          05 T PIC X OCCURS 3.
       01 I PIC 9 VALUE 1.
       01 XXW PIC X(2) VALUE "xw".
       01 YYW PIC X(2) VALUE "yw".
       01 XXW-2 PIC X(2) VALUE "x2".
       01 G1.
          05 A1 PIC X VALUE "g".
       01 G2.
          05 A1 PIC X VALUE "h".
       PROCEDURE DIVISION.
       P1.
           COPY G2CRTW REPLACING T(1) BY T(3)
                T(I + 1) BY T(1) XXW BY YYW "L1" BY "R1"
                A1 OF G1 BY A1 IN G2.
           STOP RUN.
