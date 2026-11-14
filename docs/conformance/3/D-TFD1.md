- **D-TFD1 — TEST-FORMATTED-DATETIME with an argument-2 SHORTER than the format answers
  `FUNCTION LENGTH(argument-2) + 1` (§2023:15.92.4 r1; kb/Work PB255).** The shape is reachable — §2023:15.92.3 places no
  size rule on argument-2 and the compiler applies none — and §2023:15.92.4 r1 is silent on it: it answers with "the
  ordinal character position at which the first error in argument-2 was detected", and when the data simply runs
  out there is no character IN ERROR to point at. **WiseOwl COBOL returns the position one past the last character.**
  Two grounds. (a) The rule's own words are about the position *at which the error was **detected***, not the
  position *of the character in error* — the wording §2023:15.93.4 r1 b) uses for the other case ("the position of
  the first character in error"), so the drafters distinguished the two inside one family and this clause takes
  the detection reading. (b) §2023:15.93.4 r1 c) is the standard's OWN answer for exactly this shape in the sibling
  TEST- function — an argument that "contains valid characters but is incomplete" returns
  `(FUNCTION LENGTH (argument-1) + 1)` (`cite.py --check 15.93.4 "(FUNCTION LENGTH (argument-1) + 1)"` → OK,
  rule 1) — so adopting it keeps ONE answer across the two TEST- functions instead of two conventions for one
  situation. ⚠ §2023:15.93.4 r1 c) is cited as a CONVENTION the standard already chose for this shape, never as the
  governing rule for this function. Implemented at the three `pos >= data.Length` exits of the one analyzer,
  `CobolDate.cs#Analyze`; pinned by `conformance:cases/15.92.4-Returned-value-rules/pb255_test_formatted_datetime_bounds`.
