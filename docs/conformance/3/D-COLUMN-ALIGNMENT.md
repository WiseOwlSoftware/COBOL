- **D-COLUMN-ALIGNMENT — COLUMN alignment (§2023:13.18.14.4 GR6 c)/d); kb/Work PB1220)**: RIGHT and CENTER derive an item's leftmost column
  from integer-1 and the printable-size ("integer-1 - printable-size + 1", "integer-1 - ((printable-size - 1) / 2)"),
  and the standard states no outcome when that column is before column 1 (GR5's EC-REPORT-PAGE-WIDTH covers only the
  right edge). Determination: the compiler REFUSES the clause (COBOL2712) instead of clamping, because a clamped
  placement would make GR9's horizontal counter — the item's rightmost column — disagree with where the item was
  printed. Both operands are compile-time integers over a compile-time width, so the verdict is a compile-time one.
  The standard being silent, the choice is the implementor's (CLAUDE.md rule 1 precedence); no GnuCOBOL behavior
  was measured for it.
