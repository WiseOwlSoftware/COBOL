## User guide: floating-point items in arithmetic; the shortest round-trip store held as PB4809

The owner (2026-11-05) kept D16's binary64 rule (PB4731, PB4751) and asked that it be documented for developers and that the
softer alternative be recorded for a later look. New page docs/manual/user-guide/floating-point.md states the rule, shows the
truncation surprise (`ZF + FUNCTION EXP10 (-6)` stores 0.000000 in 9V9(6), 0.000001 with ROUNDED), the exact remedies and the
GnuCOBOL difference; every value in its table was run on the compiler. kb/Work PB4809 (status owner) holds the option of
storing the shortest round-trip decimal, with its case for and against and the work if reopened.
