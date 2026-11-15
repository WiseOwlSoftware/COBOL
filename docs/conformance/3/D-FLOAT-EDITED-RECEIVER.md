- **D-FLOAT-EDITED-RECEIVER — Floating-point numeric-edited receiver — the overflow, underflow, size-error and incompatible-content
  dispositions (§2023:14.9.25.4 GR6 item 4; §2023:14.7.5 items 3/4; §2023:14.6.13.2 rule 4; kb/Work PB66, 2026-08-18).**
  §2023:14.9.25.4 GR6 item 4a says a MOVE whose sending value is farther from zero than the picture permits sets
  EC-DATA-OVERFLOW "and the content of the receiving data item is undefined"; item 4b says a value nearer to zero
  than permitted "is treated as zero". WiseOwl COBOL: **under EC-DATA-OVERFLOW checking the exception is fatal at the
  store and the receiver is UNCHANGED** (a USE declarative may RESUME); **with checking off the receiver holds the
  pinned SATURATED image** — the all-nines significand at the maximum exponent with the value's sign (`9.99E+9`
  for `PIC 9.99E+9`; `-9.99999E+99` for a negative value into `-9.9(5)E+99`) — never arbitrary content; underflow
  stores the §2023:13.18.40.5 rule-8 zero image (all digit positions zero, both signs positive; BLANK WHEN ZERO applies)
  with no exception. An ARITHMETIC statement's floating-point numeric-edited resultant takes the size error
  condition in BOTH directions (§2023:14.7.5 items 3 and 4 — the receiver unchanged, EC-SIZE-TRUNCATION) under ON SIZE
  ERROR / EC-SIZE checking; without either, the MOVE disposition above. The receiver's ROUNDED [MODE] rounds the
  SIGNIFICAND at its last digit (§2023:14.7.4.3 rules 3 to 10; a MOVE truncates, §2023:14.6.8.4 rule 2): a carry out of
  the last digit renormalizes and the exponent is tested after it (`9.996` into `+9.99E+99` is `+1.00E+01`,
  §2023:14.7.5 item 3 "after … any applicable rounding specifications"), and ROUNDED MODE IS PROHIBITED with a
  significand that cannot be held exactly is the size error condition with the receiver unchanged (§2023:14.7.4.3
  rule 7; unchecked, it lands truncated like every unchecked store, A.1 item 70). Pinned by
  `2023/pb2638_float_edited_rounded_store`. A de-editing MOVE from content that is not a possible
  result of editing into the picture (§2023:14.6.13.2 rule 4 — the result "is undefined") raises EC-DATA-INCOMPATIBLE
  (fatal) under checking with the receiver unchanged; with checking off the image is read digit-for-digit
  (a non-digit position contributes zero, a missing exponent sign reads positive). Pinned by
  `2023/pb66_float_edited_picture` and `2023/pb66_float_edited_algebraic_and_checking`. The value of
  `HIGHEST-ALGEBRAIC` / `LOWEST-ALGEBRAIC` of such an item is that same saturated extreme (§2023:15.43.4 / §2023:15.58.4 r2 —
  zero for LOWEST of an unsigned picture); an entry whose extreme would fail IN-ARITHMETIC-RANGE for the arithmetic
  mode in effect (binary64 natively — an exponent of three or four digits — or decimal128 under STANDARD-DECIMAL)
  is COBOL1660 at the function reference (§2023:15.43.4 r1 / §2023:15.58.4 r1).
