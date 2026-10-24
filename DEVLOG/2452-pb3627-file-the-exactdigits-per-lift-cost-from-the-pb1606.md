## Entry 2452 — 2026-10-24 02:41 PDT — PB3627: file the ExactDigits per-lift cost from the PB1606 review

The branch review of PB1606 (PR #483) left a low lead: `CobolDec.ExactDigits` formats every finite binary64 with the 767-digit exact formatter, about 4 us a lift, on every float operand of standard-decimal arithmetic and on every binary64 sender of a floating-point numeric-edited store in every arithmetic mode. The lead is now kb/Work note PB3627 (MINOR, runtime/numeric, performance). No code changed.

