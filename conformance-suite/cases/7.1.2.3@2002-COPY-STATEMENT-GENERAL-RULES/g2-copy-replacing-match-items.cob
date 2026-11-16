      *>--- conformance-case
      *> id: g2-copy-replacing-match-items
      *> rules: [2002-GR-7.1.2.3-10]
      *> accept: [2002]
      *> files: [G2CRMI.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR10 - the matching items not exercised by
      *> g2-copy-replacing-text-word-literal (items a, b LEADING and
      *> TRAILING, c1 separator comma, c2, c4a, c4b, c5). The COPY statement stands
      *> inside a DISPLAY operand list, so the library text G2CRMI
      *> continues that list:
      *>   , K1 "|" X1, Y1 "|" "A"   &   "B" "|" "D" & "C" "|" 'L2'
      *>   "|" 'Q''R' "|" LX-LX "|" TX-TX "|" K5
      *>   >>DEFINE G2DV AS 1   (a compiler directive line)
      *>   K6 "]".
      *> Each leg, and why it can fail (K1="1" X1="x" Y1="y" K5="5"
      *> K6="6"):
      *>   , K1 - GR10a "The leftmost library text-word that is not a
      *>     separator comma or a separator semicolon is the first
      *>     text-word used for comparison"; the comma is copied and
      *>     ==K1== matches K1 -> k. Unmatched -> 1.
      *>   X1, Y1 - GR10c1 "Each occurrence of a separator comma,
      *>     semicolon, or space ... is considered to be a single space":
      *>     ==X1 Y1== matches -> m. Unmatched -> xy.
      *>   "A"   &   "B" / "D" & "C" - GR10c2 "Each operand and
      *>     operator of a concatenation expression is a separate
      *>     text-word" (the operator is spaced on both sides,
      *>     §2002:8.7.3): the three text-words match =="A" & "B"==
      *>     across the runs of spaces -> ab, and the single operand
      *>     "C" alone matches =="C"== -> "D" & "c" -> Dc. Unmatched ->
      *>     AB and DC.
      *>   'L2' - GR10c4a "The two representations of the quotation
      *>     symbol match": literal-3 "L2" matches the apostrophe form
      *>     -> r2. Unmatched -> L2.
      *>   'Q''R' - GR10c4b "two contiguous occurrences of the
      *>     character used as the quotation symbol in the opening
      *>     delimiter are treated as a single occurrence": its content
      *>     is Q'R and it matches =="Q'R"== -> qr. Unmatched -> Q'R.
      *>   LX-LX - GR10b "When the LEADING phrase is specified,
      *>     partial-word-1 matches ... starting with the leftmost
      *>     character position of a library text-word": LEADING ==LX==
      *>     BY ==LA== rewrites the leftmost LX only -> LA-LX = p.
      *>     Unmatched LX-LX = 0; every LX rewritten LA-LA = w.
      *>   TX-TX - GR10b "When the TRAILING phrase is specified ...
      *>     ending with the rightmost character position": TRAILING
      *>     ==TX== BY ==TB== -> TX-TB = q. Unmatched TX-TX = 0; every
      *>     TX rewritten TB-TB = w.
      *>   K5 / directive line / K6 - GR10c5 "Each occurrence of a
      *>     compiler directive line is treated as a single space":
      *>     ==K5 K6== matches across the >>DEFINE line -> kk.
      *>     Unmatched -> 56.
      *> EXPECTED: [k|m|ab|Dc|r2|qr|p|q|kk]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K1 PIC X VALUE "1".
       01 X1 PIC X VALUE "x".
       01 Y1 PIC X VALUE "y".
       01 LX-LX PIC X VALUE "0".
       01 LA-LX PIC X VALUE "p".
       01 LA-LA PIC X VALUE "w".
       01 TX-TX PIC X VALUE "0".
       01 TX-TB PIC X VALUE "q".
       01 TB-TB PIC X VALUE "w".
       01 K5 PIC X VALUE "5".
       01 K6 PIC X VALUE "6".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "["
           COPY G2CRMI REPLACING ==K1== BY =="k"==
                ==X1 Y1== BY =="m"== =="A" & "B"== BY =="ab"==
                =="C"== BY =="c"==
                "L2" BY "r2" =="Q'R"== BY =="qr"==
                LEADING ==LX== BY ==LA== TRAILING ==TX== BY ==TB==
                ==K5 K6== BY =="kk"==.
           STOP RUN.
