- **D-INCOMPATIBLE-FIXED — Incompatible data in a fixed-point numeric sending operand (§2023:14.6.13.2 rule 2).** The standard makes the
  RESULT OF THE REFERENCE undefined — "the result of the reference is undefined and an EC-DATA-INCOMPATIBLE
  exception condition is set to exist" — so what happens to the value is an implementor determination.
  WiseOwl COBOL: **under EC-DATA-INCOMPATIBLE checking the exception is fatal at the SENDING READ** (a USE declarative
  may RESUME, in which case the raising statement is abandoned and every receiver it had not yet stored is
  unchanged); **with checking off the content decodes DETERMINISTICALLY** — a zoned position that is not a digit
  contributes no digit and an all-non-digit image is zero, a packed nibble above 9 contributes none, a binary
  window is read as its two's-complement value — never arbitrary content and never a trap. Only a numeric leaf
  whose storage is a character window (a REDEFINES view, or a leaf under a group used as a whole operand —
  §2023:14.9.25.4 GR4 fills such a group "without consideration for the individual elementary or group items") can hold such
  content at all; a native-carrier leaf can only hold digits, so `IS NUMERIC` on one is the constant true
  (§2023:8.8.4.4.4 GR3 n)1). The test itself is the standard's own — "would evaluate to false in a numeric class
  condition" — evaluated over the item's byte representation: n)1.a's digits-plus-declared-sign for DISPLAY,
  n)1.c's "valid representation for the usage" for packed (decimal nibbles, and a sign nibble where the usage
  reserves one), and n)1.c's picture-range half for binary, where a `COMP-5` / `BINARY-*` item's range is its
  CONTAINER rather than a picture (§2023:13.18.60.4 GR12 — "the implementor may allow a wider range" — for `BINARY-*`, item 208 for `COMP-5`). For a
  CORRESPONDING statement the condition is deferred and set once after every implied statement completes, as
  §2023:14.7.6's last paragraph requires. Pinned by `2002/pb230_incompatible_sending_sweep`,
  `2023/pb230_incompatible_corresponding` and `85/pb230_class_numeric_image`.
