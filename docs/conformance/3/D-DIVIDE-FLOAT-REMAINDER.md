- **D-DIVIDE-FLOAT-REMAINDER — DIVIDE ... GIVING identifier-3 REMAINDER identifier-4 with a floating-point identifier-3: the subsidiary quotient
  (§2023:14.9.12.4 6) c), 7); kb/Work PB2923, 2026-10-26).** §2023:14.9.12.3 2) admits any elementary numeric or numeric-edited
  identifier-3, so a floating-point numeric-edited item (`PIC +9.99E+99`) and a binary floating-point item (COMP-1/-2,
  FLOAT-SHORT/-LONG/-EXTENDED, FLOAT-BINARY-32/-64; the wider FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 are refused at the data description, COBOL1564, A.3 items 17 and 19) are legal; the PB2923 adjudication found that IBM Enterprise COBOL and Micro Focus reject them,
  and that GnuCOBOL 3.2 has no floating-point numeric-edited item, so the ISO text decides. 6) c) gives the subsidiary quotient
  "the same number of digits and the same decimal point location as" identifier-3, which a floating-point item does not
  have as a pair, so WiseOwl COBOL takes the phrase to mean **identifier-3's own shape under truncation**: for a
  floating-point numeric-edited item, the quotient truncated to the significand's digit count, normalized as in §2023:14.6.8.4
  1), at the quotient's own exponent (1/3000 into `+9.99E+99` has the subsidiary quotient 3.33E-04, so
  `DIVIDE 1 BY 3000 GIVING FE REMAINDER R` leaves R = 1 - 3.33E-04 * 3000 = 0.001; a value past the exponent's range is the
  pinned saturated content and one nearer to zero than the item holds is zero, exactly as the item's store leaves them);
  for a binary floating-point item, the quotient truncated toward zero into the item's format (binary32 for COMP-1,
  FLOAT-SHORT and FLOAT-BINARY-32, binary64 for every other; a quotient the format holds is itself), which is the landing
  the item's own store performs under implied TRUNCATION (§2023:14.7.4.3 10), `FloatResultant`, so the two cannot disagree. The remainder is GR7's
  expression over that subsidiary quotient: the SDIDI expression under STANDARD-DECIMAL, the binary64 one natively
  (A.1 item 123). **The stored quotient is never derived from the subsidiary quotient** (§2023:14.9.12.4 6) b) forms and stores it,
  as §2023:14.9.12.4 5) b) does for the statement without REMAINDER): it keeps all its digits and its exponent, rounded by the
  receiver's mode. The same adjudication measured GnuCOBOL 3.2 truncating a FLOAT-LONG subsidiary quotient at a fixed 17
  fraction digits (FLOAT-SHORT 8), so for a quotient below 1E-17 it leaves the whole dividend as the remainder; this
  compiler does not follow it because that scale is not identifier-3's. Pinned by `pb2923_divide_remainder_float_edited`, `pb2923_divide_remainder_float_usage`
  and their STANDARD-DECIMAL twins.
