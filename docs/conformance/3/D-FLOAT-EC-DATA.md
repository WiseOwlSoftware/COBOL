- **D-FLOAT-EC-DATA — EC-DATA-NOT-FINITE / EC-DATA-OVERFLOW applied to EVERY floating-point usage (§2023:14.6.13.2 item 3 / §2023:14.9.25.4
  GR6 d)4.a)**: the standard scopes these to a "standard floating-point usage" (the ISO/IEC 60559 FLOAT-BINARY-32/64/128
  and FLOAT-DECIMAL-16/34 forms — §2023:13.18.60 item 19; FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2 have
  implementor-defined representation, §2023:13.18.60 item 21, with implementor-specified exception conditions per
  §2023:14.6.13.1). In the typed-native model EVERY floating-point usage is a native IEEE `float`/`double`, so **pinned
  choice: WiseOwl COBOL raises both ECs for all floating-point usages uniformly** — mandatory for the standard usages,
  an implementor determination (which the standard delegates) for FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2.
  EC-DATA-NOT-FINITE (fatal) fires when a NaN/±Infinity float sending operand is referenced (both the numeric-value and
  the string-image read paths) except in a class condition, a sign condition, a same-usage MOVE, or VALIDATE;
  EC-DATA-OVERFLOW (fatal) fires when a MOVE's algebraic value is farther from zero than the receiver's usage
  permits — §2023:14.9.25.4 GR6 d)4.a verbatim: "If the algebraic value of the sending operand is farther from zero
  than is permitted by the usage specifications of the receiving data item, the EC-DATA-OVERFLOW exception
  condition is set to exist, and the content of the receiving data item is undefined." Both default OFF
  (byte-identical to a pre-slice build). Goldens `2023/ec_data_not_finite`, `2023/ec_data_overflow`,
  `2023/pb271_float_receiver_overflow`.
  ⛔ **THAT TEST IS ON THE ALGEBRAIC VALUE, WHICH MEANS IT MUST HOLD THE DECIMAL SENDER** (kb/Work PB271). It
  used to be inferred from an IEEE cast: a single-precision receiver asked whether a FINITE double had become
  ±Infinity, and a double receiver was a bare cast on the premise that "a double receiver cannot overflow from
  a finite double". Both premises assumed the sender reaching them was a finite double. A STANDARD-DECIMAL
  intermediate reaches ±9.999…E+6144 and collapses to ±Infinity in the conversion, so `MOVE 1.0E+400 TO` a
  FLOAT-BINARY-64 (or -32, or FLOAT-LONG) stored +Infinity and raised NOTHING, at every checking level — and
  owner decision D-B made that literal legal in the DEFAULT arithmetic mode, so the shape reached ordinary
  programs. The screen is now on the `CobolDec` itself, before the conversion.
  ⚠ **What a DEFAULT build does is unchanged and is its own determination:** checking is off by default
  (§2023:14.6.13.1.1) and GR6 d)4.a makes the receiver's content undefined in the overflow case, so WiseOwl COBOL
  performs the ISO/IEC 60559 conversion and keeps its result — ±Infinity for an over-range value, zero for an
  under-range one, which is not this EC in either direction. `2023/pb271_float_receiver_overflow` pins that
  half; `2023/ec_data_overflow` pins the checking-ON half.
  **Documented gaps:** (a) a floating-point NUMERIC-EDITED MOVE receiver is not covered by the EC-DATA-OVERFLOW seam
  (§2023:14.9.25.4 GR4 also names it) — deferred until floating-point numeric-edited PICTUREs are supported; (b) a
  multi-receiver `ADD/SUBTRACT … TO/FROM` with several float receivers under EC-DATA-NOT-FINITE checking can
  half-commit earlier receivers before a later non-finite receiver's read raises — only observable under USE-F3 +
  RESUME NEXT STATEMENT, where a fatal-EC-interrupted statement already leaves undefined results (§2023:14.6.13.1.3), a
  precise follow-on.
