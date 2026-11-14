- **D-ARITHMETIC-MODE — Compile-time arithmetic mode (§2023:7.3.6.2 SR2 / §2023:7.3.6.3 GR2 — Annex E.2 item 6; the required §2023:4.2.16 implementor
  documentation)**: this determination is A.1 item 29 in §7 — one evaluator (`CompileTimeExpressionEvaluator`) for
  constant entries and directive operands alike. At COBOL-2023 the mode is .NET `System.Decimal`, an implementor-unique
  mode (not a §2023:8.8.1 standard mode), with intermediate results rounded to nearest, a tie to the even digit (division
  does not truncate) and a diagnosed error on overflow; at COBOL-2002 and 2014 the mode is standard arithmetic
  (34-digit standard-decimal intermediates, NEAREST-AWAY-FROM-ZERO). In every edition the final result is truncated
  toward zero to an integer.
