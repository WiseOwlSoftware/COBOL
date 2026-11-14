- **D-NUMVAL-F-EXPONENT-SPACE — NUMVAL-F — a space may not SPLIT the exponent's `n` (§2023:15.69.3 r1/r5 · §2023:15.95.4 r1 b) 1; kb/Work PB256).**
  §2023:15.69.3 r5 says "Leading and trailing spaces in argument-1 are ignored. Embedded spaces in argument-1 are
  ignored **except** between the first numeric digit and the last digit that precedes a letter 'E'" — i.e. its
  except-clause names only the SIGNIFICAND. Read as "elide the space and JOIN the digit runs on either side",
  that would make `"1E+1 2345"` a five-digit exponent and §2023:15.95.4 r1 b) 5 would want the position of its fifth
  digit (9). **WiseOwl COBOL's determination is the other reading: "ignored" means the space CONTRIBUTES NOTHING
  where the format admits one, never that the runs on either side join — so an interior space ends `n` exactly
  as it ends the significand, and `FUNCTION TEST-NUMVAL-F("1E+1 2345")` is 6.** Two grounds agree. (a) §2023:15.69.3
  r1's figure — verified on the printed page 898, not only in the transcription — draws `n` as ONE contiguous
  metavariable ("n is one, two, three, or four digits representing the exponent") with `[ space-string ]` only
  before and after it; nothing admits a space inside it, and the space in `"1E+1 2345"` is therefore the legal
  trailing space-string with `'2'` the first character in error. (b) §2023:15.95.4 r1 b) 1 governs the case directly
  and is UNQUALIFIED as to which run — "if one or more spaces are embedded within a string of numeric
  characters, the returned value is the position of the first non-space character following the spaces" — where
  sub-notes 2, 3 and 4 of the same list each say "of the significand" explicitly; the drafters distinguished
  inside one list, so sub-note 1 reaches the exponent's digit string too. Consequences, all measured:
  `"1E+ 1234"` and `"1E+1234 "` conform (0), sub-note 5 fires only on a CONTIGUOUS fifth digit (`"1E+12345"`
  → 8), and the significand keeps its own answer (`"0 1E+2"` → 3, the standard's own example). Implemented at
  the one exponent loop in `CobolIntrinsics.Exact.cs#NvfScan`; pinned by
  `conformance:cases/15.95.4-Returned-value-rule/pb256_test_numval_f_spaces`.
