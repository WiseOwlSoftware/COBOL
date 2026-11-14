- **D-STR2 — STRING: an identifier DELIMITER is read when its transfer begins (kb/Work PB1908, row
  `GR-14.9.43.4-10`).** The same rule covers identifier-2 occupying identifier-3's storage (GR10 names it; Annex A.2
  item 60 names the DELIMITED BY identifier). **WiseOwl COBOL examines the sending operand against the delimiter's
  content as the earlier transfers left it**, so an earlier transfer that overwrote the delimiter changes where the
  next operand is cut (libcob `cob_string_delimited` keeps the delimiter's storage, and `cob_string_append` scans it
  with `memcmp` on every call). A literal delimiter, DELIMITED BY SIZE and a delimiter no transfer can reach are
  untouched. Measured (cobc 3.2.0 and this build agree): `STRING "x" "12x34" DELIMITED BY R1(1:1) INTO R1` over `R1 =
  ",bcdefghijkl"` gives `x12defghijkl` (the first transfer made the delimiter `x`; the earlier behavior gave
  `x12x34ghijkl`), `STRING ";" "ab;cd" DELIMITED BY DD INTO DG` with `DD` the first byte of `DG` gives `;ab.......`, and
  a two-character delimiter overwritten the same way gives `ab1.......`. Pinned by
  `conformance:cases/14.9.43-STRING/pb1908_string_overlap_delimiter` (D1 to D3) and the K2 leg of
  `conformance:cases/14.9.43-STRING/pb1908_string_overlap_linkage`.
