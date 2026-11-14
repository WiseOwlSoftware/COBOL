---
order: 13
---
> ⚖ **DETERMINATION — §2023:8.8.1.5.2 r2's "too small" is the round-to-zero test, and gradual underflow sets no
> exception** (2026-08-29; kb/Work PB145). A value below 10⁻⁶¹⁷⁶ re-rounds onto the 10⁻⁶¹⁷⁶ quantum under the
> INTERMEDIATE ROUNDING mode (genuine gradual underflow — reduced precision, not a flush); only a value that
> rounds to ZERO there raises EC-SIZE-UNDERFLOW. A tiny-and-inexact value landing on a NONZERO quantum
> (1.5E−6176 → 2E−6176) sets nothing — r2's "too small … to be contained in an item in decimal128 format"
> reads as the round-to-zero test, and IEC 60559's tiny∧inexact underflow SIGNAL (which the "exception
> conditions … shall be the same as … decimal128 format" paragraph could be read to import) is not modelled.
> Under INTERMEDIATE ROUNDING IS PROHIBITED a below-range landing keeps the too-small name EC-SIZE-UNDERFLOW —
> never §2023:14.7.4.3 r7's inexact-transfer EC-SIZE-TRUNCATION: one physical condition, one name.
