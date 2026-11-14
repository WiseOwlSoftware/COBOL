- **D-NUMVAL-F-EXPONENT-SIGN — NUMVAL-F — the exponent's sign is REQUIRED, and a text without it is refused, not read (§2023:15.69.3 r1 · §2023:15.3 r14 ·
  A.1 item 90; kb/Work PB3952).** §2023:15.69.3 r1's figure puts the exponent's `+`/`-` in braces (a required choice, unlike
  the bracketed optional leading sign), so `"1.5E2"` and `"1.5e2"` do not conform. GnuCOBOL's NUMVAL-F is lenient and reads
  `"1.5E2"` as 150; the standard is silent on no part of it (the rule speaks), so CLAUDE.md rule 1's GnuCOBOL tier does not
  apply, and the function is not made lenient. `FUNCTION TEST-NUMVAL-F("1.5E2")` is 5 (the digit where the sign belongs),
  and a NUMVAL-F reference with that argument sets EC-ARGUMENT-FUNCTION; with checking off its result is A.1 item 90's zero
  value of the numeric type (0), in every receiver and every channel (one `CobolIntrinsics.Exact.cs#NvfScan` serves the
  value twins and TEST-NUMVAL-F). Pinned by `conformance:cases/15.69.3-Argument-rules/pb3952_numval_f_exponent_sign_required`.
